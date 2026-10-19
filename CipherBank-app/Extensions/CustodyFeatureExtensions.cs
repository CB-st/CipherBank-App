// <copyright file="CustodyFeatureExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Custody;
using CipherBank_app.Services;
using Microsoft.Extensions.Configuration;

namespace CipherBank_app.Extensions;

/// <summary>Registers the custody feature as one explicit composition unit.</summary>
public static class CustodyFeatureExtensions
{
    /// <summary>
    /// Registers the platform secure store and step-up prompts, then the custody services.
    /// Use: High (MAUI startup). Scope: application service collection.
    /// </summary>
    /// <param name="services">Host service collection.</param>
    /// <param name="configuration">Defaults plus any later deployment overrides.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddCustodyFeature(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<ISecureStore, SecureStorageStore>();
        services.AddSingleton<IStepUpChallenges, ShellStepUpChallenges>();
        return services.AddCustodyServices(configuration);
    }
}
