// <copyright file="CurrencySymbolMapTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Services;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Services;

public class CurrencySymbolMapTests
{
    [Theory]
    [InlineData("BTC", "BITCOIN")]
    [InlineData("btc", "BITCOIN")]
    [InlineData("XMR", "MONERO")]
    [InlineData("USD", "USD")]
    [InlineData("BITCOIN", "BITCOIN")]
    public void ToApiCurrency_MapsKnownSymbols(string input, string expected)
    {
        CurrencySymbolMap.ToApiCurrency(input).Should().Be(expected);
    }

    [Fact]
    public void ToApiCurrency_RejectsUnsupportedSymbols()
    {
        var act = () => CurrencySymbolMap.ToApiCurrency("ETH");
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("BITCOIN", "BTC")]
    [InlineData("MONERO", "XMR")]
    [InlineData("USD", "USD")]
    public void ToAppSymbol_MapsKnownApiCodes(string input, string expected)
    {
        CurrencySymbolMap.ToAppSymbol(input).Should().Be(expected);
    }

    [Fact]
    public void IsSupported_ReturnsExpected()
    {
        CurrencySymbolMap.IsSupported("BTC").Should().BeTrue();
        CurrencySymbolMap.IsSupported("ETH").Should().BeFalse();
        CurrencySymbolMap.IsSupported(null).Should().BeFalse();
    }
}
