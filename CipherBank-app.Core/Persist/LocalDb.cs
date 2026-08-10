// <copyright file="LocalDb.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist.Sql;
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
        var connectionString = new SqliteConnectionStringBuilder { DataSource = _path }.ToString();
        _options = new DbContextOptionsBuilder<CipherBankDbContext>()
            .UseSqlite(connectionString)
            .Options;
    }

    public string Path => _path;

    public async Task InitializeAsync(CancellationToken ct = default)
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
            await using var context = new CipherBankDbContext(_options);
            await context.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);
            await LocalDbSql.ApplyCompatibilityAsync(context.Database.GetDbConnection(), ct).ConfigureAwait(false);
            _initialized = true;
        }
        finally
        {
            _initializeGate.Release();
        }
    }

    public async ValueTask<CipherBankDbContext> CreateContextAsync(CancellationToken ct = default)
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
}
