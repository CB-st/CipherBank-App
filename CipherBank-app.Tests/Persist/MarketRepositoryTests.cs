// <copyright file="MarketRepositoryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

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
        LocalDb db = new LocalDb(path);
        await db.InitializeAsync();
        MarketRepository repository = new MarketRepository(db);

        await repository.UpsertOhlcAsync("BTC", [(300, 3.0), (100, 1.0), (200, 2.0)], default);
        await repository.UpsertOhlcAsync("BTC", [(200, 2.5)], default);

        IReadOnlyList<(long T, double V)> points = await repository.GetOhlcAsync("BTC", 200, default);

        points.Should().Equal((200L, 2.5), (300L, 3.0));
    }
}
