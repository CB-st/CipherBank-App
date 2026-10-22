// <copyright file="CipherBankCoreServiceRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Cora;
using CipherBank_app.Custody;
using CipherBank_app.Pos;
using Microsoft.Extensions.DependencyInjection;

namespace CipherBank_app.Configuration;

/// <summary>Registers platform-neutral Core service implementations.</summary>
internal static class CipherBankCoreServiceRegistration
{
    // Rebase note: M2 now uses class-named .jsonc options, IOptions<T>,
    // IDbContextFactory + LocalDatabaseInitializer, configured preference defaults,
    // LocalWalletDescriptor, PricePoint persistence, and SqliteRateSnapshotStore.

    /// <summary>
    /// Registers crypto, persistence, sync dispatch, Cora copy, EMV simulation, and product session services.
    /// Use: Low (host startup). Scope: Core DI.
    /// </summary>
    internal static void AddCipherBankCoreServices(
        this IServiceCollection services,
        string databaseDirectory)
    {
        // Full custody registration, including this box, is AddCustodyFeature at the MAUI root.
        // This test root only needs the box; pin and step-up require platform ports.
        services.AddSingleton<ICryptoBox, AesGcmCryptoBox>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<ICoraLineProvider, CoraLineProvider>();
        services.AddSingleton<IEmvExchangeSimulator, EmvExchangeSimulator>();
        services.AddCipherBankPersistence(databaseDirectory);
        services.AddCipherBankSync();
        services.AddProductSessionServices();
    }
}
