// <copyright file="SqliteRateSnapshotStoreTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class SqliteRateSnapshotStoreTests
{
    [Fact]
    public async Task UpsertThenGet_FiltersBySymbol()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-rates-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new(new FileInfo(path));
        await db.InitializeAsync();
        SqliteRateSnapshotStore cache = new(db);

        await cache.UpsertAsync(
            [
                new RateRow("BTC", 67000m, 1.5m, 1000),
                new RateRow("ETH", 3500m, -0.5m, 1001),
            ],
            default);
        await cache.UpsertAsync([new RateRow("BTC", 68000m, 2.5m, 1002)], default);

        IReadOnlyList<RateRow> rows = await cache.GetAsync(["BTC"], default);
        IReadOnlyList<RateRow> allRows = await cache.GetAsync(null, default);

        rows.Should().Equal(new RateRow("BTC", 68000m, 2.5m, 1002));
        allRows.Should().HaveCount(2, "the symbol primary key bounds persistent growth");
    }

    /// <summary>
    /// Delayed responses must not replace a newer persisted market snapshot.
    /// Use: Medium (concurrent refresh regression). Scope: SqliteRateSnapshotStore.
    /// </summary>
    [Fact]
    public async Task UpsertAsync_OlderTimestamp_DoesNotReplaceNewerSnapshot()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-rates-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new(new FileInfo(path));
        await db.InitializeAsync();
        SqliteRateSnapshotStore cache = new(db);

        await cache.UpsertAsync([new RateRow("BTC", 68_000m, 2.5m, 2_000)], default);
        await cache.UpsertAsync([new RateRow(" btc ", 67_000m, 1.5m, 1_000)], default);

        IReadOnlyList<RateRow> rows = await cache.GetAsync([" btc "], default);

        rows.Should().Equal(new RateRow("BTC", 68_000m, 2.5m, 2_000));
    }

    [Fact]
    public void Constructor_BlankSymbol_ThrowsArgumentException()
    {
        Action act = () => _ = new RateRow(" ", 1m, 0m, 1);

        act.Should().Throw<ArgumentException>();
    }
}
