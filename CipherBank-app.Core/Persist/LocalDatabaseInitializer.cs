// <copyright file="LocalDatabaseInitializer.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc cref="ILocalDatabaseInitializer" />
public sealed class LocalDatabaseInitializer : ILocalDatabaseInitializer
{
    private readonly Lock _gate = new();
    private readonly IDbContextFactory<CipherBankDbContext> _contexts;
    private readonly FileInfo _databaseFile;
    private Task? _initialization;

    public LocalDatabaseInitializer(
        IDbContextFactory<CipherBankDbContext> contexts,
        FileInfo databaseFile)
    {
        ArgumentNullException.ThrowIfNull(contexts);
        ArgumentNullException.ThrowIfNull(databaseFile);
        _contexts = contexts;
        _databaseFile = databaseFile;
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (_initialization is null
                || _initialization.IsFaulted
                || _initialization.IsCanceled)
            {
                _initialization = InitializeCoreAsync(cancellationToken);
            }

            return _initialization;
        }
    }

    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        _databaseFile.Directory?.Create();
        await DiscardUnmatchedPrototypeAsync(cancellationToken).ConfigureAwait(false);
        CipherBankDbContext context = await _contexts
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        await using (context)
        {
            await context.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task DiscardUnmatchedPrototypeAsync(CancellationToken cancellationToken)
    {
        if (!_databaseFile.Exists)
        {
            return;
        }

        bool discard;
        try
        {
            CipherBankDbContext probe = await _contexts
                .CreateDbContextAsync(cancellationToken)
                .ConfigureAwait(false);
            await using (probe)
            {
                IEnumerable<string> applied = await probe.Database
                    .GetAppliedMigrationsAsync(cancellationToken)
                    .ConfigureAwait(false);
                discard = !applied.Any();
            }
        }
        catch (SqliteException)
        {
            discard = true;
        }

        if (!discard)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        foreach (string candidate in new[]
                 {
                     _databaseFile.FullName,
                     _databaseFile.FullName + "-wal",
                     _databaseFile.FullName + "-shm",
                 })
        {
            File.Delete(candidate);
        }
    }
}
