// <copyright file="WalletTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Models;

public class WalletTests
{
    [Fact]
    public void HasBalance_WhenPositiveBalance_ReturnsTrue()
    {
        // Arrange
        var wallet = new Wallet(
            "wallet123",
            "BTC",
            "Bitcoin",
            0.5m,
            "bc1qtest123",
            DateTimeOffset.UtcNow);

        // Act & Assert
        wallet.HasBalance.Should().BeTrue();
    }

    [Fact]
    public void HasBalance_WhenZeroBalance_ReturnsFalse()
    {
        // Arrange
        var wallet = new Wallet(
            "wallet123",
            "BTC",
            "Bitcoin",
            0m,
            "bc1qtest123",
            DateTimeOffset.UtcNow);

        // Act & Assert
        wallet.HasBalance.Should().BeFalse();
    }

    [Fact]
    public void FormattedBalance_ReturnsCorrectFormat()
    {
        // Arrange
        var wallet = new Wallet(
            "wallet123",
            "BTC",
            "Bitcoin",
            0.12345678m,
            "bc1qtest123",
            DateTimeOffset.UtcNow);

        // Act
        var result = wallet.FormattedBalance;

        // Assert
        result.Should().Be("0.12345678 BTC");
    }

    [Fact]
    public void FormattedBalance_HandlesLargeNumbers()
    {
        // Arrange
        var wallet = new Wallet(
            "wallet123",
            "DOGE",
            "Dogecoin",
            10000.00000000m,
            "DTest123",
            DateTimeOffset.UtcNow);

        // Act
        var result = wallet.FormattedBalance;

        // Assert
        result.Should().Be("10000.00000000 DOGE");
    }
}
