// <copyright file="LocalDb.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Persist;

/// <inheritdoc />
public sealed class LocalDb : ILocalDb, IAsyncDisposable, IDisposable
{
    private readonly string _path;
    private readonly DbContextOptions<CipherBankDbContext> _options;
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private bool _initialized;
    private bool _disposed;

    public LocalDb(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _path = System.IO.Path.GetFullPath(databasePath);
        string connectionString = new SqliteConnectionStringBuilder { DataSource = _path }.ToString();
        _options = new DbContextOptionsBuilder<CipherBankDbContext>()
            .UseSqlite(connectionString)
            .Options;
    }

    public string Path => _path;

    public Task InitializeAsync() => InitializeAsync(CancellationToken.None);

    /// <summary>
    /// Applies EF Core migrations. Prototype SQLite files without a migration history are deleted first.
    /// Use: Medium (startup). Scope: LocalDb.
    /// </summary>
    public async Task InitializeAsync(CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_initialized)
        {
            return;
        }

        await _initializeGate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized)
            {
                return;
            }

            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
            await DiscardUnmatchedPrototypeAsync(ct).ConfigureAwait(false);
            await using CipherBankDbContext context = new CipherBankDbContext(_options);
            await context.Database.MigrateAsync(ct).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initializeGate.Release();
        }
    }

    public ValueTask<CipherBankDbContext> CreateContextAsync()
        => CreateContextAsync(CancellationToken.None);

    public async ValueTask<CipherBankDbContext> CreateContextAsync(CancellationToken ct)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await InitializeAsync(ct).ConfigureAwait(false);
        return new CipherBankDbContext(_options);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _initializeGate.Dispose();
        _disposed = true;
    }

    public ValueTask DisposeAsync()
    {
        Dispose();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Deletes leftover lab databases that have no applied EF migrations.
    /// Use: Low (startup). Scope: LocalDb.
    /// </summary>
    private async Task DiscardUnmatchedPrototypeAsync(CancellationToken ct)
    {
        if (!File.Exists(_path))
        {
            return;
        }

        bool discard = true;
        try
        {
            await using CipherBankDbContext probe = new CipherBankDbContext(_options);
            IEnumerable<string> applied = await probe.Database.GetAppliedMigrationsAsync(ct).ConfigureAwait(false);
            discard = !applied.Any();
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
        foreach (string candidate in new[] { _path, _path + "-wal", _path + "-shm" })
        {
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
    }
}
