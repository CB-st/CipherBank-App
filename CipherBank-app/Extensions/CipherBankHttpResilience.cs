// <copyright file="CipherBankHttpResilience.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace CipherBank_app.Extensions;

/// <summary>
/// Resilience pipeline shared by CipherBank HTTP clients.
/// </summary>
internal static class CipherBankHttpResilience
{
    /// <summary>
    /// Adds the standard resilience handler and lets its total timeout own the deadline.
    /// </summary>
    /// <param name="builder">The HTTP client builder.</param>
    /// <returns>The same builder, so registration can be chained.</returns>
    internal static IHttpClientBuilder AddCipherBankResilience(this IHttpClientBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.AddStandardResilienceHandler(ConfigureResilienceOptions);

        // HttpClient.Timeout is outside the resilience pipeline. A finite value cancels
        // the call before TotalRequestTimeout (60s) can finish retries.
        // https://github.com/dotnet/extensions/issues/4770
        builder.ConfigureHttpClient(static (_, http) => http.Timeout = Timeout.InfiniteTimeSpan);
        return builder;
    }

    internal static void ConfigureResilienceOptions(HttpStandardResilienceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromSeconds(1);
        options.Retry.BackoffType = DelayBackoffType.Exponential;
        options.Retry.UseJitter = true;
        options.Retry.ShouldHandle = args => ValueTask.FromResult(
            args.Outcome.Exception is HttpRequestException ||
            args.Outcome.Result?.StatusCode is HttpStatusCode.ServiceUnavailable or
                HttpStatusCode.GatewayTimeout or
                HttpStatusCode.RequestTimeout or
                HttpStatusCode.TooManyRequests ||
            (int?)args.Outcome.Result?.StatusCode >= 500);

        options.CircuitBreaker.FailureRatio = 0.5;
        options.CircuitBreaker.MinimumThroughput = 10;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(15);
    }
}
