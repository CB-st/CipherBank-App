// <copyright file="CipherBankCoreServiceRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
            return new LocalDb(Path.Combine(databaseDirectory, options.DatabaseName));
        });
        services.AddSingleton<IMarketRepository, MarketRepository>();
        services.AddSingleton<IPrefsStore, PrefsStore>();
        services.AddSingleton<IRatesCache, RatesCache>();
        services.AddSingleton<IRecipientRepository, RecipientRepository>();
        services.AddSingleton<IWalletRepository, WalletRepository>();

        // Production product wire: HTTP client (host sets BaseAddress). Lab uses InMemory in tests only.
        services.AddSingleton<ISessionProofBuilder, LabSessionProofBuilder>();
        services.AddSingleton<IProductSessionStore, InMemoryProductSessionStore>();
        services.AddTransient<ProductAuthHeaderHandler>();
        services.AddHttpClient<HttpProductClient>()
            .AddHttpMessageHandler<ProductAuthHeaderHandler>();
        services.AddTransient<IProductClient>(static sp => sp.GetRequiredService<HttpProductClient>());
    }
}
