// <copyright file="ShellAuthHeaderHandlerTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using CipherBank_app.Services.Handlers;
using CipherBank_app.V1;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CipherBank_app.Tests.V1;

public sealed class ShellAuthHeaderHandlerTests
{
    private static readonly DateTimeOffset Now = new(2035, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public async Task SendAsync_TransportFailure_PreservesOriginalAndSendsOnce(
        bool cancellation, bool succeedOnSecondSend, bool callerCancelled)
    {
        using CancellationTokenSource caller = new();
        if (callerCancelled)
        {
            caller.Cancel();
        }

        Exception original = cancellation
            ? new OperationCanceledException("transport cancelled", caller.Token)
            : new HttpRequestException("transport failed");
        using ServiceProvider services = CreateServices("valid");
        ProbeHandler transport = new(original, succeedOnSecondSend);
        using HttpMessageInvoker invoker = new(new AuthHeaderHandler(services, new FixedClock()) { InnerHandler = transport });
        using HttpRequestMessage request = new(HttpMethod.Post, "https://product.test/v1/transfers");

        Exception? observed = await Record.ExceptionAsync(async () =>
        {
            using HttpResponseMessage response = await invoker.SendAsync(request, caller.Token);
        });

        Assert.Equal(1, transport.Calls);
        Assert.Same(original, observed);
        Assert.Equal(caller.Token, transport.Token);
        Assert.Equal("Bearer access", transport.Authorization);
    }

    [Theory]
    [InlineData("valid", "/v1/portfolio", "Bearer access")]
    [InlineData("missing", "/v1/portfolio", null)]
    [InlineData("expired", "/v1/portfolio", null)]
    [InlineData("skew-boundary", "/v1/portfolio", null)]
    [InlineData("no-store", "/v1/portfolio", null)]
    [InlineData("throws", "/v1/portfolio", null)]
    [InlineData("valid", "/v1/session", null)]
    [InlineData("valid", "/v1/session/refresh", null)]
    public async Task SendAsync_DecorationControls_SendOnceAndReturnOriginalResponse(string session, string path, string? authorization)
    {
        using ServiceProvider services = CreateServices(session);
        ProbeHandler transport = new();
        using HttpMessageInvoker invoker = new(new AuthHeaderHandler(services, new FixedClock()) { InnerHandler = transport });
        using HttpRequestMessage request = new(HttpMethod.Get, "https://product.test" + path);

        using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);

        Assert.Same(transport.Response, response);
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(1, transport.Calls);
        Assert.Equal(authorization, transport.Authorization);
    }

    [Fact]
    public async Task SendAsync_CoreTransportFailure_PreservesOriginalAndSendsOnce()
    {
        HttpRequestException original = new("core transport failed");
        ProbeHandler transport = new(original);
        using HttpMessageInvoker invoker = new(new ProductAuthHeaderHandler(new InMemoryProductSessionStore()) { InnerHandler = transport });
        using HttpRequestMessage request = new(HttpMethod.Get, "https://product.test/v1/portfolio");

        Exception? observed = await Record.ExceptionAsync(async () =>
        {
            using HttpResponseMessage response = await invoker.SendAsync(request, CancellationToken.None);
        });

        Assert.Equal(1, transport.Calls);
        Assert.Same(original, observed);
    }

    private static ServiceProvider CreateServices(string state)
    {
        ServiceCollection services = new();
        if (state != "no-store")
        {
            Mock<IProductSessionStore> store = new();
            if (state == "throws")
            {
                store.Setup(s => s.GetAsync()).ThrowsAsync(new InvalidOperationException("storage unavailable"));
            }
            else
            {
                (string Access, string Refresh, DateTimeOffset Expires)? credentials = state == "missing"
                    ? null
                    : ("access", "refresh", state == "expired" ? Now.AddMinutes(-1) : state == "skew-boundary" ? Now.AddMinutes(1) : Now.AddHours(1));
                store.Setup(s => s.GetAsync()).ReturnsAsync(credentials);
            }

            services.AddSingleton(store.Object);
        }

        return services.BuildServiceProvider();
    }

    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class ProbeHandler(Exception? failure = null, bool succeedOnSecondSend = false) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        public string? Authorization { get; private set; }

        public CancellationToken Token { get; private set; }

        public HttpResponseMessage Response { get; } = new(HttpStatusCode.NoContent);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Authorization = request.Headers.Authorization?.ToString();
            Token = cancellationToken;
            return failure is not null && (!succeedOnSecondSend || Calls == 1)
                ? Task.FromException<HttpResponseMessage>(failure)
                : Task.FromResult(Response);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Response.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
