// <copyright file="LocalDbMigrationTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class LocalDbMigrationTests
{
    [Fact]
    public async Task InitializeAsync_CleanDatabase_CreatesModelTablesAndMigrationHistory()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-migrate-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            LocalDb db = new LocalDb(path);
            await db.InitializeAsync();

            List<string> tables = await ListTablesAsync(path);
            tables.Should().Contain("rates_snapshot");
            tables.Should().Contain("sync_meta");
            tables.Should().Contain("ohlc");
            tables.Should().Contain("wallets");
            tables.Should().Contain("prefs");
            tables.Should().Contain("recipients");
            tables.Should().Contain("__EFMigrationsHistory");
        }
        finally
        {
            DeleteSqliteFiles(path);
        }
    }

    [Fact]
    public async Task InitializeAsync_SecondCall_IsIdempotent()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-migrate-idem-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            await using (LocalDb first = new LocalDb(path))
            {
                await first.InitializeAsync();
            }

            await using LocalDb second = new LocalDb(path);
            await second.InitializeAsync();

            await using CipherBankDbContext context = await second.CreateContextAsync();
            (await context.Database.GetAppliedMigrationsAsync()).Should().NotBeEmpty();
        }
        finally
        {
            DeleteSqliteFiles(path);
        }
    }

    [Fact]
    public async Task InitializeAsync_PrototypeDbWithoutHistory_IsReplaced()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-proto-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            await using (SqliteConnection leftover = new SqliteConnection(
                new SqliteConnectionStringBuilder { DataSource = path }.ToString()))
            {
                await leftover.OpenAsync();
                await using SqliteCommand create = leftover.CreateCommand();
                create.CommandText =
                    """
                    CREATE TABLE recipients (
                      id TEXT PRIMARY KEY,
                      name TEXT NOT NULL,
                      account TEXT,
                      created_at TEXT NOT NULL
                    );
                    INSERT INTO recipients (id, name, account, created_at)
                    VALUES ('legacy', 'Lab leftover', '88210001', '2026-08-09T00:00:00.0000000+00:00');
                    """;
                await create.ExecuteNonQueryAsync();
            }

            LocalDb db = new LocalDb(path);
            await db.InitializeAsync();

            List<string> tables = await ListTablesAsync(path);
            tables.Should().Contain("__EFMigrationsHistory");
            tables.Should().Contain("wallets");

            await using SqliteConnection inspect = new SqliteConnection(
                new SqliteConnectionStringBuilder { DataSource = path }.ToString());
            await inspect.OpenAsync();
            await using SqliteCommand count = inspect.CreateCommand();
            count.CommandText = "SELECT COUNT(*) FROM recipients";
            Convert.ToInt32(await count.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture)
                .Should().Be(0);
        }
        finally
        {
            DeleteSqliteFiles(path);
        }
    }

    private static async Task<List<string>> ListTablesAsync(string path)
    {
        await using SqliteConnection conn = new SqliteConnection(
            new SqliteConnectionStringBuilder { DataSource = path }.ToString());
        await conn.OpenAsync();
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
        List<string> tables = [];
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        return tables;
    }

    private static void DeleteSqliteFiles(string path)
    {
        foreach (string candidate in new[] { path, path + "-wal", path + "-shm" })
        {
            if (File.Exists(candidate))
            {
                File.Delete(candidate);
            }
        }
    }
}
