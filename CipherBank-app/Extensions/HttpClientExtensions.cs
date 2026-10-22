// <copyright file="HttpClientExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Reflection;
using CipherBank_app.Configuration;
using CipherBank_app.Services;
using CipherBank_app.Services.Handlers;
using Microsoft.Extensions.Options;

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
    /// <typeparam name="TClient">The typed client to register.</typeparam>
    public static IHttpClientBuilder AddCipherBankHttpClient<TClient>(
        this IServiceCollection services,
        Action<IServiceProvider, HttpClient>? configure = null)
        where TClient : class
    {
        var appVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

        IHttpClientBuilder builder = services.AddHttpClient<TClient>((sp, http) =>
        {
            ISettingsService settings = sp.GetRequiredService<ISettingsService>();
            http.BaseAddress = new Uri(settings.CipherBankEndpointBase);
            http.DefaultRequestHeaders.Add("Accept", "application/json");
            if (sp.GetRequiredService<IOptions<HostBehaviorOptions>>().Value.IncludeDiagnosticHeaders)
            {
                http.DefaultRequestHeaders.Add("X-Client-Version", appVersion);
                http.DefaultRequestHeaders.Add("X-Platform", DeviceInfo.Platform.ToString());
            }

            configure?.Invoke(sp, http);
        })
        .ConfigurePrimaryHttpMessageHandler(sp =>
            sp.GetRequiredService<IPlatformHttpMessageHandlerFactory>().CreateHandler())
        .AddHttpMessageHandler(sp => new RateLimitingHandler(sp))
        .AddHttpMessageHandler(sp => new AuthHeaderHandler(sp));

        builder.AddCipherBankResilience();

        return builder;
    }

    /// <summary>
    /// Registers the HealthCheck named HttpClient with certificate pinning for connection testing.
    /// </summary>
    public static IServiceCollection AddHealthCheckClient(this IServiceCollection services)
    {
        services.AddHttpClient("HealthCheck")
            .ConfigurePrimaryHttpMessageHandler(sp =>
                sp.GetRequiredService<IPlatformHttpMessageHandlerFactory>().CreateHandler());
        services.AddTransient<IHealthCheckClient, HealthCheckClient>();
        return services;
    }
}
