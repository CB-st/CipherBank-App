// <copyright file="CipherBankSyncRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using Microsoft.Extensions.DependencyInjection;

namespace CipherBank_app.Configuration;

/// <summary>Registers market, preference, recipient, wallet, and sync dispatch services.</summary>
internal static class CipherBankSyncRegistration
{
    /// <summary>
    /// Registers repositories, the rate snapshot store, and the prioritized sync dispatcher.
    /// Use: Low (host startup). Scope: Core sync DI.
    /// </summary>
    internal static void AddCipherBankSync(this IServiceCollection services)
    {
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
