// <copyright file="CipherBankCoreServiceCollectionExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CipherBank_app.Configuration;

/// <summary>Registers the Core implementations whose dependencies are platform-neutral.</summary>
public static class CipherBankCoreServiceCollectionExtensions
{
    /// <summary>
    /// Binds typed options and registers Core persistence, dispatch, copy, and simulation services.
    /// </summary>
    /// <param name="services">Host service collection.</param>
    /// <param name="configuration">Defaults plus any later deployment overrides.</param>
    /// <param name="databaseDirectory">Platform-owned application-data directory.</param>
    public static IServiceCollection AddCipherBankCore(
        this IServiceCollection services,
        IConfiguration configuration,
        string databaseDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseDirectory);

        services.AddCipherBankCoreOptions(configuration);
        services.AddCipherBankCoreServices(databaseDirectory);
        return services;
    }
}
