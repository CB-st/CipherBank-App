// <copyright file="PersistenceFeatureExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Persist;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
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

        services.AddRequiredOptions(configuration, new PersistenceOptions())
            .Validate(static options => options.IsValid(), "Persistence options are invalid.")
            .ValidateOnStart();
        services.AddRequiredOptions(configuration, new SyncSchedulerOptions())
            .Validate(
                static options => options.MaxConcurrency == 0
                    || (options.MaxConcurrency >= SyncSchedulerOptions.MinConcurrency
                        && options.MaxConcurrency <= SyncSchedulerOptions.MaxAllowedConcurrency),
                "SyncScheduler options are invalid.")
            .ValidateOnStart();
        services.AddRequiredOptions(configuration, new UserPreferenceDefaultsOptions())
            .Validate(static options => options.IsValid(), "User preference defaults are invalid.")
            .ValidateOnStart();

        services.AddSingleton(provider => new FileInfo(Path.Combine(
            databaseDirectory.FullName,
            provider.GetRequiredService<IOptions<PersistenceOptions>>().Value.DatabaseName)));
        services.AddDbContextFactory<CipherBankDbContext>((provider, options) =>
        {
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = provider.GetRequiredService<FileInfo>().FullName,
            }.ToString();
            options.UseSqlite(connectionString);
        });
        services.AddSingleton<ILocalDatabaseInitializer, LocalDatabaseInitializer>();
        services.AddSingleton<IPrefsStore, PrefsStore>();
        services.AddSingleton<IWalletRepository, WalletRepository>();
        services.AddSingleton<IRecipientRepository, RecipientRepository>();
        services.AddSingleton<IRecipientSeedInitializer, RecipientSeedInitializer>();
        services.AddSingleton<AppStartupCoordinator>();
        services.AddSingleton<IRateSnapshotStore, SqliteRateSnapshotStore>();
        services.AddSingleton<IMarketRepository, MarketRepository>();
        services.AddSingleton<ISingleFlightJobFactory, SingleFlightJobFactory>();
        services.AddSingleton<IPrioritizedJobDispatcher, PrioritizedJobDispatcher>();
        services.AddSingleton<ISyncJobScheduler, SyncJobScheduler>();
        services.AddSingleton<MarketRateHydrator>();
        return services;
    }
}
