// <copyright file="IndicativeQuoteMapperTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using CipherBank_app.Models;
using CipherBank_app.Services;
using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Services;

public class IndicativeQuoteMapperTests
{
    [Fact]
    public void ToQuoteDto_MapsRateAndClientExpiry()
    {
        PublicQuote quote = new PublicQuote("BTC", 0.0015m, "USD", 100m);
        QuoteDto dto = IndicativeQuoteMapper.ToQuoteDto(quote, nowMs: 1_000_000, ttlMs: 15_000);

        dto.From.Should().Be("BTC");
        dto.To.Should().Be("USD");
        decimal.Parse(dto.Rate, CultureInfo.InvariantCulture).Should().Be(quote.Rate);
        dto.ExpiresAt.Should().Be(1_015_000);
    }

    [Fact]
    public void ToQuoteDto_ZeroInputYieldsZeroRate()
    {
        PublicQuote quote = new PublicQuote("BTC", 0m, "USD", 0m);
        QuoteDto dto = IndicativeQuoteMapper.ToQuoteDto(quote, nowMs: 0);

        dto.Rate.Should().Be("0");
        dto.ExpiresAt.Should().Be(IndicativeQuoteMapper.DefaultTtlMs);
    }
}
