// <copyright file="AddressValidate.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Text.RegularExpressions;
using NBitcoin;

namespace CipherBank_app.Wallets;

/// <summary>Watch-only address validation helpers.</summary>
public static partial class AddressValidate
{
    private const int XmrAddressMinLength = 95;
    private const int XmrAddressMaxLength = 106;
    private const int GenericAddressMinLength = 8;

    // Monero Base58 alphabet (Bitcoin-style; no 0/O/I/l).
    private const string XmrBase58Alphabet =
        "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    /// <summary>
    /// Validates a watch-only deposit address for a known asset symbol.
    /// Use: High (add-watch / send paths). Scope: AddressValidate helpers.
    /// </summary>
    public static bool IsValid(string symbol, string address)
    {
        string sym = symbol.ToUpperInvariant();
        string addr = address.Trim();
        if (string.IsNullOrEmpty(addr))
        {
            return false;
        }

        try
        {
            return sym switch
            {
                "BTC" => BitcoinAddress.Create(addr, Network.Main) is not null,
                "LTC" => BitcoinAddress.Create(addr, NBitcoin.Altcoins.Litecoin.Instance.Mainnet) is not null,
                "DOGE" => BitcoinAddress.Create(addr, NBitcoin.Altcoins.Dogecoin.Instance.Mainnet) is not null,
                "ETH" => EthAddressRegex().IsMatch(addr),
                "XMR" => IsValidXmrAddress(addr),
                _ => addr.Length >= GenericAddressMinLength,
            };
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Length + Monero Base58 alphabet check (checksum validation deferred).
    /// Use: High (XMR watch/send). Scope: AddressValidate helpers.
    /// </summary>
    private static bool IsValidXmrAddress(string addr)
    {
        if (addr.Length is < XmrAddressMinLength or > XmrAddressMaxLength)
        {
            return false;
        }

        foreach (char c in addr)
        {
            if (!XmrBase58Alphabet.Contains(c, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex("^0x[0-9a-fA-F]{40}$", RegexOptions.Compiled)]
    private static partial Regex EthAddressRegex();
}
