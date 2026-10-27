// <copyright file="CipherBankHttpResilienceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using CipherBank_app.Extensions;
using CipherBank_app.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Polly;
using Polly.Retry;
using Xunit;

namespace CipherBank_app.Tests.Services;

/// <summary>
/// Proves the resilience pipeline, not <see cref="HttpClient.Timeout"/>, owns the deadline.
/// </summary>
public class CipherBankHttpResilienceTests
{
    [Fact]
    public void ConfigureResilienceOptions_UsesSixtySecondTotalBudget()
    {
        HttpStandardResilienceOptions options = new();

        CipherBankHttpResilience.ConfigureResilienceOptions(options);

        options.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(60));
        options.AttemptTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(15));
        options.Retry.MaxRetryAttempts.Should().Be(3);
    }

    [Fact]
    public async Task SlowAttempts_RetryInsideSixtySecondTotalBudget()
    {
        FakeTimeProvider clock = new();
        TimeSpan competingTimeout = TimeSpan.FromSeconds(30);
        SlowUnavailableHandler handler = new(TimeSpan.FromSeconds(11), clock);
        ServiceCollection services = new();
        services.AddSingleton<TimeProvider>(clock);
        services.AddSingleton(Mock.Of<ISettingsService>(
            settings => settings.CipherBankEndpointBase == "http://127.0.0.1/"));
        services.AddSingleton(Mock.Of<IPlatformHttpMessageHandlerFactory>(
            factory => factory.CreateHandler() == handler));
        services.AddSingleton<RateLimiter>();
        IHttpClientBuilder builder = services.AddCipherBankHttpClient<TestHttpClient>(
            (_, client) => client.Timeout = competingTimeout);
        services.Configure<HttpStandardResilienceOptions>(
            $"{builder.Name}-standard",
            static options => options.Retry.DelayGenerator = static _ => ValueTask.FromResult<TimeSpan?>(TimeSpan.Zero));

        await using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<TestHttpClient>().Client;
        client.Timeout.Should().Be(Timeout.InfiniteTimeSpan);

        // Both the standard handler and our final callback must override a finite timeout.
        // Checking both also detects removal of the explicit final callback.
        HttpClientFactoryOptions clientOptions = provider.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>()
            .Get(builder.Name);
        clientOptions.HttpClientActions.Count.Should().BeGreaterThanOrEqualTo(2);
        foreach (Action<HttpClient> configureClient in clientOptions.HttpClientActions.TakeLast(2))
        {
            using HttpClient configuredClient = new();
            configuredClient.Timeout = competingTimeout;
            configureClient(configuredClient);
            configuredClient.Timeout.Should().Be(Timeout.InfiniteTimeSpan);
        }

        // Three controlled 11s attempts cross the competing client deadline while
        // each attempt stays under 15s and the entire request stays under 60s.
        long started = clock.GetTimestamp();
        using HttpResponseMessage response = await client.GetAsync(new Uri("/", UriKind.Relative));
        TimeSpan elapsed = clock.GetElapsedTime(started);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.Attempts.Should().Be(3);
        elapsed.Should().BeGreaterThan(competingTimeout);
        elapsed.Should().BeLessThan(TimeSpan.FromSeconds(60));
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, true)]
    [InlineData(HttpStatusCode.GatewayTimeout, true)]
    [InlineData(HttpStatusCode.RequestTimeout, true)]
    [InlineData(HttpStatusCode.TooManyRequests, true)]
    [InlineData(HttpStatusCode.InternalServerError, true)]
    [InlineData(HttpStatusCode.BadGateway, true)]
    [InlineData(HttpStatusCode.OK, false)]
    [InlineData(HttpStatusCode.NotFound, false)]
    public async Task ShouldHandle_ClassifiesHttpStatus(HttpStatusCode status, bool retry)
    {
        using HttpResponseMessage response = new(status);
        bool actual = await InvokeShouldHandle(Outcome.FromResult(response));
        actual.Should().Be(retry);
    }

    [Fact]
    public async Task ShouldHandle_RetriesHttpRequestException()
    {
        bool actual = await InvokeShouldHandle(
            Outcome.FromException<HttpResponseMessage>(new HttpRequestException()));
        actual.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldHandle_IgnoresUnrelatedExceptions()
    {
        // A null response (no HttpRequestException) is the ?. path on status checks.
        bool actual = await InvokeShouldHandle(
            Outcome.FromException<HttpResponseMessage>(new InvalidOperationException()));
        actual.Should().BeFalse();
    }

    private static async Task<bool> InvokeShouldHandle(Outcome<HttpResponseMessage> outcome)
    {
        HttpStandardResilienceOptions options = new();
        CipherBankHttpResilience.ConfigureResilienceOptions(options);
        ResilienceContext context = ResilienceContextPool.Shared.Get();
        try
        {
            return await options.Retry.ShouldHandle(
                new RetryPredicateArguments<HttpResponseMessage>(context, outcome, 0));
        }
        finally
        {
            ResilienceContextPool.Shared.Return(context);
        }
    }

    public sealed class TestHttpClient(HttpClient client)
    {
        public HttpClient Client { get; } = client;
    }

    private sealed class SlowUnavailableHandler(TimeSpan attemptDelay, FakeTimeProvider clock) : HttpMessageHandler
    {
        private int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            int attempt = Interlocked.Increment(ref _attempts);
            Task delay = Task.Delay(attemptDelay, clock, cancellationToken);
            clock.Advance(attemptDelay);
            await delay;
            HttpStatusCode status = attempt < 3
                ? HttpStatusCode.ServiceUnavailable
                : HttpStatusCode.OK;
            return new HttpResponseMessage(status);
        }
    }
}
