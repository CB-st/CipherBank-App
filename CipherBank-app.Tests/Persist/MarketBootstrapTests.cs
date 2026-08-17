// <copyright file="MarketBootstrapTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Persist;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Persist;

public class MarketBootstrapTests
{
    [Fact]
    public void FromQuote_MapsInverseQuoteRateAndTimestamp()
    {
        PublicQuote quote = new PublicQuote("btc", 1m, "USD", 67_123.45m);

        RateRow row = RateRow.FromQuote(quote, updatedAtMs: 1_000);

        row.Should().Be(new RateRow("BTC", 67_123.45m, 0m, 1_000));
    }
}
