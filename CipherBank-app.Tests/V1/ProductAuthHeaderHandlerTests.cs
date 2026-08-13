// <copyright file="ProductAuthHeaderHandlerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Net;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public sealed class ProductAuthHeaderHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsBearer_WhenSessionPresent()
    {
        InMemoryProductSessionStore sessions = new InMemoryProductSessionStore();
        await sessions.SaveAsync(new SessionDto
        {
            AccessToken = "tok-abc",
            RefreshToken = "ref",
            ExpiresAt = 9_999_999_999_000,
        });

        CaptureHandler inner = new CaptureHandler();
        ProductAuthHeaderHandler handler = new ProductAuthHeaderHandler(sessions)
        {
            InnerHandler = inner,
        };
        using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        using HttpResponseMessage resp = await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://product.test/v1/portfolio"),
            CancellationToken.None);

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        inner.Authorization.Should().Be("Bearer tok-abc");
    }

    [Fact]
    public async Task SendAsync_SkipsAuthorization_WhenSessionMissing()
    {
        CaptureHandler inner = new CaptureHandler();
        ProductAuthHeaderHandler handler = new ProductAuthHeaderHandler(new InMemoryProductSessionStore())
        {
            InnerHandler = inner,
        };
        using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        await invoker.SendAsync(
            new HttpRequestMessage(HttpMethod.Get, "https://product.test/v1/portfolio"),
            CancellationToken.None);

        inner.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task SendAsync_Throws_WhenRequestNull()
    {
        ProductAuthHeaderHandler handler = new ProductAuthHeaderHandler(new InMemoryProductSessionStore())
        {
            InnerHandler = new CaptureHandler(),
        };
        using HttpMessageInvoker invoker = new HttpMessageInvoker(handler);

        Func<Task> act = () => invoker.SendAsync(null!, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    private sealed class CaptureHandler : HttpMessageHandler
    {
        public string? Authorization { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        }
    }
}
