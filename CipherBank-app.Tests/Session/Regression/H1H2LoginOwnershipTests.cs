// <copyright file="H1H2LoginOwnershipTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

#if DEBUG
using CipherBank_app.Configuration;
#endif
using CipherBank_app.Constants;
using CipherBank_app.Services;
using CipherBank_app.V1;
using CipherBank_app.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
#if DEBUG
using Microsoft.Extensions.Options;
#endif
using Moq;
using Xunit;
using static CipherBank_app.Tests.Session.Regression.H1H2TestRig;

namespace CipherBank_app.Tests.Session.Regression;

public sealed class H1H2LoginOwnershipTests
{
    [Fact]
    public async Task LockDuringStartup_OriginalLoginCannotNavigate()
    {
        using H1H2TestRig rig = new();
        AsyncGate create = new();
        rig.Product.Setup(p => p.CreateSessionAsync(It.IsAny<CancellationToken>())).Returns(async () =>
        {
            await create.PauseAsync();
            return Credential("old");
        });
        Mock<INavigationService> navigation = new();
        using LoginViewModel login = Create(rig, navigation.Object);
        login.Password = "123456";
        Task attempt = login.SignInCommand.ExecuteAsync(null);
        await create.Entered;
        rig.App.Lock();
        create.Release();
        await attempt;
        navigation.Verify(n => n.GoToAsync(Routes.Dashboard), Times.Never);
        Assert.Null(rig.App.AccessToken);
        Assert.False(rig.App.IsUnlocked);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task LateNavigationFailureOrCancellation_CannotLockOrClearReplacement(bool cancel)
    {
        H1H2ThreeKeyStore backing = new();
        using H1H2TestRig rig = new(backing);
        int creates = 0;
        rig.Product.Setup(p => p.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Credential(Interlocked.Increment(ref creates) == 1 ? "old" : "new"));
        AsyncGate navigationGate = new();
        Mock<INavigationService> navigation = new();
        navigation.Setup(n => n.GoToAsync(Routes.Dashboard)).Returns(async () =>
        {
            await navigationGate.PauseAsync();
            if (cancel)
            {
                throw new OperationCanceledException("old navigation cancelled");
            }

            throw new HttpRequestException("old navigation failed");
        });
        using LoginViewModel login = Create(rig, navigation.Object);
        login.Password = "123456";
        Task attempt = login.SignInCommand.ExecuteAsync(null);
        await navigationGate.Entered;
        rig.App.Lock();
        Assert.True(await rig.App.UnlockAsync("123456"));
        int clearsBeforeRollback = backing.ClearCalls;
        if (cancel)
        {
            login.CancelLogin();
        }

        navigationGate.Release();
        await attempt;
        await rig.Services.GetRequiredService<ProductSessionAuthority>().Drain;
        Assert.Equal(clearsBeforeRollback, backing.ClearCalls);
        Assert.Equal(3, backing.KeyCount);
        Assert.Equal("new-access", rig.App.AccessToken);
        Assert.Equal("new-access", (await rig.Store.GetAsync())?.Access);
        Assert.True(rig.App.IsUnlocked);
        Assert.True(rig.Stream.IsConnected);
        Assert.True(rig.Hub.IsRunning);
    }

    [Fact]
    public async Task B1_LoginRollback_OneDrainedClearBeforeNewerUnlock()
    {
        H1H2ThreeKeyStore backing = new();
        using H1H2TestRig rig = new(backing);
        ProductSessionAuthority authority = rig.Services.GetRequiredService<ProductSessionAuthority>();
        AsyncGate navigationGate = new();
        Mock<INavigationService> navigation = new();
        navigation.Setup(n => n.GoToAsync(Routes.Dashboard)).Returns(async () =>
        {
            await navigationGate.PauseAsync();
            throw new HttpRequestException("navigation requires login rollback");
        });
        using LoginViewModel login = Create(rig, navigation.Object);
        login.Password = "123456";
        Task attempt = login.SignInCommand.ExecuteAsync(null);
        await navigationGate.Entered;
        AsyncGate local = new();
        AsyncGate clear = new();
        backing.ClearGate = clear;
        Task admitted = authority.RunLocalAsync(authority.Capture(), () => local.PauseAsync(), CancellationToken.None);
        await local.Entered;
        Task drain;
        try
        {
            navigationGate.Release();
            // Exercise real navigation rollback through the ownership-admitted AppSession.Lock.
            await attempt.WaitAsync(TimeSpan.FromSeconds(10));
            drain = authority.Drain;
            local.Release();
            await clear.Entered;
            Assert.False(drain.IsCompleted);
        }
        finally
        {
            navigationGate.Release();
            local.Release();
            clear.Release();
        }

        await ObserveAsync(() => admitted);
        await drain.WaitAsync(TimeSpan.FromSeconds(10));
        int clearsAtDrain = backing.ClearCalls;
        Assert.True(await rig.App.UnlockAsync("123456"));
        Assert.Equal("current-access", rig.App.AccessToken);
        Assert.Equal("current-access", (await backing.GetAsync())?.Access);
        Assert.Equal(3, backing.KeyCount);
        Assert.Equal(clearsAtDrain, backing.ClearCalls);
        Assert.Equal(1, clearsAtDrain);
    }

    private static LoginViewModel Create(H1H2TestRig rig, INavigationService navigation)
    {
        ProductSessionAuthority authority = rig.Services.GetRequiredService<ProductSessionAuthority>();
#if DEBUG
        Mock<ISettingsService> settings = new();
        settings.SetupGet(s => s.Environment).Returns("Production");
        return new LoginViewModel(
            NullLogger<LoginViewModel>.Instance,
            rig.App,
            navigation,
            Mock.Of<IDialogService>(),
            settings.Object,
            Options.Create(new HostBehaviorOptions()),
            authority);
#else
        return new LoginViewModel(NullLogger<LoginViewModel>.Instance, rig.App, navigation, Mock.Of<IDialogService>(), authority);
#endif
    }
}
