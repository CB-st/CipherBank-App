// <copyright file="CipherBankCoreOptionsRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
                static options => options.IsValid() && options.MatchesPersistedProfile(),
                OptionsValidationMessages.CryptographyUnsafe)
            .ValidateOnStart();
        services.AddOptions<SyncSchedulerOptions>()
            .Bind(configuration.GetSection(SyncSchedulerOptions.SectionName))
            .Validate(
                static options => options.MaxConcurrency == 0
                    || (options.MaxConcurrency >= SyncSchedulerOptions.MinConcurrency
                        && options.MaxConcurrency <= SyncSchedulerOptions.MaxAllowedConcurrency),
                OptionsValidationMessages.SyncConcurrencyOutOfRange)
            .ValidateOnStart();
        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .Validate(
                static options => !string.IsNullOrWhiteSpace(options.DatabaseName),
                OptionsValidationMessages.DatabaseNameRequired)
            .Validate(
                static options => Path.GetFileName(options.DatabaseName) == options.DatabaseName,
                OptionsValidationMessages.DatabaseNameMustBeFileName)
            .Validate(
                static options => options.AreDefaultRecipientsValid(),
                OptionsValidationMessages.DefaultRecipientsInvalid)
            .ValidateOnStart();
        services.AddRequiredOptions(configuration, new UserPreferenceDefaultsOptions())
            .Validate(static options => options.IsValid(), "User preference defaults are invalid.")
            .ValidateOnStart();
        services.AddRequiredOptions(configuration, new HostBehaviorOptions())
            .Validate(static options => options.IsValid(), "Host behavior options are invalid.")
            .ValidateOnStart();
        services.AddOptions<CoraOptions>()
            .Bind(configuration.GetSection(CoraOptions.SectionName));
        services.AddOptions<CarouselLayoutConfig>()
            .Bind(configuration.GetSection(CarouselLayoutConfig.SectionName));
        services.AddOptions<NetworkOptions>()
            .Bind(configuration.GetSection(NetworkOptions.SectionName));
    }
}
