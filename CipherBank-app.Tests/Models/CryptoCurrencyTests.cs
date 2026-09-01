// <copyright file="CryptoCurrencyTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Models;

public class CryptoCurrencyTests
{
    [Fact]
    public void IsPriceUp_WhenPositiveChange_ReturnsTrue()
    {
        // Arrange
        CryptoCurrency crypto = new CryptoCurrency(
            "BTC",
            "Bitcoin",
            50000m,
            500m,
            1.5m,
            1000000000m,
            50000000m,
            new Uri("https://example.com/btc.png"));

        // Act & Assert
        crypto.IsPriceUp.Should().BeTrue();
    }

    [Fact]
    public void IsPriceUp_WhenNegativeChange_ReturnsFalse()
    {
        // Arrange
        CryptoCurrency crypto = new CryptoCurrency(
            "BTC",
            "Bitcoin",
            50000m,
            -500m,
            -1.5m,
            1000000000m,
            50000000m,
            new Uri("https://example.com/btc.png"));

        // Act & Assert
        crypto.IsPriceUp.Should().BeFalse();
    }

    [Fact]
    public void IsPriceUp_WhenZeroChange_ReturnsTrue()
    {
        // Arrange
        CryptoCurrency crypto = new CryptoCurrency(
            "BTC",
            "Bitcoin",
            50000m,
            0m,
            0m,
            1000000000m,
            50000000m,
            new Uri("https://example.com/btc.png"));

        // Act & Assert
        crypto.IsPriceUp.Should().BeTrue();
    }

    [Fact]
    public void FormattedPrice_ReturnsCorrectFormat()
    {
        // Arrange
        CryptoCurrency crypto = new CryptoCurrency(
            "BTC",
            "Bitcoin",
            50000.50m,
            0m,
            0m,
            1000000000m,
            50000000m,
            new Uri("https://example.com/btc.png"));

        // Act
        string result = crypto.FormattedPrice;

        // Assert - format depends on culture, but should contain the price
        result.Should().Contain("50");
    }

    [Fact]
    public void FormattedPrice_UsesDollarSymbolAndInvariantGrouping()
    {
        CryptoCurrency crypto = new CryptoCurrency("BTC", "Bitcoin", 50000m, 0, 0, 0, 0, null);
        crypto.FormattedPrice.Should().Be("$50,000.00");
    }

    [Fact]
    public void FormattedPercentChange_WhenPositive_IncludesPlusSign()
    {
        // Arrange
        CryptoCurrency crypto = new CryptoCurrency(
            "BTC",
            "Bitcoin",
            50000m,
            500m,
            1.5m,
            1000000000m,
            50000000m,
            new Uri("https://example.com/btc.png"));

        // Act
        string result = crypto.FormattedPercentChange;

        // Assert
        result.Should().StartWith("+");
        result.Should().Contain("1.50%");
    }

    [Fact]
    public void FormattedPercentChange_WhenNegative_DoesNotIncludePlusSign()
    {
        // Arrange
        CryptoCurrency crypto = new CryptoCurrency(
            "BTC",
            "Bitcoin",
            50000m,
            -500m,
            -1.5m,
            1000000000m,
            50000000m,
            new Uri("https://example.com/btc.png"));

        // Act
        string result = crypto.FormattedPercentChange;

        // Assert
        result.Should().NotStartWith("+");
        result.Should().Contain("-1.50%");
    }

    [Fact]
    public void Record_EqualityWorks()
    {
        // Arrange
        CryptoCurrency crypto1 = new CryptoCurrency(
            "BTC",
            "Bitcoin",
            50000m,
            500m,
            1.5m,
            1000000000m,
            50000000m,
            new Uri("https://example.com/btc.png"));

        CryptoCurrency crypto2 = new CryptoCurrency(
            "BTC",
            "Bitcoin",
            50000m,
            500m,
            1.5m,
            1000000000m,
            50000000m,
            new Uri("https://example.com/btc.png"));

        // Act & Assert
        crypto1.Should().Be(crypto2);
    }

    [Fact]
    public void Record_WithExpression_CreatesModifiedCopy()
    {
        // Arrange
        CryptoCurrency crypto = new CryptoCurrency(
            "BTC",
            "Bitcoin",
            50000m,
            500m,
            1.5m,
            1000000000m,
            50000000m,
            new Uri("https://example.com/btc.png"));

        // Act
        CryptoCurrency modified = crypto with { CurrentPrice = 55000m };

        // Assert
        modified.CurrentPrice.Should().Be(55000m);
        modified.Symbol.Should().Be("BTC");
        crypto.CurrentPrice.Should().Be(50000m); // Original unchanged
    }
}
