// <copyright file="UserDataServiceCollectionExtensions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.V1;
using Microsoft.Extensions.DependencyInjection;

namespace CipherBank_app.UserData;

/// <summary>
/// DI helpers for pack-backed prefs sync (replaces plaintext <see cref="PrefsSyncService"/> registration).
/// </summary>
public static class UserDataServiceCollectionExtensions
{
    /// <summary>
    /// Registers dual-write <see cref="UserDataPrefsSyncService"/> with mock userdata client defaults.
    /// Use: Low (composition root). Scope: Shell MauiProgram / test hosts.
    /// </summary>
    public static IServiceCollection AddUserDataPrefsSync(this IServiceCollection services)
        => AddUserDataPrefsSync(services, UserDataPrefsSyncOptions.DualWrite());

    /// <summary>
    /// Registers pack prefs sync with the given migration options and a mock userdata client.
    /// Use: Low (composition root). Scope: Shell MauiProgram / test hosts.
    /// </summary>
    public static IServiceCollection AddUserDataPrefsSync(
        this IServiceCollection services,
        UserDataPrefsSyncOptions options)
        => AddUserDataPrefsSync(services, options, static _ => new MockUserDataClient());

    /// <summary>
    /// Registers pack prefs sync, options, meta store, mutable account context, and userdata client.
    /// Requires <see cref="Persist.IPrefsStore"/> and <see cref="IProductApi"/> already registered.
    /// Shell must populate <see cref="MutableUserDataAccountContext"/> (or replace
    /// <see cref="IUserDataAccountContext"/>) after unlock with username + mnemonic.
    /// Use: Low (composition root). Scope: Shell MauiProgram / test hosts.
    /// </summary>
    public static IServiceCollection AddUserDataPrefsSync(
        this IServiceCollection services,
        UserDataPrefsSyncOptions options,
        Func<IServiceProvider, IUserDataClient> userDataClientFactory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(userDataClientFactory);

        services.AddSingleton(options);
        services.AddSingleton<IUserDataPackMetaStore, InMemoryUserDataPackMetaStore>();
        services.AddSingleton<MutableUserDataAccountContext>();
        services.AddSingleton<IUserDataAccountContext>(
            static sp => sp.GetRequiredService<MutableUserDataAccountContext>());
        services.AddSingleton(userDataClientFactory);
        services.AddSingleton<IPrefsSyncService, UserDataPrefsSyncService>();
        return services;
    }
}
