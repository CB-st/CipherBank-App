// <copyright file="LocalDbMigrationTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
        string path = Path.Combine(Path.GetTempPath(), "cb-migrate-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();

        await using CipherBankDbContext context = await db.CreateContextAsync();
        SqliteConnection conn = (SqliteConnection)context.Database.GetDbConnection();
        await conn.OpenAsync();
        await using SqliteCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
        List<string> tables = new List<string>();
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
        string path = Path.Combine(Path.GetTempPath(), "cb-legacy-" + Guid.NewGuid().ToString("N") + ".db");
        await using (SqliteConnection legacy = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString()))
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

        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();

        await using CipherBankDbContext context = await db.CreateContextAsync();
        SqliteConnection connection = (SqliteConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using SqliteCommand inspect = connection.CreateCommand();
        inspect.CommandText =
            "SELECT account, routing, account_type, account_mask, routing_mask FROM recipients WHERE id = 'legacy'";
        await using SqliteDataReader reader = await inspect.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.IsDBNull(0).Should().BeTrue();
        reader.IsDBNull(1).Should().BeTrue();
        reader.GetString(2).Should().Be("checking");
        reader.GetString(3).Should().Be(AchRecipientValidation.MaskAccount("88210001"));
        reader.GetString(4).Should().Be(AchRecipientValidation.MaskRouting("021000021"));
    }

    [Fact]
    public async Task InitializeAsync_LegacyAccountOnly_PopulatesAccountMask()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-legacy-acct-" + Guid.NewGuid().ToString("N") + ".db");
        await using (SqliteConnection legacy = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString()))
        {
            await legacy.OpenAsync();
            await using SqliteCommand create = legacy.CreateCommand();
            create.CommandText = """
                CREATE TABLE recipients (
                  id TEXT PRIMARY KEY,
                  name TEXT NOT NULL,
                  account TEXT,
                  created_at TEXT NOT NULL
                );
                INSERT INTO recipients (id, name, account, created_at)
                VALUES ('acct', 'Acct only', '88210001', '2026-08-09T00:00:00.0000000+00:00');
                """;
            await create.ExecuteNonQueryAsync();
        }

        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();

        await using CipherBankDbContext context = await db.CreateContextAsync();
        SqliteConnection connection = (SqliteConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using SqliteCommand inspect = connection.CreateCommand();
        inspect.CommandText = "SELECT account, account_mask FROM recipients WHERE id = 'acct'";
        await using SqliteDataReader reader = await inspect.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.IsDBNull(0).Should().BeTrue();
        reader.GetString(1).Should().Be(AchRecipientValidation.MaskAccount("88210001"));
    }

    [Fact]
    public async Task InitializeAsync_LegacyRoutingOnly_PopulatesRoutingMask()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-legacy-routing-" + Guid.NewGuid().ToString("N") + ".db");
        await using (SqliteConnection legacy = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString()))
        {
            await legacy.OpenAsync();
            await using SqliteCommand create = legacy.CreateCommand();
            create.CommandText = """
                CREATE TABLE recipients (
                  id TEXT PRIMARY KEY,
                  name TEXT NOT NULL,
                  routing TEXT,
                  created_at TEXT NOT NULL
                );
                INSERT INTO recipients (id, name, routing, created_at)
                VALUES ('routing', 'Routing only', '021000021', '2026-08-09T00:00:00.0000000+00:00');
                """;
            await create.ExecuteNonQueryAsync();
        }

        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();

        await using CipherBankDbContext context = await db.CreateContextAsync();
        SqliteConnection connection = (SqliteConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using SqliteCommand inspect = connection.CreateCommand();
        inspect.CommandText = "SELECT routing, routing_mask, account_mask FROM recipients WHERE id = 'routing'";
        await using SqliteDataReader reader = await inspect.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.IsDBNull(0).Should().BeTrue();
        reader.GetString(1).Should().Be(AchRecipientValidation.MaskRouting("021000021"));
        reader.IsDBNull(2).Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_LegacyAccountFullOnly_PopulatesAccountMask()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-legacy-acct-full-" + Guid.NewGuid().ToString("N") + ".db");
        await using (SqliteConnection legacy = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString()))
        {
            await legacy.OpenAsync();
            await using SqliteCommand create = legacy.CreateCommand();
            create.CommandText = """
                CREATE TABLE recipients (
                  id TEXT PRIMARY KEY,
                  name TEXT NOT NULL,
                  account_full TEXT,
                  created_at TEXT NOT NULL
                );
                INSERT INTO recipients (id, name, account_full, created_at)
                VALUES ('full', 'Full only', '88210001', '2026-08-09T00:00:00.0000000+00:00');
                """;
            await create.ExecuteNonQueryAsync();
        }

        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();

        await using CipherBankDbContext context = await db.CreateContextAsync();
        SqliteConnection connection = (SqliteConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using SqliteCommand inspect = connection.CreateCommand();
        inspect.CommandText = "SELECT account_full, account_mask FROM recipients WHERE id = 'full'";
        await using SqliteDataReader reader = await inspect.ExecuteReaderAsync();
        (await reader.ReadAsync()).Should().BeTrue();
        reader.IsDBNull(0).Should().BeTrue();
        reader.GetString(1).Should().Be(AchRecipientValidation.MaskAccount("88210001"));
    }

    /// <summary>
    /// Pre-EF DBs with only recipients still get the rest of the EF model tables.
    /// Use: Medium (upgrade gate). Scope: LocalDbMigrationTests.
    /// </summary>
    [Fact]
    public async Task InitializeAsync_LegacyRecipientsOnly_CreatesMissingModelTables()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-legacy-partial-" + Guid.NewGuid().ToString("N") + ".db");
        await using (SqliteConnection legacy = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = path }.ToString()))
        {
            await legacy.OpenAsync();
            await using SqliteCommand create = legacy.CreateCommand();
            create.CommandText = """
                CREATE TABLE recipients (
                  id TEXT PRIMARY KEY,
                  name TEXT NOT NULL,
                  created_at TEXT NOT NULL
                );
                """;
            await create.ExecuteNonQueryAsync();
        }

        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();

        await using CipherBankDbContext context = await db.CreateContextAsync();
        SqliteConnection connection = (SqliteConnection)context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using SqliteCommand inspect = connection.CreateCommand();
        inspect.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";
        List<string> tables = new List<string>();
        await using SqliteDataReader reader = await inspect.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }

        tables.Should().Contain("wallets");
        tables.Should().Contain("prefs");
        tables.Should().Contain("rates_snapshot");
        tables.Should().Contain("sync_meta");
        tables.Should().Contain("ohlc");
        tables.Should().Contain("recipients");
    }
}
