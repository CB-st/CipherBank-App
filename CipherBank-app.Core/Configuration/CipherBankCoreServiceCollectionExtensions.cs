// <copyright file="CipherBankCoreServiceCollectionExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Animations;
using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Pos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

    private static void AddCipherBankCoreOptions(
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
                static options => options.MaxConcurrency is >= SyncSchedulerOptions.MinConcurrency
                    and <= SyncSchedulerOptions.MaxAllowedConcurrency,
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
            .ValidateOnStart();
        services.AddOptions<CoraOptions>()
            .Bind(configuration.GetSection(CoraOptions.SectionName));
        services.AddOptions<CarouselLayoutConfig>()
            .Bind(configuration.GetSection(CarouselLayoutConfig.SectionName));
    }

    private static void AddCipherBankCoreServices(
        this IServiceCollection services,
        string databaseDirectory)
    {
        services.AddSingleton<ICryptoBox, AesGcmCryptoBox>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICoraLineProvider, CoraLineProvider>();
        services.AddSingleton<IEmvExchangeSimulator, EmvExchangeSimulator>();
        services.AddSingleton<ISyncJobScheduler>(static provider => new SyncJobScheduler(
            TaskScheduler.Default,
            provider.GetRequiredService<IOptions<SyncSchedulerOptions>>().Value));
        services.AddSingleton<ILocalDb>(provider =>
        {
            PersistenceOptions options = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            return new LocalDb(Path.Combine(databaseDirectory, options.DatabaseName));
        });
        services.AddSingleton<IMarketRepository, MarketRepository>();
        services.AddSingleton<IPrefsStore, PrefsStore>();
        services.AddSingleton<IRatesCache, RatesCache>();
        services.AddSingleton<IRecipientRepository, RecipientRepository>();
        services.AddSingleton<IWalletRepository, WalletRepository>();
    }
}
