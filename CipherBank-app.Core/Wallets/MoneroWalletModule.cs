// <copyright file="MoneroWalletModule.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Wallets;

/// <summary>Server-managed Monero module. Native derive stays deferred.</summary>
public sealed class MoneroWalletModule : IWalletModule
{
    private const int AddressMinLength = 95;
    private const int AddressMaxLength = 106;

    // Monero Base58 alphabet (Bitcoin-style; no 0/O/I/l). Checksum stays deferred until a Keccak helper exists.
    private const string Base58Alphabet =
        "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    /// <inheritdoc />
    public AssetSymbol Symbol { get; } = new("XMR");

    /// <inheritdoc />
    public IReadOnlyList<WalletUiMode> AddModes { get; } =
        [WalletUiMode.Managed, WalletUiMode.Unmanaged, WalletUiMode.Watch];

    /// <inheritdoc />
    public bool CanDerive => false;

    /// <inheritdoc />
    public bool UsesServerWallets => true;

    /// <inheritdoc />
    public string? Notes => "Hybrid: managed/unmanaged via /wallets API — native derive deferred";

    /// <inheritdoc />
    public bool IsValidAddress(string address)
    {
        if (address.Length is < AddressMinLength or > AddressMaxLength)
        {
            return false;
        }

        return address.All(character => Base58Alphabet.Contains(character, StringComparison.Ordinal));
    }

    /// <inheritdoc />
    public DerivedAddress? TryDerive(string mnemonic, int accountIndex = 0)
    {
        _ = mnemonic;
        _ = accountIndex;
        return null;
    }

    /// <inheritdoc />
    public Uri BuildReceiveUri(string address, string? amount, string? label, string? message)
    {
        _ = label;
        _ = message;
        return PaymentUri.Account("monero", "tx_amount", address, amount);
    }
}
