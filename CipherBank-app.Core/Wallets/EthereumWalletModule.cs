// <copyright file="EthereumWalletModule.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Text.RegularExpressions;
using CipherBank_app.Custody;
using CipherBank_app.Models;
using Nethereum.Util;

namespace CipherBank_app.Wallets;

/// <summary>BIP44 Ethereum account module.</summary>
public sealed partial class EthereumWalletModule : IWalletModule
{
    /// <inheritdoc />
    public AssetSymbol Symbol { get; } = new("ETH");

    /// <inheritdoc />
    public IReadOnlyList<WalletUiMode> AddModes { get; } = [WalletUiMode.Derive, WalletUiMode.Watch];

    /// <inheritdoc />
    public bool CanDerive => true;

    /// <inheritdoc />
    public bool UsesServerWallets => false;

    /// <inheritdoc />
    public string? Notes => "BIP44 m/44'/60'/0'/0/i from on-device BIP39";

    /// <inheritdoc />
    public bool IsValidAddress(string address) => EthAddressRegex().IsMatch(address);

    /// <inheritdoc />
    public DerivedAddress? TryDerive(string mnemonic, int accountIndex = 0)
    {
        Nethereum.HdWallet.Wallet wallet = new Nethereum.HdWallet.Wallet(MnemonicHelper.Normalize(mnemonic), null);
        Nethereum.Web3.Accounts.Account account = wallet.GetAccount(accountIndex);
        string path = $"m/44'/60'/0'/0/{accountIndex}";
        string checksum = new AddressUtil().ConvertToChecksumAddress(account.Address);
        return new DerivedAddress(checksum, path, accountIndex);
    }

    /// <inheritdoc />
    public Uri BuildReceiveUri(string address, string? amount, string? label, string? message)
    {
        _ = label;
        _ = message;
        return PaymentUri.Account("ethereum", "value", address, amount);
    }

    [GeneratedRegex("^0x[0-9a-fA-F]{40}$", RegexOptions.Compiled)]
    private static partial Regex EthAddressRegex();
}
