// <copyright file="CipherBankCoreOptionsRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Animations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Configuration;

/// <summary>Binds and validates typed Core options from configuration.</summary>
internal static class CipherBankCoreOptionsRegistration
{
    /// <summary>
    /// Registers Core options with start-time validation.
    /// Persistence, sync, and preference defaults are bound here because this composition
    /// root is self-contained for Core tests. The MAUI host binds those three through
    /// <c>AddPersistenceFeature</c> and must not also call <c>AddCipherBankCore</c>.
    /// Use: Low (host startup). Scope: Core DI.
    /// </summary>
    internal static void AddCipherBankCoreOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<CryptographyOptions>, CryptographyOptionsValidator>();
        services.AddRequiredOptions(configuration, new CryptographyOptions())
            .ValidateOnStart();
        services.AddRequiredOptions(configuration, new SyncSchedulerOptions())
            .Validate(
                static options => options.MaxConcurrency == 0
                    || (options.MaxConcurrency >= SyncSchedulerOptions.MinConcurrency
                        && options.MaxConcurrency <= SyncSchedulerOptions.MaxAllowedConcurrency),
                OptionsValidationMessages.SyncConcurrencyOutOfRange)
            .ValidateOnStart();
        services.AddRequiredOptions(configuration, new PersistenceOptions())
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
            .Validate(
                static options => options.IsValid(),
                "User preference defaults are invalid.")
            .ValidateOnStart();
        services.AddRequiredOptions(configuration, new CoraOptions());
        services.AddRequiredOptions(configuration, new CarouselLayoutConfig());
    }
}
