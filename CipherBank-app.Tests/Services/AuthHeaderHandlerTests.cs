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
