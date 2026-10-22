// <copyright file="ProductSessionServiceCollectionExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.V1;
using Microsoft.Extensions.DependencyInjection;

namespace CipherBank_app.Configuration;

/// <summary>Registers the product session slice shared by the MAUI host and the Core test root.</summary>
public static class ProductSessionServiceCollectionExtensions
{
    /// <summary>
    /// Registers the session proof builder, the session store, and the product auth header handler.
    /// The host registers <see cref="HttpProductClient"/> on its pinned, rate-limited pipeline.
    /// Use: High (MAUI startup and composition tests). Scope: product session feature.
    /// </summary>
    /// <param name="services">Host service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddProductSessionServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<ISessionProofBuilder, LabSessionProofBuilder>();
        services.AddSingleton<IProductSessionStore, InMemoryProductSessionStore>();
        services.AddTransient<ProductAuthHeaderHandler>();
        return services;
    }
}
