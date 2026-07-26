// <copyright file="LocalDb.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using Microsoft.Data.Sqlite;

namespace CipherBank_app.Persist;

/// <summary>SQLite public environment (Cora persist schema).</summary>
public interface ILocalDb
{
    Task InitializeAsync();

    SqliteConnection Open();

    string Path { get; }
}

/// <inheritdoc />
public sealed class LocalDb : ILocalDb
{
    private readonly string _path;

    public LocalDb(string databasePath)
    {
        _path = databasePath;
    }

    public string Path => _path;

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
        await using SqliteConnection conn = Open();
        await conn.OpenAsync().ConfigureAwait(false);
        const string sql = """
            CREATE TABLE IF NOT EXISTS wallets (
              id TEXT PRIMARY KEY,
              symbol TEXT NOT NULL,
              label TEXT,
              address TEXT,
              path TEXT,
              account_index INTEGER NOT NULL DEFAULT 0,
              kind TEXT NOT NULL DEFAULT 'derived',
              created_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS prefs (
              key TEXT PRIMARY KEY,
              value TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS recipients (
              id TEXT PRIMARY KEY,
              name TEXT NOT NULL,
              account_mask TEXT,
              routing_mask TEXT,
              created_at TEXT NOT NULL
            );
            CREATE TABLE IF NOT EXISTS ohlc (
              symbol TEXT NOT NULL,
              t INTEGER NOT NULL,
              v REAL NOT NULL,
              PRIMARY KEY (symbol, t)
            );
            CREATE TABLE IF NOT EXISTS rates_snapshot (
              symbol TEXT PRIMARY KEY NOT NULL,
              usd REAL NOT NULL,
              change24h REAL NOT NULL DEFAULT 0,
              updated_at INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS sync_meta (
              key TEXT PRIMARY KEY NOT NULL,
              value TEXT NOT NULL,
              updated_at INTEGER NOT NULL
            );
            """;
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public SqliteConnection Open()
        => new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = _path }.ToString());
}
