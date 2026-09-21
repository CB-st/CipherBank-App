// <copyright file="TemporaryDatabase.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CipherBank_app.Tests.Persist;

/// <summary>Test-owned file database using production EF initialization and contexts.</summary>
public sealed class TemporaryDatabase :
    IDbContextFactory<CipherBankDbContext>,
    IAsyncDisposable,
    IDisposable
{
    private readonly FileInfo _file;
    private readonly DbContextOptions<CipherBankDbContext> _options;
    private readonly LocalDatabaseInitializer _initializer;

    public TemporaryDatabase(FileInfo file)
    {
        _file = file;
        string connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = file.FullName,
        }.ToString();
        _options = new DbContextOptionsBuilder<CipherBankDbContext>()
            .UseSqlite(connectionString)
            .Options;
        _initializer = new LocalDatabaseInitializer(this, file);
    }

    public string Path => _file.FullName;

    public Task InitializeAsync(CancellationToken cancellationToken = default) =>
        _initializer.InitializeAsync(cancellationToken);

    public ValueTask<CipherBankDbContext> CreateDbContextAsync(
        CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(new CipherBankDbContext(_options));

    public ValueTask<CipherBankDbContext> CreateContextAsync(
        CancellationToken cancellationToken = default) =>
        CreateDbContextAsync(cancellationToken);

    public CipherBankDbContext CreateDbContext() => new(_options);

    public void Dispose() => Cleanup();

    public ValueTask DisposeAsync()
    {
        Cleanup();
        return ValueTask.CompletedTask;
    }

    private void Cleanup()
    {
        SqliteConnection.ClearAllPools();
        foreach (string candidate in new[] { Path, Path + "-wal", Path + "-shm" })
        {
            File.Delete(candidate);
        }
    }
}
