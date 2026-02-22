// <copyright file="AddressValidator.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace CipherBank_app.Services.Validation;

/// <summary>
/// Validates cryptocurrency addresses for various blockchain networks.
/// Provides format validation for Bitcoin, Ethereum, and Solana addresses.
/// </summary>
public static partial class AddressValidator
{
    // Base58 alphabet used by Bitcoin and Solana
    private const string Base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    /// <summary>
    /// Validates a cryptocurrency address for the specified symbol.
    /// </summary>
    /// <param name="address">The address to validate</param>
    /// <param name="symbol">The cryptocurrency symbol (BTC, ETH, SOL, etc.)</param>
    /// <returns>True if the address format is valid, false otherwise</returns>
    public static bool IsValidAddress(string address, string symbol)
    {
        if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(symbol))
        {
            return false;
        }

        return symbol.ToUpperInvariant() switch
        {
            "BTC" => IsValidBitcoinAddress(address),
            "ETH" => IsValidEthereumAddress(address),
            "SOL" => IsValidSolanaAddress(address),
            _ => IsValidGenericAddress(address),
        };
    }

    /// <summary>
    /// Validates a Bitcoin address.
    /// Supports P2PKH (1...), P2SH (3...), and Bech32 (bc1...) addresses.
    /// </summary>
    public static bool IsValidBitcoinAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        // P2PKH addresses: Start with 1, 25-34 characters
        if (address.StartsWith('1'))
        {
            return address.Length >= 26 && address.Length <= 35 && IsValidBase58(address);
        }

        // P2SH addresses: Start with 3, 25-35 characters
        if (address.StartsWith('3'))
        {
            return address.Length >= 26 && address.Length <= 35 && IsValidBase58(address);
        }

        // Bech32 addresses: Start with bc1, 42-62 characters
        if (address.StartsWith("bc1", StringComparison.OrdinalIgnoreCase))
        {
            return address.Length >= 42 && address.Length <= 62 && IsValidBech32(address);
        }

        // Testnet addresses: Start with m, n, or 2 (P2PKH/P2SH) or tb1 (Bech32)
        if (address.StartsWith('m') || address.StartsWith('n') || address.StartsWith('2'))
        {
            return address.Length >= 26 && address.Length <= 35 && IsValidBase58(address);
        }

        if (address.StartsWith("tb1", StringComparison.OrdinalIgnoreCase))
        {
            return address.Length >= 42 && address.Length <= 62 && IsValidBech32(address);
        }

        return false;
    }

    /// <summary>
    /// Validates an Ethereum address.
    /// Expects 0x prefix followed by 40 hexadecimal characters.
    /// </summary>
    public static bool IsValidEthereumAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        // Must start with 0x and be exactly 42 characters (0x + 40 hex)
        if (!address.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (address.Length != 42)
        {
            return false;
        }

        // Check that remaining characters are valid hex
        return EthereumAddressRegex().IsMatch(address);
    }

    /// <summary>
    /// Validates a Solana address.
    /// Expects a Base58 encoded string of 32-44 characters.
    /// </summary>
    public static bool IsValidSolanaAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        // Solana addresses are typically 32-44 characters in Base58
        if (address.Length < 32 || address.Length > 44)
        {
            return false;
        }

        return IsValidBase58(address);
    }

    /// <summary>
    /// Generic address validation for unknown cryptocurrencies.
    /// Checks basic length and character requirements.
    /// </summary>
    private static bool IsValidGenericAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return false;
        }

        // Basic validation: reasonable length, alphanumeric
        return address.Length >= 20 && address.Length <= 100 &&
               address.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    /// <summary>
    /// Checks if a string contains only valid Base58 characters.
    /// </summary>
    private static bool IsValidBase58(string value)
    {
        return !string.IsNullOrEmpty(value) && value.All(c => Base58Alphabet.Contains(c));
    }

    /// <summary>
    /// Validates Bech32 address format (simplified validation).
    /// </summary>
    private static bool IsValidBech32(string address)
    {
        // Bech32 uses lowercase a-z and 0-9, excluding 1, b, i, o
        var bech32Data = address.Substring(3); // Remove bc1 or tb1 prefix
        const string bech32Alphabet = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";

        return bech32Data.All(c => bech32Alphabet.Contains(char.ToLowerInvariant(c)) || char.IsDigit(c));
    }

    [GeneratedRegex("^0x[a-fA-F0-9]{40}$")]
    private static partial Regex EthereumAddressRegex();
}
