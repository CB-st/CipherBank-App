// <copyright file="HttpClientExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using System.Net.Http;
using System.Reflection;
using CipherBank_app.Services;
using CipherBank_app.Services.Handlers;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace CipherBank_app.Extensions;

/// <summary>
/// Extension methods for registering CipherBank HTTP clients with shared configuration.
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Registers a typed HttpClient with CipherBank's standard configuration:
    /// certificate pinning, rate limiting, auth headers, and resilience.
    /// </summary>
    public static IHttpClientBuilder AddCipherBankHttpClient<TClient>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient>? configure = null)
        where TClient : class
    {
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        var builder = services.AddHttpClient<TClient>((sp, http) =>
        {
            var settings = sp.GetRequiredService<ISettingsService>();
            http.BaseAddress = new Uri(settings.CipherBankEndpointBase);
            http.Timeout = TimeSpan.FromSeconds(30);
            http.DefaultRequestHeaders.Add("Accept", "application/json");
#if DEBUG
            http.DefaultRequestHeaders.Add("X-Client-Version", appVersion);
            http.DefaultRequestHeaders.Add("X-Platform", DeviceInfo.Platform.ToString());
#endif
            configure?.Invoke(sp, http);
        })
        .ConfigurePrimaryHttpMessageHandler(() => PlatformHttpHandlerFactory.CreateHandler())
        .AddHttpMessageHandler(sp => new RateLimitingHandler(sp))
        .AddHttpMessageHandler(sp => new AuthHeaderHandler(sp));

        builder.AddStandardResilienceHandler(ConfigureResilienceOptions);

        return builder;
    }

    /// <summary>
    /// Registers the HealthCheck named HttpClient with certificate pinning for connection testing.
    /// </summary>
    public static IServiceCollection AddHealthCheckClient(this IServiceCollection services)
    {
        services.AddHttpClient("HealthCheck")
            .ConfigurePrimaryHttpMessageHandler(() => PlatformHttpHandlerFactory.CreateHandler());
        services.AddTransient<IHealthCheckClient, HealthCheckClient>();
        return services;
    }

    private static void ConfigureResilienceOptions(HttpStandardResilienceOptions options)
    {
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
