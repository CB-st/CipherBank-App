// <copyright file="AddressValidate.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Wallets;

/// <summary>Watch-only address validation. Chain rules live on <see cref="IWalletModule"/>.</summary>
public static class AddressValidate
{
    /// <summary>
    /// Validates a watch-only deposit address for a known asset symbol.
    /// Use: High (add-watch / send paths). Scope: AddressValidate helpers.
    /// </summary>
    public static bool IsValid(string symbol, string address)
    {
        string addr = address.Trim();
        if (string.IsNullOrEmpty(addr) || !AssetSymbol.TryParse(symbol, out AssetSymbol? parsed))
        {
            return false;
        }

        return WalletRegistry.Get(parsed).IsValidAddress(addr);
    }
}
