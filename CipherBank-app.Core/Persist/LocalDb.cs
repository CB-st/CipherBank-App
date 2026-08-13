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
    private readonly ILegacySchemaRepair _schemaRepair;
    private readonly SemaphoreSlim _initializeGate = new(1, 1);
    private bool _initialized;
    private bool _disposed;

    public LocalDb(string databasePath)
        : this(databasePath, new LocalDbSql())
    {
    }

    internal LocalDb(string databasePath, ILegacySchemaRepair schemaRepair)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        ArgumentNullException.ThrowIfNull(schemaRepair);
        _path = System.IO.Path.GetFullPath(databasePath);
        _schemaRepair = schemaRepair;
        string connectionString = new SqliteConnectionStringBuilder { DataSource = _path }.ToString();
        _options = new DbContextOptionsBuilder<CipherBankDbContext>()
            .UseSqlite(connectionString)
            .Options;
    }

    public string Path => _path;

    public Task InitializeAsync() => InitializeAsync(CancellationToken.None);

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
            await using CipherBankDbContext context = new CipherBankDbContext(_options);

            // EnsureCreated no-ops when any table already exists (legacy pre-EF DBs).
            await context.Database.EnsureCreatedAsync(ct).ConfigureAwait(false);
            await _schemaRepair.EnsureMissingModelTablesAsync(context, ct).ConfigureAwait(false);
            await _schemaRepair.ApplyCompatibilityAsync(context.Database.GetDbConnection(), ct).ConfigureAwait(false);
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
}
