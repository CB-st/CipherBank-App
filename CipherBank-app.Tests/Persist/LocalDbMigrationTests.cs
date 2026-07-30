// <copyright file="LocalDbMigrationTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Microsoft.Data.Sqlite;
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

        await using SqliteConnection conn = db.Open();
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
}
