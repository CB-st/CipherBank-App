// <copyright file="RatesCacheTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class RatesCacheTests
{
    [Fact]
    public async Task UpsertThenGet_FiltersBySymbol()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-rates-" + Guid.NewGuid().ToString("N") + ".db");
        var db = new LocalDb(path);
        await db.InitializeAsync();
        var cache = new RatesCache(db);

        await cache.UpsertAsync(
            [
                new RateRow("BTC", 67000, 1.5, 1000),
                new RateRow("ETH", 3500, -0.5, 1001),
            ],
            default);
        await cache.UpsertAsync([new RateRow("BTC", 68000, 2.5, 1002)], default);

        var rows = await cache.GetAsync(["BTC"], default);

        rows.Should().Equal(new RateRow("BTC", 68000, 2.5, 1002));
    }
}
