// <copyright file="InMemoryHistoryRangeTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public class InMemoryHistoryRangeTests
{
    [Theory]
    [InlineData("1d", 25)]
    [InlineData("1w", 8)]
    [InlineData("1m", 31)]
    [InlineData("1y", 53)]
    public async Task GetHistoryAsync_HonorsRangePointCounts(string range, int expectedCount)
    {
        InMemoryProductClient api = new InMemoryProductClient();
        IReadOnlyList<HistoryPointDto> pts = await api.GetHistoryAsync("BTC", range, default);
        pts.Should().HaveCount(expectedCount);
    }
}
