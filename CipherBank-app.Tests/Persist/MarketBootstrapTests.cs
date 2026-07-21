// <copyright file="MarketBootstrapTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class MarketBootstrapTests
{
    [Fact]
    public void ToRateRow_MapsInverseQuoteRateAndTimestamp()
    {
        var quote = new PublicQuote("btc", 1m, "USD", 67_123.45m);

        RateRow row = MarketBootstrap.ToRateRow(quote, updatedAtMs: 1_000);

        row.Should().Be(new RateRow("BTC", 67_123.45d, 0d, 1_000));
    }
}
