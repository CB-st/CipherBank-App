// <copyright file="CipherBankHttpResilienceTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Diagnostics;
using System.Net;
using CipherBank_app.Extensions;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
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
        var options = new HttpStandardResilienceOptions();

        CipherBankHttpResilience.ConfigureResilienceOptions(options);

        options.TotalRequestTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(60));
        options.AttemptTimeout.Timeout.Should().Be(TimeSpan.FromSeconds(15));
        options.Retry.MaxRetryAttempts.Should().Be(3);
    }

    [Fact]
    public async Task SlowAttempts_RetryInsideSixtySecondTotalBudget()
    {
        // Two 11s 503 responses plus one 11s success exceed the old 30s HttpClient.Timeout
        // and stay inside the 60s total request budget. Each attempt stays under the 15s attempt timeout.
        var handler = new SlowUnavailableHandler(TimeSpan.FromSeconds(11));
        ServiceCollection services = new();
        services.AddHttpClient(
                "cipherbank",
                static client => client.BaseAddress = new Uri("http://127.0.0.1/"))
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddCipherBankResilience();

        await using ServiceProvider provider = services.BuildServiceProvider();
        HttpClient client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("cipherbank");
        client.Timeout.Should().Be(Timeout.InfiniteTimeSpan);

        var stopwatch = Stopwatch.StartNew();
        using HttpResponseMessage response = await client.GetAsync(new Uri("/", UriKind.Relative));
        stopwatch.Stop();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        handler.Attempts.Should().Be(3);
        stopwatch.Elapsed.Should().BeGreaterThan(TimeSpan.FromSeconds(30));
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(60));
    }

    private sealed class SlowUnavailableHandler(TimeSpan attemptDelay) : HttpMessageHandler
    {
        private int _attempts;

        public int Attempts => Volatile.Read(ref _attempts);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            int attempt = Interlocked.Increment(ref _attempts);
            await Task.Delay(attemptDelay, cancellationToken);
            HttpStatusCode status = attempt < 3
                ? HttpStatusCode.ServiceUnavailable
                : HttpStatusCode.OK;
            return new HttpResponseMessage(status);
        }
    }
}
