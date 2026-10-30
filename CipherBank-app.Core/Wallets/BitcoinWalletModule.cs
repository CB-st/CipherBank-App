// <copyright file="BitcoinWalletModule.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using NBitcoin;

namespace CipherBank_app.Wallets;

/// <summary>BIP84 native segwit Bitcoin module.</summary>
public sealed class BitcoinWalletModule : AddressDerive, IWalletModule
{
    /// <inheritdoc />
    public AssetSymbol Symbol => AssetSymbol.Btc;

    /// <inheritdoc />
    public IReadOnlyList<WalletUiMode> AddModes { get; } = [WalletUiMode.Derive, WalletUiMode.Watch];

    /// <inheritdoc />
    public bool CanDerive => true;

    /// <inheritdoc />
    public bool UsesServerWallets => false;

    /// <inheritdoc />
    public string? Notes => "BIP84 native segwit from on-device BIP39";

    /// <inheritdoc />
    public bool IsValidAddress(string address)
    {
        try
        {
            return BitcoinAddress.Create(address, Network.Main) is not null;
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public Uri BuildReceiveUri(string address, string? amount, string? label, string? message)
        => PaymentUri.Bip21("bitcoin", address, amount, label, message);

    /// <inheritdoc />
    protected override DerivedAddress DeriveCore(string mnemonic, int accountIndex)
    {
        string path = ReceivePath(84, 0, accountIndex);
        ExtKey key = DeriveKey(mnemonic, path);
        BitcoinAddress address = key.Neuter().PubKey.GetAddress(ScriptPubKeyType.Segwit, Network.Main);
        return new DerivedAddress(address.ToString(), path, accountIndex);
    }
}
