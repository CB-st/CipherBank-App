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
                "XMR" => addr.Length is >= XmrAddressMinLength and <= XmrAddressMaxLength,
                _ => addr.Length >= GenericAddressMinLength,
            };
        }
        catch
        {
            return false;
        }
    }

    [GeneratedRegex("^0x[0-9a-fA-F]{40}$", RegexOptions.Compiled)]
    private static partial Regex EthAddressRegex();
}
