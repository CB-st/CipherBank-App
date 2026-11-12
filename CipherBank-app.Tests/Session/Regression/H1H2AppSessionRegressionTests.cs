// <copyright file="H1H2AppSessionRegressionTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Session;
using CipherBank_app.Tests.Persist;
using CipherBank_app.V1;
using CipherBank_app.Wallets;
using Moq;
using Xunit;
using static CipherBank_app.Tests.Session.Regression.H1H2TestRig;

namespace CipherBank_app.Tests.Session.Regression;

/// <summary>Tests the public AppSession boundary, including collaborators that ignore cancellation.</summary>
public sealed class H1H2AppSessionRegressionTests
{
    public static TheoryData<string, string, bool> InvalidationCases
    {
        get
        {
            TheoryData<string, string, bool> rows = [];
            foreach (string entry in new[] { "pin", "device", "setup" })
            {
                foreach (string boundary in new[] { "custody", "startup" })
                {
                    rows.Add(entry, boundary, false);
                    rows.Add(entry, boundary, true);
                }
            }

            return rows;
        }
    }

    [Theory]
    [MemberData(nameof(InvalidationCases))]
    public async Task LockOrExpiry_AtCustodyOrFinalResult_RejectsLateSuccess(string entry, string boundary, bool expiry)
    {
        using H1H2TestRig rig = new();
        AsyncGate gate = new();
        Mock<IProductSessionCoordinator> product = new();
        product.Setup(p => p.DisconnectAsync()).Returns(Task.CompletedTask);
        product.Setup(p => p.StartAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (boundary == "startup")
                {
                    await gate.PauseAsync(CancellationToken.None);
                }

                return new ProductSessionStartResult("late-access", 120);
            });
        if (boundary == "custody")
        {
            rig.CustodyGate = gate;
        }

        AppSession app = CreateApp(rig.Custody.Object, product.Object);
        bool success = false;
        Task attempt = AttemptAsync(app, entry, value => success = value);
        await gate.Entered;
        if (expiry)
        {
            rig.Expire();
        }
        else
        {
            app.Lock();
        }

        gate.Release();
        await ObserveAsync(() => attempt);

        Assert.False(success);
        Assert.Null(app.AccessToken);
        Assert.False(app.IsUnlocked);
        if (entry == "setup")
        {
            // Durable custody truth survives remote-session invalidation; no destructive rollback oracle.
            await app.BootAsync();
            Assert.True(app.HasWallet);
        }
    }

    [Theory]
    [InlineData("pin", false)]
    [InlineData("pin", true)]
    [InlineData("device", false)]
    [InlineData("device", true)]
    [InlineData("setup", false)]
    [InlineData("setup", true)]
    public async Task OlderStartupSuccessOrFailure_AfterNewerSuccess_PreservesNewerOwnership(string entry, bool fail)
    {
        using H1H2TestRig rig = new();
        AsyncGate gate = new();
        int starts = 0;
        Mock<IProductSessionCoordinator> product = new();
        product.Setup(p => p.DisconnectAsync()).Returns(Task.CompletedTask);
        product.Setup(p => p.StartAsync(It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                if (Interlocked.Increment(ref starts) == 1)
                {
                    await gate.PauseAsync(CancellationToken.None);
                    if (fail)
                    {
                        throw new HttpRequestException("old startup failed");
                    }

                    return new ProductSessionStartResult("old-access", 120);
                }

                return new ProductSessionStartResult("new-access", 120);
            });
        AppSession app = CreateApp(rig.Custody.Object, product.Object);
        bool oldSuccess = false;
        Task older = AttemptAsync(app, entry, value => oldSuccess = value);
        await gate.Entered;
        app.Lock();
        Assert.True(await app.UnlockAsync("123456"));
        gate.Release();
        await ObserveAsync(() => older);

        Assert.False(oldSuccess);
        Assert.Equal("new-access", app.AccessToken);
        Assert.True(app.IsUnlocked);
    }

    private static AppSession CreateApp(ICustodyService custody, IProductSessionCoordinator product)
    {
        Mock<IPrefsStore> prefs = new();
        prefs.Setup(p => p.LoadAsync()).ReturnsAsync(new UserPrefs(TestPreferenceDefaults.Value));
        return new AppSession(
            custody,
            product,
            Mock.Of<ILocalWalletSeeder>(),
            prefs.Object,
            new FixedClock(),
            ProductSessionAuthority.For(new InMemoryProductSessionStore()));
    }

    private static async Task AttemptAsync(AppSession app, string entry, Action<bool> completed)
    {
        if (entry == "setup")
        {
            await app.FinishCustodySetupAsync("fixture mnemonic is never sent", "123456");
            completed(true);
        }
        else
        {
            completed(entry == "device" ? await app.UnlockWithDeviceOwnerAsync() : await app.UnlockAsync("123456"));
        }
    }
}
