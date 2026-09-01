// <copyright file="CipherBankCoreOptionsRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Animations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CipherBank_app.Configuration;

/// <summary>Binds and validates typed Core options from configuration.</summary>
internal static class CipherBankCoreOptionsRegistration
{
    /// <summary>
    /// Registers Cryptography / Sync / Persistence / Cora / Carousel options with start-time validation.
    /// Use: Low (host startup). Scope: Core DI.
    /// </summary>
    internal static void AddCipherBankCoreOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<CryptographyOptions>()
            .Bind(configuration.GetSection(CryptographyOptions.SectionName))
            .Validate(
                static options => options.IsValid(),
                ConfigurationValidationMessages.CryptographyUnsafe)
            .ValidateOnStart();
        services.AddOptions<SyncSchedulerOptions>()
            .Bind(configuration.GetSection(SyncSchedulerOptions.SectionName))
            .Validate(
                static options => options.MaxConcurrency == 0
                    || (options.MaxConcurrency >= SyncSchedulerOptions.MinConcurrency
                        && options.MaxConcurrency <= SyncSchedulerOptions.MaxAllowedConcurrency),
                ConfigurationValidationMessages.SyncConcurrencyOutOfRange)
            .ValidateOnStart();
        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.DatabaseName),
                ConfigurationValidationMessages.DatabaseNameRequired)
            .Validate(
                static options => Path.GetFileName(options.DatabaseName) == options.DatabaseName,
                ConfigurationValidationMessages.DatabaseNameMustBeFileName)
            .Validate(
                static options => options.AreDefaultRecipientsValid(),
                ConfigurationValidationMessages.DefaultRecipientsInvalid)
            .ValidateOnStart();
        services.AddOptions<CoraOptions>()
            .Bind(configuration.GetSection(CoraOptions.SectionName));
        services.AddOptions<CarouselLayoutConfig>()
            .Bind(configuration.GetSection(CarouselLayoutConfig.SectionName));
        services.AddOptions<NetworkOptions>()
            .Bind(configuration.GetSection(NetworkOptions.SectionName));
    }
}
