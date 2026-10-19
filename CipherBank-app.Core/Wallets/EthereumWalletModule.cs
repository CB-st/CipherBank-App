// <copyright file="EthereumWalletModule.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using CipherBank_app.Custody;
using CipherBank_app.Models;
using Nethereum.Util;

namespace CipherBank_app.Wallets;

/// <summary>BIP44 Ethereum account module.</summary>
public sealed partial class EthereumWalletModule : IWalletModule
{
    /// <summary>Fractional digits between 1 ETH and 1 wei. ERC-681 <c>value</c> is an integer wei count.</summary>
    private const int WeiDecimalPlaces = 18;

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
    public DerivedAddress? TryDerive(string mnemonic, int accountIndex)
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
        // Nominal ETH (for example 0.1) is not a wei count. Encode an exact integer.
        string? wei = string.IsNullOrWhiteSpace(amount) ? null : ToWei(amount);
        return PaymentUri.Account("ethereum", "value", address, wei);
    }

    /// <summary>
    /// Converts a nominal ETH decimal to wei with integer arithmetic.
    /// Use: Medium (receive QR). Scope: ERC-681 value parameter.
    /// </summary>
    private static string ToWei(string amount)
    {
        string trimmed = amount.Trim();
        int separator = trimmed.IndexOf('.');
        if (separator != trimmed.LastIndexOf('.')
            || !ContainsAsciiDigit(trimmed))
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "ETH amount must be a non-negative decimal with at most 18 fractional digits.");
        }

        string whole = separator < 0 ? trimmed : trimmed[..separator];
        string fraction = separator < 0 ? string.Empty : trimmed[(separator + 1)..];
        if (whole.StartsWith('-') || fraction.Contains('-'))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "ETH amount cannot be negative.");
        }

        if (whole.Length == 0)
        {
            whole = "0";
        }

        if (!IsAsciiDigits(whole) || !IsAsciiDigits(fraction) || fraction.Length > WeiDecimalPlaces)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "ETH amount must be a non-negative decimal with at most 18 fractional digits.");
        }

        fraction = fraction.PadRight(WeiDecimalPlaces, '0');
        return BigInteger.Parse(whole + fraction, CultureInfo.InvariantCulture)
            .ToString(CultureInfo.InvariantCulture);
    }

    private static bool ContainsAsciiDigit(string value)
    {
        foreach (char c in value)
        {
            if (c is >= '0' and <= '9')
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsAsciiDigits(string value)
    {
        foreach (char c in value)
        {
            if (c is < '0' or > '9')
            {
                return false;
            }
        }

        return true;
    }

    [GeneratedRegex("^0x[0-9a-fA-F]{40}$", RegexOptions.Compiled)]
    private static partial Regex EthAddressRegex();
}
