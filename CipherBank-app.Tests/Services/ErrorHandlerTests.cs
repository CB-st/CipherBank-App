// <copyright file="ErrorHandlerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using CipherBank_app.Constants;
using CipherBank_app.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Services;

public class ErrorHandlerTests
{
    [Fact]
    public async Task HandleApiErrorsAsync_DirectUnauthorizedHttpRequest_ClearsSessionBeforeLoginRedirect()
    {
        var navigation = new Mock<INavigationService>();
        var auth = new Mock<IAuthService>();
        var logout = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var navigatedBeforeLogout = false;
        auth.Setup(service => service.LogoutAsync()).Returns(() => logout.Task);
        navigation.Setup(service => service.GoToAsync(Routes.Login)).Returns(() =>
        {
            navigatedBeforeLogout = true;
            return Task.CompletedTask;
        });
        ErrorHandler handler = CreateHandler(navigation, auth);
        string? message = null;

        Task<bool> pending = handler.HandleApiErrorsAsync(
            () => throw Unauthorized(),
            value => message = value);

        await Task.Yield();
        navigatedBeforeLogout.Should().BeFalse();
        navigation.Verify(service => service.GoToAsync(It.IsAny<string>()), Times.Never);

        logout.SetResult();
        bool result = await pending;

        result.Should().BeFalse();
        message.Should().Be("Session expired. Please log in again.");
        navigatedBeforeLogout.Should().BeTrue();
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
    }

    [Fact]
    public async Task HandleApiErrorsAsync_WrappedUnauthorized_ClearsSessionThenNavigates()
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler();
        var order = new List<string>();
        auth.Setup(service => service.LogoutAsync()).Returns(() =>
        {
            order.Add("logout");
            return Task.CompletedTask;
        });
        navigation.Setup(service => service.GoToAsync(Routes.Login)).Returns(() =>
        {
            order.Add("navigate");
            return Task.CompletedTask;
        });

        bool result = await handler.HandleApiErrorsAsync(
            () => throw new InvalidOperationException("prices failed", Unauthorized()),
            _ => { });

        result.Should().BeFalse();
        order.Should().Equal("logout", "navigate");
    }

    [Fact]
    public async Task HandleApiErrorsAsync_DeeplyNestedUnauthorized_ClearsSessionThenNavigates()
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler();
        Exception nested = new InvalidOperationException(
            "outer",
            new IOException("middle", Unauthorized(new InvalidOperationException("root"))));

        bool result = await handler.HandleApiErrorsAsync(() => throw nested, _ => { });

        result.Should().BeFalse();
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
    }

    [Fact]
    public async Task HandleApiErrorsAsync_AggregateContainingUnauthorized_ClearsSessionThenNavigates()
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler();
        var aggregate = new AggregateException(
            new InvalidOperationException("unrelated"),
            new IOException("wrapped", Unauthorized()));

        bool result = await handler.HandleApiErrorsAsync(() => throw aggregate, _ => { });

        result.Should().BeFalse();
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
    }

    [Fact]
    public async Task HandleApiErrorsAsync_UnauthorizedAccessException_ClearsSessionThenNavigates()
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler();

        bool result = await handler.HandleApiErrorsAsync(
            () => throw new InvalidOperationException("wrapper", new UnauthorizedAccessException("expired")),
            _ => { });

        result.Should().BeFalse();
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
    }

    [Theory]
    [InlineData("//LoginPage")]
    [InlineData("//LoginPage/")]
    [InlineData("//LoginPage?return=dashboard")]
    [InlineData("//LoginPage#top")]
    [InlineData("/LoginPage")]
    [InlineData("LoginPage")]
    [InlineData("app://cipherbank/LoginPage")]
    [InlineData("//DashboardPage/LoginPage")]
    [InlineData("//loginpage")]
    public async Task HandleApiErrorsAsync_WhenAlreadyOnLogin_ClearsSessionWithoutNavigating(string route)
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler(route);

        bool result = await handler.HandleApiErrorsAsync(() => throw Unauthorized(), _ => { });

        result.Should().BeFalse();
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("//DashboardPage")]
    [InlineData("//LoginPageSettings")]
    [InlineData("//Login")]
    public async Task HandleApiErrorsAsync_WhenNotOnLogin_NavigatesAfterClearingSession(string? route)
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler(route);

        bool result = await handler.HandleApiErrorsAsync(() => throw Unauthorized(), _ => { });

        result.Should().BeFalse();
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
    }

    [Fact]
    public async Task HandleApiErrorsAsync_ReentrantUnauthorizedDuringRedirect_NavigatesOnce()
    {
        var navigation = new Mock<INavigationService>();
        var auth = new Mock<IAuthService>();
        auth.Setup(service => service.LogoutAsync()).Returns(Task.CompletedTask);
        navigation.SetupGet(service => service.CurrentRoute).Returns((string?)null);
        ErrorHandler handler = new(navigation.Object, auth.Object);
        var navigations = 0;
        navigation.Setup(service => service.GoToAsync(Routes.Login)).Returns(() =>
        {
            navigations++;
            return handler.HandleApiErrorsAsync(() => throw Unauthorized(), _ => { });
        });

        bool result = await handler.HandleApiErrorsAsync(() => throw Unauthorized(), _ => { });

        result.Should().BeFalse();
        navigations.Should().Be(1);
        auth.Verify(service => service.LogoutAsync(), Times.Exactly(2));
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
    }

    [Fact]
    public async Task HandleApiErrorsAsync_ConcurrentUnauthorized_NavigatesOnce()
    {
        var navigation = new Mock<INavigationService>();
        var auth = new Mock<IAuthService>();
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var navigations = 0;
        auth.Setup(service => service.LogoutAsync()).Returns(Task.CompletedTask);
        navigation.SetupGet(service => service.CurrentRoute).Returns((string?)null);
        navigation.Setup(service => service.GoToAsync(Routes.Login)).Returns(async () =>
        {
            if (Interlocked.Increment(ref navigations) == 1)
            {
                started.TrySetResult();
                await release.Task;
            }
        });
        ErrorHandler handler = new(navigation.Object, auth.Object);

        Task<bool> first = handler.HandleApiErrorsAsync(() => throw Unauthorized(), _ => { });
        await started.Task;
        bool second = await handler.HandleApiErrorsAsync(() => throw Unauthorized(), _ => { });
        release.SetResult();
        bool firstResult = await first;

        firstResult.Should().BeFalse();
        second.Should().BeFalse();
        navigations.Should().Be(1);
        auth.Verify(service => service.LogoutAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleApiErrorsAsync_AfterRedirectReachesLogin_SecondUnauthorizedDoesNotNavigate()
    {
        var route = "//DashboardPage";
        var navigation = new Mock<INavigationService>();
        var auth = new Mock<IAuthService>();
        auth.Setup(service => service.LogoutAsync()).Returns(Task.CompletedTask);
        navigation.SetupGet(service => service.CurrentRoute).Returns(() => route);
        navigation.Setup(service => service.GoToAsync(Routes.Login)).Returns(() =>
        {
            route = Routes.Login;
            return Task.CompletedTask;
        });
        ErrorHandler handler = new(navigation.Object, auth.Object);

        await handler.HandleApiErrorsAsync(() => throw Unauthorized(), _ => { });
        await handler.HandleApiErrorsAsync(() => throw Unauthorized(), _ => { });

        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
        auth.Verify(service => service.LogoutAsync(), Times.Exactly(2));
    }

    [Fact]
    public async Task HandleApiErrorsAsync_NonUnauthorizedHttpRequest_DoesNotClearSessionOrNavigate()
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler();
        string? message = null;

        bool result = await handler.HandleApiErrorsAsync(
            () => throw new HttpRequestException("offline", null, HttpStatusCode.ServiceUnavailable),
            value => message = value,
            "Failed to refresh. Pull down to try again.");

        result.Should().BeFalse();
        message.Should().Be("Failed to refresh. Pull down to try again.");
        auth.Verify(service => service.LogoutAsync(), Times.Never);
        navigation.Verify(service => service.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleApiErrorsAsync_WrappedNonUnauthorized_PropagatesWithoutClearingSession()
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler();
        var failure = new InvalidOperationException(
            "prices failed",
            new HttpRequestException("error", null, HttpStatusCode.InternalServerError));

        Func<Task> act = () => handler.HandleApiErrorsAsync(() => throw failure, _ => { });

        await act.Should().ThrowAsync<InvalidOperationException>();
        auth.Verify(service => service.LogoutAsync(), Times.Never);
        navigation.Verify(service => service.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleApiErrorsAsync_CancellationWrappingUnauthorized_ClearsSessionAndRedirects()
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler();
        string? message = null;

        bool result = await handler.HandleApiErrorsAsync(
            () => throw new OperationCanceledException("cancelled", Unauthorized()),
            value => message = value);

        result.Should().BeFalse();
        message.Should().Be("Session expired. Please log in again.");
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
    }

    [Fact]
    public async Task HandleApiErrorsAsync_TaskCanceledWrappingUnauthorized_ClearsSessionAndRedirects()
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler();
        string? message = null;

        bool result = await handler.HandleApiErrorsAsync(
            () => throw new TaskCanceledException("cancelled", Unauthorized()),
            value => message = value);

        result.Should().BeFalse();
        message.Should().Be("Session expired. Please log in again.");
        auth.Verify(service => service.LogoutAsync(), Times.Once);
        navigation.Verify(service => service.GoToAsync(Routes.Login), Times.Once);
    }

    [Fact]
    public async Task HandleApiErrorsAsync_CancellationWithoutUnauthorized_DoesNotClearSessionOrNavigate()
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler();
        string? message = null;

        bool result = await handler.HandleApiErrorsAsync(
            () => throw new OperationCanceledException("cancelled"),
            value => message = value);

        result.Should().BeFalse();
        message.Should().BeNull();
        auth.Verify(service => service.LogoutAsync(), Times.Never);
        navigation.Verify(service => service.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleApiErrorsAsync_WhenLogoutThrows_DoesNotNavigate()
    {
        (ErrorHandler handler, Mock<INavigationService> navigation, Mock<IAuthService> auth) = CreateReadyHandler();
        auth.Setup(service => service.LogoutAsync()).ThrowsAsync(new InvalidOperationException("storage failed"));

        Func<Task> act = () => handler.HandleApiErrorsAsync(() => throw Unauthorized(), _ => { });

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("storage failed");
        navigation.Verify(service => service.GoToAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task HandleApiErrorsAsync_Success_DoesNotClearSession()
    {
        (ErrorHandler handler, _, Mock<IAuthService> auth) = CreateReadyHandler();

        bool result = await handler.HandleApiErrorsAsync(() => Task.CompletedTask, _ => { });

        result.Should().BeTrue();
        auth.Verify(service => service.LogoutAsync(), Times.Never);
    }

    private static (ErrorHandler Handler, Mock<INavigationService> Navigation, Mock<IAuthService> Auth) CreateReadyHandler(string? route = null)
    {
        var navigation = new Mock<INavigationService>();
        var auth = new Mock<IAuthService>();
        navigation.SetupGet(service => service.CurrentRoute).Returns(route);
        auth.Setup(service => service.LogoutAsync()).Returns(Task.CompletedTask);
        navigation.Setup(service => service.GoToAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
        return (new ErrorHandler(navigation.Object, auth.Object), navigation, auth);
    }

    private static ErrorHandler CreateHandler(Mock<INavigationService> navigation, Mock<IAuthService> auth)
    {
        navigation.SetupGet(service => service.CurrentRoute).Returns((string?)null);
        return new ErrorHandler(navigation.Object, auth.Object);
    }

    private static HttpRequestException Unauthorized(Exception? inner = null)
        => new("denied", inner, HttpStatusCode.Unauthorized);
}
