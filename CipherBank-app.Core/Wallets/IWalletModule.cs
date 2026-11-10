// <copyright file="IWalletModule.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Wallets;

/// <summary>
/// One asset's create modes, derivation, address check, and receive URI.
/// Use: High (add-wallet / receive). Scope: wallet registry.
/// </summary>
public interface IWalletModule
{
    /// <summary>Gets the normalized app ticker this module owns.</summary>
    AssetSymbol Symbol { get; }

    /// <summary>Gets the create actions the UI may offer for this asset.</summary>
    IReadOnlyList<WalletUiMode> AddModes { get; }

    /// <summary>Gets whether an on-device BIP39 seed can derive a receive address.</summary>
    bool CanDerive { get; }

    /// <summary>
    /// Gets whether managed and unmanaged creates are server wallets. When false,
    /// <see cref="WalletUiMode.Unmanaged"/> stays on the device.
    /// </summary>
    bool UsesServerWallets { get; }

    /// <summary>Gets optional operator notes for the module.</summary>
    string? Notes { get; }

    /// <summary>Returns true when <paramref name="address"/> is an acceptable watch destination.</summary>
    bool IsValidAddress(string address);

    /// <summary>Derives account 0, or null when this module cannot derive.</summary>
    DerivedAddress? TryDerive(string mnemonic) => TryDerive(mnemonic, 0);

    /// <summary>
    /// Derives the account address, or null when this module cannot derive.
    /// </summary>
    DerivedAddress? TryDerive(string mnemonic, int accountIndex);

    /// <summary>Builds the receive URI for a non-empty address.</summary>
    Uri BuildReceiveUri(string address, string? amount, string? label, string? message);
}
