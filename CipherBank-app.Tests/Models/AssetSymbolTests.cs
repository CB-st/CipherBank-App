// <copyright file="AssetSymbolTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.Json;
using CipherBank_app.Models;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Models;

public sealed class AssetSymbolTests
{
    [Theory]
    [InlineData("BTC", "BTC")]
    [InlineData(" btc ", "BTC")]
    [InlineData("xMr", "XMR")]
    public void Constructor_NormalizesNonblankTicker(string input, string expected)
    {
        AssetSymbol symbol = new(input);

        symbol.Value.Should().Be(expected);
        symbol.ToString().Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void Constructor_RejectsBlankTicker(string input)
    {
        Action create = () => _ = new AssetSymbol(input);

        create.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Equality_UsesNormalizedTickerValue()
    {
        AssetSymbol.Parse(" btc ").Should().Be(AssetSymbol.Parse("BTC"));
        AssetSymbol.Parse("BTC").Should().NotBe(AssetSymbol.Parse("ETH"));
    }

    [Fact]
    public void Json_RoundTripUsesPlainNormalizedString()
    {
        AssetSymbol symbol = JsonSerializer.Deserialize<AssetSymbol>("\" btc \"")!;

        symbol.Should().Be(AssetSymbol.Parse("BTC"));
        JsonSerializer.Serialize(symbol).Should().Be("\"BTC\"");
    }

    [Theory]
    [InlineData("null")]
    [InlineData("42")]
    [InlineData("\" \"")]
    public void Json_RejectsMissingOrInvalidTicker(string json)
    {
        Action deserialize = () => JsonSerializer.Deserialize<AssetSymbol>(json);

        deserialize.Should().Throw<JsonException>();
    }
}
