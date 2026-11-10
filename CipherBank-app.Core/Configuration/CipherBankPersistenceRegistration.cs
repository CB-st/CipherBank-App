// <copyright file="CipherBankPersistenceRegistration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Configuration;

/// <summary>Registers the local database, its initializers, and startup coordination.</summary>
internal static class CipherBankPersistenceRegistration
{
    /// <summary>
    /// Registers the database file, EF Core factory, seed initializers, and startup coordinator.
    /// Use: Low (host startup). Scope: Core persistence DI.
    /// </summary>
    internal static void AddCipherBankPersistence(
        this IServiceCollection services,
        string databaseDirectory)
    {
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
    }
}
