// <copyright file="MockHistoryRangeTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public class MockHistoryRangeTests
{
    [Theory]
    [InlineData("1d", 25)]
    [InlineData("1w", 8)]
    [InlineData("1m", 31)]
    [InlineData("1y", 53)]
    public async Task GetHistoryAsync_HonorsRangePointCounts(string range, int expectedCount)
    {
        var api = new MockProductApi();
        IReadOnlyList<HistoryPointDto> pts = await api.GetHistoryAsync("BTC", range, default);
        pts.Should().HaveCount(expectedCount);
    }
}
