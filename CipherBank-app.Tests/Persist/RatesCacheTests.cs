// <copyright file="RatesCacheTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();
        RatesCache cache = new RatesCache(db);

        await cache.UpsertAsync(
            [
                new RateRow("BTC", 67000, 1.5, 1000),
                new RateRow("ETH", 3500, -0.5, 1001),
            ],
            default);
        await cache.UpsertAsync([new RateRow("BTC", 68000, 2.5, 1002)], default);

        IReadOnlyList<RateRow> rows = await cache.GetAsync(["BTC"], default);

        rows.Should().Equal(new RateRow("BTC", 68000, 2.5, 1002));
    }
}
