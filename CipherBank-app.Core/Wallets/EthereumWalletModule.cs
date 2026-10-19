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
        if (!TryGetSeparator(trimmed, out int separator))
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "ETH amount must be a non-negative decimal with at most 18 fractional digits.");
        }

        string whole = IntegerPart(trimmed, separator);
        string fraction = FractionalPart(trimmed, separator);
        if (IsNegative(whole, fraction))
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "ETH amount cannot be negative.");
        }

        whole = whole.Length == 0 ? "0" : whole;
        if (!HasWeiPrecision(whole, fraction))
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                "ETH amount must be a non-negative decimal with at most 18 fractional digits.");
        }

        return FormatWei(whole, fraction);
    }

    private static bool TryGetSeparator(string trimmed, out int separator)
    {
        separator = trimmed.IndexOf('.');
        return separator == trimmed.LastIndexOf('.') && ContainsAsciiDigit(trimmed);
    }

    private static string IntegerPart(string trimmed, int separator)
        => separator < 0 ? trimmed : trimmed[..separator];

    private static string FractionalPart(string trimmed, int separator)
        => separator < 0 ? string.Empty : trimmed[(separator + 1)..];

    private static bool IsNegative(string whole, string fraction)
        => whole.StartsWith('-') || fraction.Contains('-');

    private static bool HasWeiPrecision(string whole, string fraction)
        => IsAsciiDigits(whole) && IsAsciiDigits(fraction) && fraction.Length <= WeiDecimalPlaces;

    private static string FormatWei(string whole, string fraction)
    {
        fraction = fraction.PadRight(WeiDecimalPlaces, '0');
        return BigInteger.Parse(whole + fraction, CultureInfo.InvariantCulture)
            .ToString(CultureInfo.InvariantCulture);
    }

    private static bool ContainsAsciiDigit(string value)
        => value.Where(static c => c is >= '0' and <= '9').Any();

    private static bool IsAsciiDigits(string value)
        => !value.Where(static c => c is < '0' or > '9').Any();

    [GeneratedRegex("^0x[0-9a-fA-F]{40}$", RegexOptions.Compiled)]
    private static partial Regex EthAddressRegex();
}
