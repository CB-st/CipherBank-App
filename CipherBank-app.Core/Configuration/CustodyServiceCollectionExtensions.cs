// <copyright file="CustodyServiceCollectionExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Wallets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Configuration;

/// <summary>
/// Registers the custody slice. The host registers <see cref="ISecureStore"/>,
/// <see cref="IStepUpChallenges"/>, and the wallet repository before resolving seeded wallets.
/// </summary>
public static class CustodyServiceCollectionExtensions
{
    /// <summary>
    /// Binds the frozen cryptography profile and registers custody, PIN, backup, step-up, and wallet seeding.
    /// Use: High (MAUI startup and composition tests). Scope: custody feature.
    /// </summary>
    /// <param name="services">Host service collection.</param>
    /// <param name="configuration">Defaults plus any later deployment overrides.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddCustodyServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<CryptographyOptions>, CryptographyOptionsValidator>();
        services.AddRequiredOptions(configuration, new CryptographyOptions())
            .ValidateOnStart();
        services.AddSingleton<ICryptoBox, AesGcmCryptoBox>();
        services.AddSingleton<IPinService, PinService>();
        services.AddSingleton<ICustodyService, CustodyService>();
        services.AddSingleton<IMnemonicBackupService, MnemonicBackupService>();
        services.AddSingleton<IStepUpAuth, StepUpAuthService>();
        services.AddSingleton<ILocalWalletSeeder, LocalWalletSeeder>();
        return services;
    }
}
