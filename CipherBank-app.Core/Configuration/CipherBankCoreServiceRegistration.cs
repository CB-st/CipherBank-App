// <copyright file="CipherBankCoreServiceRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Pos;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Configuration;

/// <summary>Registers platform-neutral Core service implementations.</summary>
internal static class CipherBankCoreServiceRegistration
{
    /// <summary>
    /// Registers crypto, persistence, sync dispatch, Cora copy, and EMV simulation services.
    /// Use: Low (host startup). Scope: Core DI.
    /// </summary>
    internal static void AddCipherBankCoreServices(
        this IServiceCollection services,
        string databaseDirectory)
    {
        services.AddSingleton<ICryptoBox, AesGcmCryptoBox>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICoraLineProvider, CoraLineProvider>();
        services.AddSingleton<IEmvExchangeSimulator, EmvExchangeSimulator>();
        services.AddSingleton(provider =>
        {
            PersistenceOptions options = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            return new FileInfo(Path.Combine(databaseDirectory, options.DatabaseName));
        });
        services.AddDbContextFactory<CipherBankDbContext>((provider, options) =>
        {
            string connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = provider.GetRequiredService<FileInfo>().FullName,
            }.ToString();
            options.UseSqlite(connectionString);
        });
        services.AddSingleton<ILocalDatabaseInitializer, LocalDatabaseInitializer>();
        services.AddSingleton<IRecipientSeedInitializer, RecipientSeedInitializer>();
        services.AddSingleton<AppStartupCoordinator>();
        services.AddSingleton<IMarketRepository, MarketRepository>();
        services.AddSingleton<IPrefsStore, PrefsStore>();
        services.AddSingleton<IRateSnapshotStore, SqliteRateSnapshotStore>();
        services.AddSingleton<IRecipientRepository, RecipientRepository>();
        services.AddSingleton<IWalletRepository, WalletRepository>();
        services.AddSingleton<ISingleFlightJobFactory, SingleFlightJobFactory>();
        services.AddSingleton<IPrioritizedJobDispatcher, PrioritizedJobDispatcher>();
        services.AddSingleton<ISyncJobScheduler, SyncJobScheduler>();
        services.AddSingleton<MarketRateHydrator>();
    }
}
