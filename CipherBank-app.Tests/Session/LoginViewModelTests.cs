// <copyright file="LoginViewModelTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

#if DEBUG
using CipherBank_app.Configuration;
#endif
using CipherBank_app.Constants;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Services;
using CipherBank_app.Session;
using CipherBank_app.V1;
using CipherBank_app.ViewModels;
using CipherBank_app.Wallets;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
#if DEBUG
using Microsoft.Extensions.Options;
#endif
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Session;

/// <summary>Exercises the linked production ViewModel and generated command without MAUI.</summary>
public sealed class LoginViewModelTests
{
    /// <summary>PIN authentication goes through the application session, with no username requirement.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SignIn_RoutesThroughSession_AndNavigatesOnlyOnSuccess(bool unlocked)
    {
        Mock<IAppSession> session = new();
        session.Setup(s => s.UnlockAsync("123456")).ReturnsAsync(unlocked);
        Mock<INavigationService> navigation = new();
        using LoginViewModel viewModel = Create(session.Object, navigation.Object);
        viewModel.Password = "123456";

        await viewModel.SignInCommand.ExecuteAsync(null);

        session.Verify(s => s.UnlockAsync("123456"), Times.Once);
        navigation.Verify(n => n.GoToAsync(Routes.Dashboard), unlocked ? Times.Once() : Times.Never());
        viewModel.IsBusy.Should().BeFalse();
        viewModel.Password.Should().BeEmpty();
        if (!unlocked)
        {
            viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        }
    }

    /// <summary>Navigation waits for the real AppSession to complete product startup.</summary>
    [Fact]
    public async Task SignIn_AwaitsRealLifecycleBeforeNavigation()
    {
        Mock<ICustodyService> custody = new();
        custody.Setup(c => c.UnlockAsync("123456")).ReturnsAsync(true);
        TaskCompletionSource<ProductSessionStartResult> startup = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IProductSessionCoordinator> coordinator = new();
        coordinator.Setup(c => c.StartAsync(true, It.Is<CancellationToken>(ct => ct.CanBeCanceled))).Returns(startup.Task);
        AppSession session = new(
            custody.Object,
            coordinator.Object,
            Mock.Of<ILocalWalletSeeder>(),
            Mock.Of<IPrefsStore>(),
            TimeProvider.System,
            ProductSessionAuthority.For(new InMemoryProductSessionStore()));
        Mock<INavigationService> navigation = new();
        using LoginViewModel viewModel = Create(session, navigation.Object);
        viewModel.Password = "123456";

        Task login = viewModel.SignInCommand.ExecuteAsync(null);
        navigation.VerifyNoOtherCalls();
        viewModel.IsBusy.Should().BeTrue();
        startup.SetResult(new ProductSessionStartResult("access", 60));
        await login;

        session.AccessToken.Should().Be("access");
        navigation.Verify(n => n.GoToAsync(Routes.Dashboard), Times.Once);
    }

    /// <summary>Cancellation and disposal both observe pending startup and lock before returning.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancelDuringUnlock_ObservesCompletionAndLocksWithoutNavigation(bool dispose)
    {
        TaskCompletionSource<bool> unlock = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IAppSession> session = new();
        session.Setup(s => s.UnlockAsync("123456")).Returns(unlock.Task);
        Mock<INavigationService> navigation = new();
        using LoginViewModel viewModel = Create(session.Object, navigation.Object);
        viewModel.Password = "123456";
        Task login = viewModel.SignInCommand.ExecuteAsync(null);

        if (dispose)
        {
            viewModel.Dispose();
        }
        else
        {
            viewModel.CancelLogin();
        }

        login.IsCompleted.Should().BeFalse("startup must not be abandoned");
        viewModel.IsBusy.Should().BeTrue();
        unlock.SetResult(true);
        await login;

        session.Verify(s => s.Lock(), Times.Once);
        navigation.VerifyNoOtherCalls();
        viewModel.ErrorMessage.Should().Be("Login cancelled");
        viewModel.IsBusy.Should().BeFalse();
    }

    /// <summary>Unexpected startup failure stays on login, clears busy state, and relocks.</summary>
    [Fact]
    public async Task StartupFailure_DoesNotNavigateAndLocksSession()
    {
        Mock<IAppSession> session = new();
        session.Setup(s => s.UnlockAsync("123456")).ThrowsAsync(new InvalidOperationException("unavailable"));
        Mock<INavigationService> navigation = new();
        using LoginViewModel viewModel = Create(session.Object, navigation.Object);
        viewModel.Password = "123456";

        await viewModel.SignInCommand.ExecuteAsync(null);

        session.Verify(s => s.Lock(), Times.Once);
        navigation.VerifyNoOtherCalls();
        viewModel.ErrorMessage.Should().NotBeNullOrEmpty();
        viewModel.IsBusy.Should().BeFalse();
    }

    /// <summary>Empty PIN is validation only and never attempts custody unlock.</summary>
    [Fact]
    public async Task MissingPin_DoesNotAttemptUnlock()
    {
        Mock<IAppSession> session = new(MockBehavior.Strict);
        Mock<INavigationService> navigation = new(MockBehavior.Strict);
        using LoginViewModel viewModel = Create(session.Object, navigation.Object);

        await viewModel.SignInCommand.ExecuteAsync(null);

        session.VerifyNoOtherCalls();
        navigation.VerifyNoOtherCalls();
        viewModel.ErrorMessage.Should().Be("Please enter your custody PIN");
    }

    /// <summary>Concurrent lifetime notifications cannot race cancellation-source disposal.</summary>
    [Fact]
    public async Task ConcurrentCancellationAndDisposal_RemainSafeAcrossCompletion()
    {
        TaskCompletionSource<bool> unlock = new(TaskCreationOptions.RunContinuationsAsynchronously);
        Mock<IAppSession> session = new();
        session.Setup(s => s.UnlockAsync("123456")).Returns(unlock.Task);
        Mock<INavigationService> navigation = new();
        using LoginViewModel viewModel = Create(session.Object, navigation.Object);
        viewModel.Password = "123456";
        Task login = viewModel.SignInCommand.ExecuteAsync(null);
        viewModel.CancelLogin();

        Task cancel = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                viewModel.CancelLogin();
                viewModel.Dispose();
            }
        });
        unlock.SetResult(true);
        await Task.WhenAll(login, cancel);
        viewModel.CancelLogin();
        viewModel.Dispose();

        session.Verify(s => s.Lock(), Times.Once);
        navigation.VerifyNoOtherCalls();
        viewModel.IsBusy.Should().BeFalse();
    }

    private static LoginViewModel Create(IAppSession session, INavigationService navigation)
    {
#if DEBUG
        Mock<ISettingsService> settings = new();
        settings.SetupGet(s => s.Environment).Returns("Production");
        return new LoginViewModel(
            NullLogger<LoginViewModel>.Instance,
            session,
            navigation,
            Mock.Of<IDialogService>(),
            settings.Object,
            Options.Create(new HostBehaviorOptions()));
#else
        return new LoginViewModel(NullLogger<LoginViewModel>.Instance, session, navigation, Mock.Of<IDialogService>());
#endif
    }
}
