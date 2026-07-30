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

    private const int Base58LegacyMinLength = 26;
    private const int Base58LegacyMaxLength = 35;
    private const int Bech32MinLength = 42;
    private const int Bech32MaxLength = 62;
    private const int EthereumAddressLength = 42;
    private const int SolanaMinLength = 32;
    private const int SolanaMaxLength = 44;
    private const int GenericMinLength = 20;
    private const int GenericMaxLength = 100;
    private const int Bech32HrpLength = 3;

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

        if (address.StartsWith('1'))
        {
            return IsValidLegacyBase58Address(address);
        }

        if (address.StartsWith('3'))
        {
            return IsValidLegacyBase58Address(address);
        }

        if (address.StartsWith("bc1", StringComparison.OrdinalIgnoreCase))
        {
            return IsValidBech32Range(address);
        }

        if (address.StartsWith('m') || address.StartsWith('n') || address.StartsWith('2'))
        {
            return IsValidLegacyBase58Address(address);
        }

        if (address.StartsWith("tb1", StringComparison.OrdinalIgnoreCase))
        {
            return IsValidBech32Range(address);
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

        if (!address.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (address.Length != EthereumAddressLength)
        {
            return false;
        }

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

        if (address.Length < SolanaMinLength || address.Length > SolanaMaxLength)
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

        return address.Length >= GenericMinLength && address.Length <= GenericMaxLength &&
               address.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private static bool IsValidLegacyBase58Address(string address)
        => address.Length >= Base58LegacyMinLength
           && address.Length <= Base58LegacyMaxLength
           && IsValidBase58(address);

    private static bool IsValidBech32Range(string address)
        => address.Length >= Bech32MinLength
           && address.Length <= Bech32MaxLength
           && IsValidBech32(address);

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
        var bech32Data = address.Substring(Bech32HrpLength); // Remove bc1 or tb1 prefix
        const string bech32Alphabet = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";

        return bech32Data.All(c => bech32Alphabet.Contains(char.ToLowerInvariant(c)) || char.IsDigit(c));
    }

    [GeneratedRegex("^0x[a-fA-F0-9]{40}$")]
    private static partial Regex EthereumAddressRegex();
}
