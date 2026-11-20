// <copyright file="ProductSessionServiceCollectionExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Session;
using CipherBank_app.V1;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CipherBank_app.Configuration;

/// <summary>Registers the product session slice shared by the MAUI host and the Core test root.</summary>
public static class ProductSessionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the session lifecycle, fail-closed proof default, store, and auth handler.
    /// The host registers <see cref="HttpProductClient"/> on its pinned, rate-limited pipeline.
    /// Use: High (MAUI startup and composition tests). Scope: product session feature.
    /// </summary>
    /// <param name="services">Host service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddProductSessionServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<ISessionProofBuilder, UnavailableSessionProofBuilder>();
        services.TryAddSingleton<IProductSessionStore, InMemoryProductSessionStore>();
        services.TryAddSingleton<ProductSessionAuthority>(provider =>
        {
            ServiceDescriptor effective = services.Last(d => d.ServiceType == typeof(IProductSessionStore));
            if (effective.Lifetime != ServiceLifetime.Singleton)
            {
                throw new InvalidOperationException("Product session backing must be a singleton.");
            }

            // Resolve only from the completed provider: M7 registers its secure store after Core.
            return ProductSessionAuthority.For(provider.GetRequiredService<IProductSessionStore>());
        });
        services.TryAddSingleton<IAppSession, AppSession>();
        services.TryAddSingleton<IProductSessionCoordinator, ProductSessionCoordinator>();
        services.TryAddSingleton<IStreamHub, StreamHub>();
        services.TryAddSingleton<IPrefsSyncService, PrefsSyncService>();
        services.TryAddSingleton<IAccountBootstrapService, AccountBootstrapService>();
        services.TryAddTransient<ProductAuthHeaderHandler>();
        return services;
    }

    /// <summary>Applies explicit lab policy before registering the shared session lifecycle.</summary>
    /// <param name="services">Host service collection.</param>
    /// <param name="useLabProof">Explicit opt-in; never inferred from mock services.</param>
    /// <param name="isDevelopment">Whether this host permits development-only proof.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddProductSessionServices(
        this IServiceCollection services,
        bool useLabProof,
        bool isDevelopment)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (useLabProof && !isDevelopment)
        {
            throw new InvalidOperationException("Lab session proof is forbidden in production.");
        }

        if (useLabProof)
        {
            // Preserve host secure proof ports, including when Core registered its fallback first.
            ServiceDescriptor? fallback = services.FirstOrDefault(descriptor =>
                descriptor.ServiceType == typeof(ISessionProofBuilder)
                && descriptor.ImplementationType == typeof(UnavailableSessionProofBuilder));
            if (fallback is not null)
            {
                services.Remove(fallback);
            }

            services.TryAddSingleton<ISessionProofBuilder, LabSessionProofBuilder>();
        }

        return services.AddProductSessionServices();
    }

    /// <summary>Registers the configured live stream or a fail-closed unavailable adapter.</summary>
    /// <param name="services">Host service collection.</param>
    /// <param name="endpoint">Deployment-supplied absolute wss endpoint.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddProductSessionStream(this IServiceCollection services, string? endpoint)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<IStreamService>(_ =>
            Uri.TryCreate(endpoint, UriKind.Absolute, out Uri? uri) && uri.Scheme == "wss"
                ? new ClientWebSocketStreamService(uri)
                : new UnavailableStreamService());
        return services;
    }
}
