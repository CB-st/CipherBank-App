// <copyright file="AddressValidator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Collections.Frozen;
using System.Text.RegularExpressions;
using CipherBank_app.Models;

namespace CipherBank_app.Services.Validation;

/// <summary>
/// Validates cryptocurrency addresses for various blockchain networks.
/// Provides format validation for Bitcoin, Ethereum, and Solana addresses.
/// </summary>
public static partial class AddressValidator
{
    private const int Bech32PrefixLength = 3;
    private const int Base58LegacyMinLength = 26;
    private const int Base58LegacyMaxLength = 35;
    private const int Bech32MinLength = 42;
    private const int Bech32MaxLength = 62;
    private const int EthereumAddressLength = 42;
    private const int SolanaMinLength = 32;
    private const int SolanaMaxLength = 44;
    private const int GenericMinLength = 20;
    private const int GenericMaxLength = 100;

    // Base58 alphabet used by Bitcoin and Solana
    private static readonly FrozenSet<char> _base58Alphabet =
        "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz".ToFrozenSet();

    private static readonly FrozenSet<char> _bech32Alphabet =
        ("qpzry9x8gf2tvdw0s3jn54khce6mua7l" + "QPZRY9X8GF2TVDW0S3JN54KHCE6MUA7L").ToFrozenSet();

    /// <summary>
    /// Validates a cryptocurrency address for the specified symbol.
    /// </summary>
    /// <param name="address">The address to validate</param>
    /// <param name="symbol">The cryptocurrency symbol (BTC, ETH, SOL, etc.)</param>
    /// <returns>True if the address format is valid, false otherwise</returns>
    public static bool IsValidAddress(string address, string symbol)
    {
        return AssetSymbol.TryParse(symbol, out AssetSymbol? parsed)
            && IsValidAddress(address, parsed);
    }

    /// <summary>Validates an address for a normalized asset symbol.</summary>
    /// <param name="address">The address to validate.</param>
    /// <param name="symbol">The normalized asset symbol.</param>
    /// <returns>True if the address format is valid, false otherwise.</returns>
    public static bool IsValidAddress(string address, AssetSymbol symbol)
    {
        if (string.IsNullOrWhiteSpace(address) || symbol is null)
        {
            return false;
        }

        return symbol.Value switch
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
        => !string.IsNullOrEmpty(value) && value.All(c => _base58Alphabet.Contains(c));

    /// <summary>
    /// Validates Bech32 address format (simplified validation).
    /// </summary>
    private static bool IsValidBech32(string address)
    {
        // BIP-173: an address is all lowercase or all uppercase, never mixed case
        if (address.Any(char.IsLower) && address.Any(char.IsUpper))
        {
            return false;
        }

        // Bech32 uses ASCII a-z and 0-9, excluding 1, b, i, o in either case.
        // The alphabet set contains ASCII characters only, so non-ASCII lookalikes cannot match.
        // Callers check the bc1/tb1 prefix and the 42-62 length.
        foreach (char character in address.AsSpan(Bech32PrefixLength))
        {
            if (!_bech32Alphabet.Contains(character))
            {
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex("^0x[a-fA-F0-9]{40}$")]
    private static partial Regex EthereumAddressRegex();
}
