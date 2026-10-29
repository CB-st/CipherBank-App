// <copyright file="AddressDerive.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Wallets;

/// <summary>BIP84/BIP44 derivation facade. Chain logic lives on <see cref="IWalletModule"/>.</summary>
public static class AddressDerive
{
    /// <summary>Returns true when <paramref name="symbol"/> has an on-device derive path.</summary>
    public static bool IsDerivable(AssetSymbol symbol)
        => WalletRegistry.Get(symbol).CanDerive;

    /// <summary>Derives account 0, or null when the module cannot derive.</summary>
    public static DerivedAddress? Derive(AssetSymbol symbol, string mnemonic)
        => Derive(symbol, mnemonic, 0);

    /// <summary>Derives <paramref name="accountIndex"/>, or null when the module cannot derive.</summary>
    public static DerivedAddress? Derive(AssetSymbol symbol, string mnemonic, int accountIndex)
        => WalletRegistry.Get(symbol).TryDerive(mnemonic, accountIndex);

    /// <summary>Derives <paramref name="accountIndex"/> and throws when the module cannot derive.</summary>
    public static DerivedAddress DeriveRequired(AssetSymbol symbol, string mnemonic, int accountIndex)
        => Derive(symbol, mnemonic, accountIndex)
            ?? throw new InvalidOperationException($"{symbol} has no on-device derivation path.");
}
