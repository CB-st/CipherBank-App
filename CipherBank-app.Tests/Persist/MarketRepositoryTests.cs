// <copyright file="MarketRepositoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class MarketRepositoryTests
{
    [Fact]
    public async Task UpsertThenGet_ReturnsPointsOrderedByTimestamp()
    {
        string path = Path.Combine(Path.GetTempPath(), "cb-market-" + Guid.NewGuid().ToString("N") + ".db");
        LocalDb db = new(new FileInfo(path));
        await db.InitializeAsync();
        MarketRepository repository = new(db);

        await repository.UpsertOhlcAsync(
            "BTC",
            [Point(300, 3m, 12m), Point(100, 1m), Point(200, 2m)],
            default);
        await repository.UpsertOhlcAsync("BTC", [Point(200, 2.5m)], default);

        IReadOnlyList<PricePoint> points = await repository.GetOhlcAsync("BTC", 200, default);

        points.Should().Equal(Point(200, 2.5m), Point(300, 3m, 12m));
    }

    private static PricePoint Point(long timestamp, decimal price, decimal? volume = null) =>
        new(DateTimeOffset.FromUnixTimeMilliseconds(timestamp), price, volume);
}
