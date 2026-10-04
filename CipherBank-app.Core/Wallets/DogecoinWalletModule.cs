// <copyright file="DogecoinWalletModule.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Models;
using NBitcoin;

namespace CipherBank_app.Wallets;

/// <summary>BIP44 P2PKH Dogecoin module.</summary>
public sealed class DogecoinWalletModule : IWalletModule
{
    /// <inheritdoc />
    public AssetSymbol Symbol { get; } = new("DOGE");

    /// <inheritdoc />
    public IReadOnlyList<WalletUiMode> AddModes { get; } = [WalletUiMode.Derive, WalletUiMode.Watch];

    /// <inheritdoc />
    public bool CanDerive => true;

    /// <inheritdoc />
    public bool UsesServerWallets => false;

    /// <inheritdoc />
    public string? Notes => "BIP44 m/44'/3'/0'/0/i P2PKH";

    /// <inheritdoc />
    public bool IsValidAddress(string address)
    {
        try
        {
            return BitcoinAddress.Create(address, NBitcoin.Altcoins.Dogecoin.Instance.Mainnet) is not null;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public DerivedAddress? TryDerive(string mnemonic, int accountIndex = 0)
    {
        Mnemonic parsed = MnemonicHelper.Parse(mnemonic);
        ExtKey root = parsed.DeriveExtKey();
        string path = $"m/44'/3'/0'/0/{accountIndex}";
        ExtKey key = root.Derive(new KeyPath(path));
        BitcoinAddress address = key.Neuter().PubKey.GetAddress(
            ScriptPubKeyType.Legacy,
            NBitcoin.Altcoins.Dogecoin.Instance.Mainnet);
        return new DerivedAddress(address.ToString(), path, accountIndex);
    }

    /// <inheritdoc />
    public Uri BuildReceiveUri(string address, string? amount, string? label, string? message)
        => PaymentUri.Bip21("dogecoin", address, amount, label, message);
}
