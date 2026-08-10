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

        services.AddOptions<CryptographyOptions>()
            .Bind(configuration.GetSection(CryptographyOptions.SectionName))
            .Validate(options => options.IsValid(), "Cryptography parameters are unsafe or blob-incompatible.")
            .ValidateOnStart();
        services.AddOptions<SyncSchedulerOptions>()
            .Bind(configuration.GetSection(SyncSchedulerOptions.SectionName))
            .Validate(options => options.MaxConcurrency is >= 1 and <= 8, "Sync concurrency must be between 1 and 8.")
            .ValidateOnStart();
        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName), "DatabaseName is required.")
            .Validate(options => Path.GetFileName(options.DatabaseName) == options.DatabaseName, "DatabaseName must not contain a path.")
            .ValidateOnStart();
        services.AddOptions<CoraOptions>()
            .Bind(configuration.GetSection(CoraOptions.SectionName));
        services.AddOptions<CarouselLayoutConfig>()
            .Bind(configuration.GetSection(CarouselLayoutConfig.SectionName));

        services.AddSingleton<ICryptoBox, AesGcmCryptoBox>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICoraLineProvider, CoraLineProvider>();
        services.AddSingleton<IEmvExchangeSimulator, EmvExchangeSimulator>();
        services.AddSingleton<ISyncJobScheduler>(provider => new SyncJobScheduler(
            TaskScheduler.Default,
            provider.GetRequiredService<IOptions<SyncSchedulerOptions>>().Value));
        services.AddSingleton<ILocalDb>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            return new LocalDb(Path.Combine(databaseDirectory, options.DatabaseName));
        });
        services.AddSingleton<IMarketRepository, MarketRepository>();
        services.AddSingleton<IPrefsStore, PrefsStore>();
        services.AddSingleton<IRatesCache, RatesCache>();
        services.AddSingleton<IRecipientRepository, RecipientRepository>();
        services.AddSingleton<IWalletRepository, WalletRepository>();

        return services;
    }
}
