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
    public async Task InitializeAsync_CreatesRatesSnapshotAndSyncMetaTables()
    {
        var path = Path.Combine(Path.GetTempPath(), "cb-migrate-" + Guid.NewGuid().ToString("N") + ".db");
        var db = new LocalDb(path);
        await db.InitializeAsync();

        await using CipherBankDbContext context = await db.CreateContextAsync();
        var conn = (SqliteConnection)context.Database.GetDbConnection();
        await conn.OpenAsync();
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
        var tables = new List<string>();
        await using SqliteDataReader reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        tables.Should().Contain("rates_snapshot");
        tables.Should().Contain("sync_meta");
        tables.Should().Contain("ohlc");
    }

    [Fact]
    public async Task InitializeAsync_LegacyRecipientSchema_AddsMetadataAndScrubsCleartext()
    {
        var path = Path.Combine(Path.GetTempPath(), "cb-legacy-" + Guid.NewGuid().ToString("N") + ".db");
        await using (var legacy = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString()))
        {
            await legacy.OpenAsync();
            await using SqliteCommand create = legacy.CreateCommand();
            create.CommandText = """
                CREATE TABLE recipients (
                  id TEXT PRIMARY KEY,
                  name TEXT NOT NULL,
                  account TEXT,
                  routing TEXT,
                  created_at TEXT NOT NULL
                );
                INSERT INTO recipients (id, name, account, routing, created_at)
                VALUES ('legacy', 'Legacy payee', '88210001', '021000021', '2026-08-09T00:00:00.0000000+00:00');
                """;
            await create.ExecuteNonQueryAsync();
        }

        var db = new LocalDb(path);
        await db.InitializeAsync();

        await using CipherBankDbContext context = await db.CreateContextAsync();
        var connection = (SqliteConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using SqliteCommand inspect = connection.CreateCommand();
        inspect.CommandText = "SELECT account, routing, account_type FROM recipients WHERE id = 'legacy'";
        await using SqliteDataReader reader = await inspect.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.IsDBNull(0).Should().BeTrue();
        reader.IsDBNull(1).Should().BeTrue();
        reader.GetString(2).Should().Be("checking");
    }
}
