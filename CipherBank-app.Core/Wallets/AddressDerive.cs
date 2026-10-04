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

    /// <summary>Derives the first Bitcoin receive address.</summary>
    public static DerivedAddress DeriveBtc(string mnemonic)
        => DeriveBtc(mnemonic, 0);

    /// <summary>Derives a Bitcoin receive address at <paramref name="accountIndex"/>.</summary>
    public static DerivedAddress DeriveBtc(string mnemonic, int accountIndex)
        => Require(Derive("BTC", mnemonic, accountIndex));

    /// <summary>Derives the first Litecoin receive address.</summary>
    public static DerivedAddress DeriveLtc(string mnemonic)
        => DeriveLtc(mnemonic, 0);

    /// <summary>Derives a Litecoin receive address at <paramref name="accountIndex"/>.</summary>
    public static DerivedAddress DeriveLtc(string mnemonic, int accountIndex)
        => Require(Derive("LTC", mnemonic, accountIndex));

    /// <summary>Derives the first Dogecoin receive address.</summary>
    public static DerivedAddress DeriveDoge(string mnemonic)
        => DeriveDoge(mnemonic, 0);

    /// <summary>Derives a Dogecoin receive address at <paramref name="accountIndex"/>.</summary>
    public static DerivedAddress DeriveDoge(string mnemonic, int accountIndex)
        => Require(Derive("DOGE", mnemonic, accountIndex));

    /// <summary>Derives the first Ethereum account.</summary>
    public static DerivedAddress DeriveEth(string mnemonic)
        => DeriveEth(mnemonic, 0);

    /// <summary>Derives an Ethereum account at <paramref name="accountIndex"/>.</summary>
    public static DerivedAddress DeriveEth(string mnemonic, int accountIndex)
        => Require(Derive("ETH", mnemonic, accountIndex));

    private static DerivedAddress Require(DerivedAddress? derived)
        => derived ?? throw new InvalidOperationException("Wallet derivation returned no address.");
}
