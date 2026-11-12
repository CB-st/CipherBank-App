// <copyright file="H1H2CoordinatorRegressionTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.V1;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using static CipherBank_app.Tests.Session.Regression.H1H2TestRig;

namespace CipherBank_app.Tests.Session.Regression;

public sealed class H1H2CoordinatorRegressionTests
{
    public static TheoryData<string, string, bool> StartupCases
    {
        get
        {
            TheoryData<string, string, bool> rows = [];
            foreach (string entry in new[] { "pin", "device", "setup" })
            {
                foreach (string boundary in new[] { "custody", "create", "publish", "connect", "prefs", "bootstrap" })
                {
                    // Setup deliberately skips bootstrap; its final prefs read is the executed late boundary.
                    string executedBoundary = entry == "setup" && boundary == "bootstrap" ? "final-prefs" : boundary;
                    rows.Add(entry, executedBoundary, false);
                    rows.Add(entry, executedBoundary, true);
                }
            }

            return rows;
        }
    }

    [Theory]
    [MemberData(nameof(StartupCases))]
    public async Task LockOrExpiry_AtStartupBoundary_RevokesRealCoordinatorAndHub(string entry, string boundary, bool expiry)
    {
        using H1H2TestRig rig = new(new H1H2ThreeKeyStore());
        AsyncGate gate = GateBoundary(rig, boundary);
        bool success = false;
        Task attempt = CompleteAsync();
        await gate.WaitForEntryAsync(attempt);
        if (expiry)
        {
            rig.Expire();
        }
        else
        {
            rig.App.Lock();
        }

        gate.Release();
        await ObserveAsync(() => attempt);

        Assert.False(success);
        Assert.Null(rig.App.AccessToken);
        Assert.Null(await rig.Store.GetAsync());
        Assert.False(rig.Stream.IsConnected);
        Assert.False(rig.Hub.IsRunning);
        Assert.False(rig.App.IsUnlocked);
        if (entry == "setup")
        {
            await rig.App.BootAsync();
            Assert.True(rig.App.HasWallet);
            rig.Bootstrap.Verify(p => p.ApplyAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        async Task CompleteAsync()
        {
            if (entry == "setup")
            {
                await rig.App.FinishCustodySetupAsync("fixture mnemonic is never sent", "123456");
                success = true;
            }
            else
            {
                success = entry == "device"
                    ? await rig.App.UnlockWithDeviceOwnerAsync() : await rig.App.UnlockAsync("123456");
            }
        }
    }

    [Theory]
    [InlineData("pin")]
    [InlineData("device")]
    [InlineData("setup")]
    public async Task FinalPrefsBoundary_WithoutInvalidation_CompletesAndSelectsBootstrap(string entry)
    {
        using H1H2TestRig rig = new(new H1H2ThreeKeyStore());
        AsyncGate gate = GateBoundary(rig, "final-prefs");
        Task attempt = CompleteAsync();
        try
        {
            await gate.WaitForEntryAsync(attempt);
            Assert.True(rig.App.IsUnlocked);
            Assert.Equal("current-access", (await rig.Store.GetAsync())?.Access);
            Assert.True(rig.Stream.IsConnected);
            Assert.True(rig.Hub.IsRunning);
            rig.Bootstrap.Verify(
                p => p.ApplyAsync(It.IsAny<CancellationToken>()),
                entry == "setup" ? Times.Never() : Times.Once());
        }
        finally
        {
            gate.Release();
        }

        await attempt.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal("current-access", rig.App.AccessToken);
        Assert.True(rig.App.IsUnlocked);
        if (entry == "setup")
        {
            await rig.App.BootAsync();
            Assert.True(rig.App.HasWallet);
        }

        async Task CompleteAsync()
        {
            if (entry == "setup")
            {
                await rig.App.FinishCustodySetupAsync("fixture mnemonic is never sent", "123456");
            }
            else
            {
                Assert.True(entry == "device"
                    ? await rig.App.UnlockWithDeviceOwnerAsync() : await rig.App.UnlockAsync("123456"));
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OlderCreateSuccessOrFailure_AfterReplacement_PreservesRealSession(bool fail)
    {
        using H1H2TestRig rig = new();
        AsyncGate gate = new();
        int calls = 0;
        rig.Product.Setup(p => p.CreateSessionAsync(It.IsAny<CancellationToken>())).Returns(async () =>
        {
            if (Interlocked.Increment(ref calls) == 1)
            {
                await gate.PauseAsync(CancellationToken.None);
                if (fail)
                {
                    throw new HttpRequestException("old create failed");
                }

                return Credential("old");
            }

            return Credential("new");
        });
        bool oldSuccess = false;
        Task older = CompleteAsync();
        await gate.Entered;
        rig.App.Lock();
        Assert.True(await rig.App.UnlockAsync("123456"));
        gate.Release();
        await ObserveAsync(() => older);

        Assert.False(oldSuccess);
        Assert.Equal("new-access", rig.App.AccessToken);
        Assert.Equal("new-access", (await rig.Store.GetAsync())?.Access);
        Assert.True(rig.App.IsUnlocked);
        Assert.True(rig.Stream.IsConnected);
        Assert.True(rig.Hub.IsRunning);

        async Task CompleteAsync() => oldSuccess = await rig.App.UnlockAsync("123456");
    }

    [Theory]
    [InlineData("connect", false)]
    [InlineData("prefs", false)]
    [InlineData("prefs", true)]
    [InlineData("bootstrap", false)]
    [InlineData("bootstrap", true)]
    public async Task LateOwnedEffect_AfterReplacementAdmission_CannotDamageReplacement(string boundary, bool fail)
    {
        using H1H2TestRig rig = new();
        AsyncGate gate = new();
        int creates = 0;
        rig.Product.Setup(p => p.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Credential(Interlocked.Increment(ref creates) == 1 ? "old" : "new"));
        if (boundary == "connect")
        {
            rig.Stream.ConnectGate = gate;
        }
        else if (boundary == "prefs")
        {
            rig.Sync.Setup(p => p.PullMergeAsync(It.IsAny<CancellationToken>())).Returns(OldEffectAsync);
        }
        else
        {
            rig.Bootstrap.Setup(p => p.ApplyAsync(It.IsAny<CancellationToken>())).Returns(OldEffectAsync);
        }

        bool oldSuccess = false;
        Task older = CompleteOldAsync();
        await gate.Entered;
        rig.App.Lock();
        rig.Stream.ConnectGate = null;
        rig.Sync.Setup(p => p.PullMergeAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        rig.Bootstrap.Setup(p => p.ApplyAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        Task<bool> replacement = rig.App.UnlockAsync("123456");
        gate.Release();
        await ObserveAsync(() => older);
        Assert.True(await replacement.WaitAsync(TimeSpan.FromSeconds(10)));

        Assert.False(oldSuccess);
        Assert.Equal("new-access", rig.App.AccessToken);
        Assert.Equal("new-access", (await rig.Store.GetAsync())?.Access);
        Assert.True(rig.App.IsUnlocked);
        Assert.True(rig.Stream.IsConnected);
        Assert.True(rig.Hub.IsRunning);

        async Task CompleteOldAsync() => oldSuccess = await rig.App.UnlockAsync("123456");

        async Task OldEffectAsync()
        {
            await gate.PauseAsync(CancellationToken.None);
            if (fail)
            {
                throw new HttpRequestException("late best-effort failure");
            }
        }
    }

    [Fact]
    public async Task InMemoryDtoProvider_StartLockAndReunlock_PersistsAndDeliversEarlyFrames()
    {
        using H1H2TestRig rig = new(client: new InMemoryProductClient(new FixedClock()));
        int delivered = 0;
        rig.Hub.EventReceived += (_, _) => delivered++;
        Assert.True(await rig.App.UnlockAsync("123456"));
        Assert.Equal("mock-access", (await rig.Store.GetAsync())?.Access);
        Assert.Equal(1, delivered); // Subscription must precede connect's early frame.
        rig.App.Lock();
        Assert.Null(await rig.Store.GetAsync());
        Assert.False(rig.Hub.IsRunning);
        rig.Stream.Emit();
        Assert.Equal(1, delivered);
        Assert.True(await rig.App.UnlockWithDeviceOwnerAsync());
        Assert.Equal("mock-access", rig.App.AccessToken);
        Assert.Equal(2, delivered);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task HttpResponse_LockOrExpiry_RevokesRealStartup(bool expiry)
    {
        AsyncGate response = new();
        using HttpClient http = new(new Transport(async (_, _) =>
        {
            await response.PauseAsync(CancellationToken.None);
            return Json(Credential("late-http"));
        })) { BaseAddress = new Uri("https://product.test/") };
        using H1H2TestRig rig = new(
            new H1H2ThreeKeyStore(),
            clientFactory: services => ActivatorUtilities.CreateInstance<HttpProductClient>(services, http));
        bool success = false;
        Task attempt = CompleteAsync();
        await response.Entered;
        if (expiry)
        {
            rig.Expire();
        }
        else
        {
            rig.App.Lock();
        }

        response.Release();
        await ObserveAsync(() => attempt);
        Assert.False(success);
        Assert.Null(rig.App.AccessToken);
        Assert.Null(await rig.Store.GetAsync());
        Assert.False(rig.Stream.IsConnected);
        Assert.False(rig.Hub.IsRunning);

        async Task CompleteAsync() => success = await rig.App.UnlockAsync("123456");
    }

    [Fact]
    public async Task HttpProvider_RealCoordinatorStartup_HasOnePublisher()
    {
        H1H2ThreeKeyStore backing = new();
        using HttpClient http = new(new Transport((_, _) => Task.FromResult(Json(Credential("http")))))
        {
            BaseAddress = new Uri("https://product.test/"),
        };
        using H1H2TestRig rig = new(
            backing,
            clientFactory: services => ActivatorUtilities.CreateInstance<HttpProductClient>(services, http));

        Assert.True(await rig.App.UnlockAsync("123456"));
        Assert.Equal("http-access", rig.App.AccessToken);
        Assert.Equal("http-access", (await backing.GetAsync())?.Access);
        Assert.True(rig.Stream.IsConnected);
        Assert.True(rig.Hub.IsRunning);
        Assert.Equal(1, backing.SaveCalls);
    }

    [Fact]
    public async Task CallerCancellation_DuringPrefs_RollsBackAndPropagates()
    {
        using H1H2TestRig rig = new();
        using CancellationTokenSource caller = new();
        AsyncGate gate = new();
        rig.Sync.Setup(p => p.PullMergeAsync(It.IsAny<CancellationToken>()))
            .Returns<CancellationToken>(ct => gate.PauseAsync(ct));
        Task start = rig.Coordinator.StartAsync(true, caller.Token);
        await gate.Entered;
        caller.Cancel();
        Exception? error = await ObserveAsync(() => start);
        Assert.IsAssignableFrom<OperationCanceledException>(error);
        Assert.Null(await rig.Store.GetAsync());
        Assert.False(rig.Hub.IsRunning);
        Assert.False(rig.Stream.IsConnected);
    }

    private static AsyncGate GateBoundary(H1H2TestRig rig, string boundary)
    {
        AsyncGate gate = new();
        switch (boundary)
        {
            case "custody":
                rig.CustodyGate = gate;
                break;
            case "create":
                rig.Product.Setup(p => p.CreateSessionAsync(It.IsAny<CancellationToken>()))
                    .Returns(async () =>
                    {
                        await gate.PauseAsync(CancellationToken.None);
                        return Credential("late");
                    });
                break;
            case "publish":
                ((H1H2ThreeKeyStore)rig.Store).SaveGate = gate;
                break;
            case "connect":
                rig.Stream.ConnectGate = gate;
                break;
            case "prefs":
                rig.Sync.Setup(p => p.PullMergeAsync(It.IsAny<CancellationToken>())).Returns(() => gate.PauseAsync());
                break;
            case "final-prefs":
                int loads = 0;
                rig.Prefs.Setup(p => p.LoadAsync(It.IsAny<CancellationToken>())).Returns(async () =>
                {
                    // Coordinator loads once before create, then again after sync/optional bootstrap.
                    if (Interlocked.Increment(ref loads) == 2)
                    {
                        await gate.PauseAsync(CancellationToken.None);
                    }

                    return new CipherBank_app.Persist.UserPrefs(CipherBank_app.Tests.Persist.TestPreferenceDefaults.Value)
                    {
                        LockIdleSeconds = 120,
                    };
                });
                break;
            case "bootstrap":
                rig.Bootstrap.Setup(p => p.ApplyAsync(It.IsAny<CancellationToken>())).Returns(() => gate.PauseAsync());
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(boundary));
        }

        return gate;
    }
}
