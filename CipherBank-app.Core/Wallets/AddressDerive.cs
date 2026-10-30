// <copyright file="AddressDerive.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Models;
using NBitcoin;

namespace CipherBank_app.Wallets;

/// <summary>BIP84/BIP44 derivation base and facade. Chains supply address formatting.</summary>
public abstract class AddressDerive
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

    /// <summary>Derives a receive address using the chain's path and address format.</summary>
    public DerivedAddress TryDerive(string mnemonic, int accountIndex)
        => DeriveCore(mnemonic, accountIndex);

    /// <summary>Builds a BIP44/BIP84 first-account external receive path.</summary>
    protected static string ReceivePath(int purpose, int coinType, int accountIndex)
        => $"m/{purpose}'/{coinType}'/0'/0/{accountIndex}";

    /// <summary>Derives the BIP32 child key at the chain's receive path.</summary>
    protected static ExtKey DeriveKey(string mnemonic, string path)
        => MnemonicHelper.Parse(mnemonic).DeriveExtKey().Derive(new KeyPath(path));

    /// <summary>Formats the derived chain address and reports the exact path used.</summary>
    protected abstract DerivedAddress DeriveCore(string mnemonic, int accountIndex);
}
