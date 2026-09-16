// <copyright file="PersistenceFeatureExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Persist;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Extensions;

/// <summary>Registers the M2 persistence feature as one explicit composition unit.</summary>
public static class PersistenceFeatureExtensions
{
    /// <summary>
    /// Binds validated persistence options and registers focused persistence capabilities.
    /// Use: High (MAUI startup). Scope: application service collection.
    /// </summary>
    public static IServiceCollection AddPersistenceFeature(
        this IServiceCollection services,
        IConfiguration configuration,
        DirectoryInfo databaseDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(databaseDirectory);

        services.AddOptions<PersistenceOptions>()
            .Bind(configuration.GetSection(PersistenceOptions.SectionName))
            .Validate(static options => options.IsValid(), "Persistence options are invalid.")
            .ValidateOnStart();
        services.AddOptions<SyncSchedulerOptions>()
            .Bind(configuration.GetSection(SyncSchedulerOptions.SectionName))
            .Validate(
                static options => options.MaxConcurrency == 0
                    || (options.MaxConcurrency >= SyncSchedulerOptions.MinConcurrency
                        && options.MaxConcurrency <= SyncSchedulerOptions.MaxAllowedConcurrency),
                "SyncScheduler options are invalid.")
            .ValidateOnStart();

        services.AddSingleton(static provider => provider.GetRequiredService<IOptions<PersistenceOptions>>().Value);
        services.AddSingleton(static provider => provider.GetRequiredService<IOptions<SyncSchedulerOptions>>().Value);
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<ILocalDb>(provider =>
        {
            PersistenceOptions options = provider.GetRequiredService<PersistenceOptions>();
            return new LocalDb(new FileInfo(Path.Combine(databaseDirectory.FullName, options.DatabaseName)));
        });
        services.AddSingleton<IPrefsStore, PrefsStore>();
        services.AddSingleton<IWalletRepository, WalletRepository>();
        services.AddSingleton<IRecipientRepository, RecipientRepository>();
        services.AddSingleton<IRecipientSeedInitializer, RecipientSeedInitializer>();
        services.AddSingleton<IRatesCache, RatesCache>();
        services.AddSingleton<IMarketRepository, MarketRepository>();
        services.AddSingleton<ISingleFlightJobFactory, SingleFlightJobFactory>();
        services.AddSingleton<IPrioritizedJobDispatcher, PrioritizedJobDispatcher>();
        services.AddSingleton<ISyncJobScheduler, SyncJobScheduler>();
        services.AddSingleton<MarketRateHydrator>();
        return services;
    }
}
