// <copyright file="AuthHeaderHandlerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using System.Net.Http.Headers;
using CipherBank_app.Models;
using CipherBank_app.Services;
using CipherBank_app.Services.Handlers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.Services;

public sealed class AuthHeaderHandlerTests
{
    [Fact]
    public async Task SendAsync_WhenTokenIsMissing_FailsWithoutSendingRequest()
    {
        Mock<IAuthService> auth = new();
        auth.Setup(service => service.GetStoredTokenAsync()).ReturnsAsync((AuthToken?)null);
        int sendCount = 0;
        using ServiceProvider provider = CreateProvider(auth.Object);
        using AuthHeaderHandler handler = CreateHandler(provider, _ => sendCount++);
        using HttpMessageInvoker invoker = new(handler);

        Func<Task> act = async () => await invoker.SendAsync(CreateRequest("wallets"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        sendCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_WhenAuthServiceIsUnavailable_FailsWithoutSendingRequest()
    {
        int sendCount = 0;
        using ServiceProvider provider = CreateProvider();
        using AuthHeaderHandler handler = CreateHandler(provider, _ => sendCount++);
        using HttpMessageInvoker invoker = new(handler);

        Func<Task> act = async () => await invoker.SendAsync(CreateRequest("wallets"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        sendCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_WhenStoredAccessTokenIsWhitespace_FailsWithoutSendingRequest()
    {
        Mock<IAuthService> auth = new();
        auth.Setup(service => service.GetStoredTokenAsync()).ReturnsAsync(
            new AuthToken(" ", "refresh", DateTimeOffset.UtcNow.AddHours(1)));
        using ServiceProvider provider = CreateProvider(auth.Object);
        int sendCount = 0;
        using AuthHeaderHandler handler = CreateHandler(provider, _ => sendCount++);
        using HttpMessageInvoker invoker = new(handler);

        Func<Task> act = async () => await invoker.SendAsync(CreateRequest("wallets"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        sendCount.Should().Be(0);
        auth.Verify(service => service.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WhenRefreshTokenIsWhitespace_FailsWithoutRefreshing()
    {
        Mock<IAuthService> auth = new();
        auth.Setup(service => service.GetStoredTokenAsync()).ReturnsAsync(
            new AuthToken("stale", " ", DateTimeOffset.UtcNow.AddMinutes(1)));
        using ServiceProvider provider = CreateProvider(auth.Object);
        int sendCount = 0;
        using AuthHeaderHandler handler = CreateHandler(provider, _ => sendCount++);
        using HttpMessageInvoker invoker = new(handler);

        Func<Task> act = async () => await invoker.SendAsync(CreateRequest("wallets"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        sendCount.Should().Be(0);
        auth.Verify(service => service.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("blank")]
    [InlineData("null")]
    [InlineData("expired")]
    public async Task SendAsync_WhenRefreshReturnsUnusableToken_FailsWithoutSendingRequest(string kind)
    {
        Mock<IAuthService> auth = new();
        auth.Setup(service => service.GetStoredTokenAsync()).ReturnsAsync(
            new AuthToken("stale", "refresh", DateTimeOffset.UtcNow.AddMinutes(1)));
        AuthToken? refreshed = kind == "null" ? null : kind == "blank"
            ? new AuthToken(" ", "new-refresh", DateTimeOffset.UtcNow.AddHours(1))
            : new AuthToken("fresh", "new-refresh", DateTimeOffset.UtcNow);
        auth.Setup(service => service.RefreshAsync("refresh", It.IsAny<CancellationToken>())).ReturnsAsync(refreshed!);
        int sendCount = 0;
        using ServiceProvider provider = CreateProvider(auth.Object);
        using AuthHeaderHandler handler = CreateHandler(provider, _ => sendCount++);
        using HttpMessageInvoker invoker = new(handler);

        Func<Task> act = async () => await invoker.SendAsync(CreateRequest("wallets"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        sendCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_WhenTokenDoesNotExpireSoon_DoesNotRefresh()
    {
        Mock<IAuthService> auth = new();
        auth.Setup(service => service.GetStoredTokenAsync()).ReturnsAsync(
            new AuthToken("valid", "refresh", DateTimeOffset.UtcNow.AddHours(1)));
        using ServiceProvider provider = CreateProvider(auth.Object);
        AuthenticationHeaderValue? authorization = null;
        using AuthHeaderHandler handler = CreateHandler(provider, request => authorization = request.Headers.Authorization);
        using HttpMessageInvoker invoker = new(handler);

        using HttpResponseMessage response = await invoker.SendAsync(CreateRequest("wallets"), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        authorization?.Scheme.Should().Be("Bearer");
        authorization?.Parameter.Should().Be("valid");
        auth.Verify(service => service.RefreshAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendAsync_WhenTokenRetrievalFails_FailsWithoutSendingRequest()
    {
        Mock<IAuthService> auth = new();
        auth.Setup(service => service.GetStoredTokenAsync()).ThrowsAsync(new InvalidOperationException());
        int sendCount = 0;
        using ServiceProvider provider = CreateProvider(auth.Object);
        using AuthHeaderHandler handler = CreateHandler(provider, _ => sendCount++);
        using HttpMessageInvoker invoker = new(handler);

        Func<Task> act = async () => await invoker.SendAsync(CreateRequest("wallets"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        sendCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_WhenCallerCancelsRefresh_PropagatesCancellation()
    {
        using CancellationTokenSource cancellation = new();
        Mock<IAuthService> auth = new();
        auth.Setup(service => service.GetStoredTokenAsync()).ReturnsAsync(
            new AuthToken("stale", "refresh", DateTimeOffset.UtcNow.AddMinutes(1)));
        auth.Setup(service => service.RefreshAsync("refresh", cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));
        using ServiceProvider provider = CreateProvider(auth.Object);
        using AuthHeaderHandler handler = CreateHandler(provider, _ => { });
        using HttpMessageInvoker invoker = new(handler);
        cancellation.Cancel();

        Func<Task> act = async () => await invoker.SendAsync(CreateRequest("wallets"), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SendAsync_ForAuthEndpointLookalike_RequiresAuthentication()
    {
        using ServiceProvider provider = CreateProvider();
        int sendCount = 0;
        using AuthHeaderHandler handler = CreateHandler(provider, _ => sendCount++);
        using HttpMessageInvoker invoker = new(handler);

        Func<Task> act = async () => await invoker.SendAsync(CreateRequest("api/v1/auth/login-history"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        sendCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_WhenRefreshFails_FailsWithoutSendingRequest()
    {
        Mock<IAuthService> auth = new();
        auth.Setup(service => service.GetStoredTokenAsync()).ReturnsAsync(
            new AuthToken("stale", "refresh", DateTimeOffset.UtcNow.AddMinutes(1)));
        auth.Setup(service => service.RefreshAsync("refresh", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException());
        int sendCount = 0;
        using ServiceProvider provider = CreateProvider(auth.Object);
        using AuthHeaderHandler handler = CreateHandler(provider, _ => sendCount++);
        using HttpMessageInvoker invoker = new(handler);

        Func<Task> act = async () => await invoker.SendAsync(CreateRequest("wallets"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        sendCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_WhenRefreshSucceeds_SendsRefreshedBearerToken()
    {
        Mock<IAuthService> auth = new();
        auth.Setup(service => service.GetStoredTokenAsync()).ReturnsAsync(
            new AuthToken("stale", "refresh", DateTimeOffset.UtcNow.AddMinutes(1)));
        auth.Setup(service => service.RefreshAsync("refresh", It.IsAny<CancellationToken>())).ReturnsAsync(
            new AuthToken("fresh", "new-refresh", DateTimeOffset.UtcNow.AddHours(1)));
        AuthenticationHeaderValue? authorization = null;
        using ServiceProvider provider = CreateProvider(auth.Object);
        using AuthHeaderHandler handler = CreateHandler(provider, request => authorization = request.Headers.Authorization);
        using HttpMessageInvoker invoker = new(handler);

        using HttpResponseMessage response = await invoker.SendAsync(CreateRequest("wallets"), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        authorization?.Scheme.Should().Be("Bearer");
        authorization?.Parameter.Should().Be("fresh");
    }

    [Theory]
    [InlineData("auth/login")]
    [InlineData("auth/refresh")]
    [InlineData("auth/register")]
    public async Task SendAsync_ForPublicAuthEndpoint_AllowsAnonymousRequest(string path)
    {
        using ServiceProvider provider = CreateProvider();
        int sendCount = 0;
        using AuthHeaderHandler handler = CreateHandler(provider, _ => sendCount++);
        using HttpMessageInvoker invoker = new(handler);

        using HttpResponseMessage response = await invoker.SendAsync(CreateRequest(path), CancellationToken.None);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        sendCount.Should().Be(1);
    }

    [Fact]
    public async Task SendAsync_WhenRequestUriIsRelative_FailsClosedWithoutUriException()
    {
        using ServiceProvider provider = CreateProvider();
        int sendCount = 0;
        using AuthHeaderHandler handler = CreateHandler(provider, _ => sendCount++);
        using HttpMessageInvoker invoker = new(handler);

        Func<Task> act = async () => await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "wallets"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        sendCount.Should().Be(0);
    }

    [Fact]
    public async Task SendAsync_WhenRelativeUriQueryContainsAuthPath_RequiresAuthentication()
    {
        using ServiceProvider provider = CreateProvider();
        int sendCount = 0;
        using AuthHeaderHandler handler = CreateHandler(provider, _ => sendCount++);
        using HttpMessageInvoker invoker = new(handler);

        Func<Task> act = async () => await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "wallets?next=/auth/login"), CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
        sendCount.Should().Be(0);
    }

    private static ServiceProvider CreateProvider(IAuthService? auth = null)
    {
        ServiceCollection services = new();
        if (auth is not null)
        {
            services.AddSingleton(auth);
        }

        return services.BuildServiceProvider();
    }

    private static AuthHeaderHandler CreateHandler(IServiceProvider provider, Action<HttpRequestMessage> onSend)
    {
        return new AuthHeaderHandler(provider)
        {
            InnerHandler = new CallbackHandler(onSend),
        };
    }

    private static HttpRequestMessage CreateRequest(string path) =>
        new(HttpMethod.Get, new Uri($"https://example.test/{path}"));

    private sealed class CallbackHandler(Action<HttpRequestMessage> onSend) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            onSend(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
