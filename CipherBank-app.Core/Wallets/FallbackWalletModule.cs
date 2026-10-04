// <copyright file="FallbackWalletModule.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Wallets;

/// <summary>Watch-only module for tickers that do not have a dedicated chain implementation.</summary>
public sealed class FallbackWalletModule : IWalletModule
{
    private const int GenericAddressMinLength = 8;

    /// <summary>Initializes a new instance of the <see cref="FallbackWalletModule"/> class.</summary>
    public FallbackWalletModule(AssetSymbol symbol)
    {
        Symbol = symbol;
    }

    /// <inheritdoc />
    public AssetSymbol Symbol { get; }

    /// <inheritdoc />
    public IReadOnlyList<WalletUiMode> AddModes { get; } = [WalletUiMode.Watch];

    /// <inheritdoc />
    public bool CanDerive => false;

    /// <inheritdoc />
    public bool UsesServerWallets => false;

    /// <inheritdoc />
    public string? Notes => "No dedicated module — watch address only";

    /// <inheritdoc />
    public bool IsValidAddress(string address) => address.Length >= GenericAddressMinLength;

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
        _ = amount;
        _ = label;
        _ = message;
        return Uri.TryCreate(address, UriKind.Absolute, out Uri? parsed)
            ? parsed
            : new Uri(address, UriKind.Relative);
    }
}
