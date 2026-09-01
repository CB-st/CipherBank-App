// <copyright file="CipherBankCoreServiceRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Persist;
using CipherBank_app.Pos;
using CipherBank_app.V1;
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
        services.AddSingleton<ISyncJobScheduler>(static provider => new SyncJobScheduler(
            TaskScheduler.Default,
            provider.GetRequiredService<IOptions<SyncSchedulerOptions>>().Value));
        services.AddSingleton<ILocalDb>(provider =>
        {
            PersistenceOptions options = provider.GetRequiredService<IOptions<PersistenceOptions>>().Value;
            return new LocalDb(new FileInfo(Path.Combine(databaseDirectory, options.DatabaseName)));
        });
        services.AddSingleton<IMarketRepository, MarketRepository>();
        services.AddSingleton<IPrefsStore, PrefsStore>();
        services.AddSingleton<IRatesCache, RatesCache>();
        services.AddSingleton<IRecipientRepository>(provider => new RecipientRepository(
            provider.GetRequiredService<ILocalDb>(),
            provider.GetRequiredService<IOptions<PersistenceOptions>>().Value,
            provider.GetRequiredService<TimeProvider>()));
        services.AddSingleton<IWalletRepository, WalletRepository>();

        // Production product wire: host (MauiProgram) registers HttpProductClient on the
        // pinned/rate-limited pipeline. Isolated Core tests construct the client directly.
        services.AddSingleton<ISessionProofBuilder, LabSessionProofBuilder>();
        services.AddSingleton<IProductSessionStore, InMemoryProductSessionStore>();
        services.AddTransient<ProductAuthHeaderHandler>();
    }
}
