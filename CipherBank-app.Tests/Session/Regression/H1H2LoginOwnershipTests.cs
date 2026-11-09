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

    [Fact]
    public async Task LateNavigationFailure_CannotLockOrClearReplacement()
    {
        using H1H2TestRig rig = new();
        int creates = 0;
        rig.Product.Setup(p => p.CreateSessionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Credential(Interlocked.Increment(ref creates) == 1 ? "old" : "new"));
        AsyncGate navigationGate = new();
        Mock<INavigationService> navigation = new();
        navigation.Setup(n => n.GoToAsync(Routes.Dashboard)).Returns(async () =>
        {
            await navigationGate.PauseAsync();
            throw new HttpRequestException("old navigation failed");
        });
        using LoginViewModel login = Create(rig, navigation.Object);
        login.Password = "123456";
        Task attempt = login.SignInCommand.ExecuteAsync(null);
        await navigationGate.Entered;
        rig.App.Lock();
        Assert.True(await rig.App.UnlockAsync("123456"));
        navigationGate.Release();
        await attempt;
        Assert.Equal("new-access", rig.App.AccessToken);
        Assert.Equal("new-access", (await rig.Store.GetAsync())?.Access);
        Assert.True(rig.App.IsUnlocked);
        Assert.True(rig.Stream.IsConnected);
        Assert.True(rig.Hub.IsRunning);
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
