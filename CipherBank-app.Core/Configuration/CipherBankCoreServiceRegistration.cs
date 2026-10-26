// <copyright file="CipherBankCoreServiceRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Pos;
using CipherBank_app.V1;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Configuration;

/// <summary>Registers platform-neutral Core service implementations.</summary>
internal static class CipherBankCoreServiceRegistration
{
    // Rebase note: M2 now gates AppShell on LocalDatabaseInitializer then recipient
    // seeding, uses IDbContextFactory per operation, class-named .jsonc options,
    // generated-only migrations, and typed wallet/PricePoint/rate-store contracts.
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
        // FENCE: preserve startup ordering and short-lived repository contexts from main.
        // Migration and seeding finish before the shell opens; repositories never initialize schema.
        services.AddSingleton(provider => new FileInfo(Path.Combine(
            databaseDirectory,
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
        services.AddSingleton<AppStartupCoordinator>();
        services.AddSingleton<IMarketRepository, MarketRepository>();
        services.AddSingleton<IPrefsStore, PrefsStore>();
        services.AddSingleton<IRateSnapshotStore, SqliteRateSnapshotStore>();
        services.AddSingleton<IRecipientRepository, RecipientRepository>();
        services.AddSingleton<IRecipientSeedInitializer, RecipientSeedInitializer>();
        services.AddSingleton<IWalletRepository, WalletRepository>();
        services.AddSingleton(provider => UserPreferenceDefaults.FromOptions(
            provider.GetRequiredService<IOptions<UserPreferenceDefaultsOptions>>().Value));
        services.AddSingleton<ISingleFlightJobFactory, SingleFlightJobFactory>();
        services.AddSingleton<IPrioritizedJobDispatcher, PrioritizedJobDispatcher>();
        services.AddSingleton<ISyncJobScheduler, SyncJobScheduler>();
        services.AddSingleton<MarketRateHydrator>();

        // Production product wire: host (MauiProgram) registers HttpProductClient on the
        // pinned/rate-limited pipeline. Isolated Core tests construct the client directly.
        services.AddSingleton<ISessionProofBuilder, LabSessionProofBuilder>();
        services.AddSingleton<IProductSessionStore, InMemoryProductSessionStore>();
        services.AddTransient<ProductAuthHeaderHandler>();
    }
}
