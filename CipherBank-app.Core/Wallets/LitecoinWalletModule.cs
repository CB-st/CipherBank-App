// <copyright file="LitecoinWalletModule.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Models;
using NBitcoin;

namespace CipherBank_app.Wallets;

/// <summary>BIP84 native segwit Litecoin module.</summary>
public sealed class LitecoinWalletModule : IWalletModule
{
    /// <inheritdoc />
    public AssetSymbol Symbol { get; } = new("LTC");

    /// <inheritdoc />
    public IReadOnlyList<WalletUiMode> AddModes { get; } = [WalletUiMode.Derive, WalletUiMode.Watch];

    /// <inheritdoc />
    public bool CanDerive => true;

    /// <inheritdoc />
    public bool UsesServerWallets => false;

    /// <inheritdoc />
    public string? Notes => "BIP84 native segwit m/84'/2'/0'/0/i";

    /// <inheritdoc />
    public bool IsValidAddress(string address)
    {
        try
        {
            return BitcoinAddress.Create(address, NBitcoin.Altcoins.Litecoin.Instance.Mainnet) is not null;
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
        string path = $"m/84'/2'/0'/0/{accountIndex}";
        ExtKey key = root.Derive(new KeyPath(path));
        WitKeyId wit = key.Neuter().PubKey.WitHash;
        string address = new BitcoinWitPubKeyAddress(wit, NBitcoin.Altcoins.Litecoin.Instance.Mainnet).ToString();
        return new DerivedAddress(address, path, accountIndex);
    }

    /// <inheritdoc />
    public Uri BuildReceiveUri(string address, string? amount, string? label, string? message)
        => PaymentUri.Bip21("litecoin", address, amount, label, message);
}
