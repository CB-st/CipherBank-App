// <copyright file="PublicQuoteTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Models;

public sealed class PublicQuoteTests
{
    [Fact]
    public void RateAndInverseRate_AreReciprocalsWhenAmountsArePositive()
    {
        PublicQuote quote = new PublicQuote("BTC", 2m, "USD", 100_000m);

        quote.Rate.Should().Be(50_000m);
        quote.InverseRate.Should().Be(0.00002m);
        (quote.Rate * quote.InverseRate).Should().Be(1m);
    }

    [Fact]
    public void RateAndInverseRate_AreZeroWhenTheDivisorIsZero()
    {
        PublicQuote zeroInput = new PublicQuote("BTC", 0m, "USD", 100m);
        PublicQuote zeroOutput = new PublicQuote("BTC", 2m, "USD", 0m);

        zeroInput.Rate.Should().Be(0m);
        zeroOutput.InverseRate.Should().Be(0m);
    }
}
