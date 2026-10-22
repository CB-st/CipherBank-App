// <copyright file="AddressValidatorTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Services.Validation;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Services;

/// <summary>
/// Unit tests for the AddressValidator class.
/// </summary>
public class AddressValidatorTests
{
    [Theory]
    [InlineData("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", true)] // Genesis block address
    [InlineData("1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2", true)] // P2PKH
    [InlineData("3J98t1WpEZ73CNmQviecrnyiWrnqRhWNLy", true)] // P2SH
    [InlineData("bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdq", true)] // Bech32
    [InlineData("", false)]
    [InlineData("invalid", false)]
    [InlineData("1", false)]
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb0", false)] // Ethereum address
    public void IsValidAddress_Bitcoin_ValidatesCorrectly(string address, bool expected)
    {
        // Act
        var result = AddressValidator.IsValidAddress(address, "BTC");

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidBitcoinAddress_P2PKHAddress_ReturnsTrue()
    {
        // Arrange - P2PKH addresses start with 1
        var address = "1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2";

        // Act
        var result = AddressValidator.IsValidBitcoinAddress(address);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidBitcoinAddress_P2SHAddress_ReturnsTrue()
    {
        // Arrange - P2SH addresses start with 3
        var address = "3J98t1WpEZ73CNmQviecrnyiWrnqRhWNLy";

        // Act
        var result = AddressValidator.IsValidBitcoinAddress(address);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidBitcoinAddress_Bech32Address_ReturnsTrue()
    {
        // Arrange - Bech32 addresses start with bc1
        var address = "bc1qar0srrr7xfkvy5l643lydnw9re59gtzzwf5mdq";

        // Act
        var result = AddressValidator.IsValidBitcoinAddress(address);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidBitcoinAddress_TestnetAddress_ReturnsTrue()
    {
        // Arrange - Testnet addresses start with m, n, 2, or tb1
        var address = "tb1qw508d6qejxtdg4y5r3zarvary0c5xw7kxpjzsx";

        // Act
        var result = AddressValidator.IsValidBitcoinAddress(address);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb0", true)]
    [InlineData("0xde0B295669a9FD93d5F28D9Ec85E40f4cb697BAe", true)]
    [InlineData("0x0000000000000000000000000000000000000000", true)]
    [InlineData("", false)]
    [InlineData("0x", false)]
    [InlineData("0xinvalid", false)]
    [InlineData("742d35Cc6634C0532925a3b844Bc9e7595f0bEb0", false)] // Missing 0x prefix
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f0bE", false)] // Too short
    [InlineData("1BvBMSEYstWetqTFn5Au4m4GFg7xJaNVN2", false)] // Bitcoin address
    public void IsValidAddress_Ethereum_ValidatesCorrectly(string address, bool expected)
    {
        // Act
        var result = AddressValidator.IsValidAddress(address, "ETH");

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidEthereumAddress_ValidChecksumAddress_ReturnsTrue()
    {
        // Arrange
        var address = "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb0";

        // Act
        var result = AddressValidator.IsValidEthereumAddress(address);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidEthereumAddress_LowercaseAddress_ReturnsTrue()
    {
        // Arrange
        var address = "0x742d35cc6634c0532925a3b844bc9e7595f0beb0";

        // Act
        var result = AddressValidator.IsValidEthereumAddress(address);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData("DRpbCBMxVnDK7maPM5tGv6MvB3v1sRMC86PZ8okm21hy", true)] // Valid Solana address
    [InlineData("9WzDXwBbmkg8ZTbNMqUxvQRAyrZzDsGYdLVL9zYtAWWM", true)]
    [InlineData("", false)]
    [InlineData("short", false)]
    [InlineData("0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb0", false)] // Contains invalid Base58 chars (0, l, I, O)
    public void IsValidAddress_Solana_ValidatesCorrectly(string address, bool expected)
    {
        // Act
        var result = AddressValidator.IsValidAddress(address, "SOL");

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void IsValidAddress_NullAddress_ReturnsFalse()
    {
        // Act
        var result = AddressValidator.IsValidAddress(null!, "BTC");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidAddress_NullSymbol_ReturnsFalse()
    {
        // Act
        var result = AddressValidator.IsValidAddress("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidAddress_WhitespaceAddress_ReturnsFalse()
    {
        // Act
        var result = AddressValidator.IsValidAddress("   ", "BTC");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValidAddress_UnknownSymbol_UsesGenericValidation()
    {
        // Arrange - Generic validation accepts alphanumeric 20-100 chars
        var address = "ABCDEFGHIJKLMNOPQRSTuvwxyz123456";

        // Act
        var result = AddressValidator.IsValidAddress(address, "UNKNOWN");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsValidAddress_CaseInsensitiveSymbol_Works()
    {
        // Arrange
        var address = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";

        // Act & Assert
        AddressValidator.IsValidAddress(address, "btc").Should().BeTrue();
        AddressValidator.IsValidAddress(address, "BTC").Should().BeTrue();
        AddressValidator.IsValidAddress(address, "Btc").Should().BeTrue();
    }
}
