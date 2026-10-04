// <copyright file="ILocalWalletSeeder.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

namespace CipherBank_app.Wallets;

/// <summary>Seeds derived wallet rows after custody seal (Cora ensureDerivedWallets).</summary>
public interface ILocalWalletSeeder
{
    /// <summary>
    /// Derives and upserts wallet rows for the default symbol set; existing rows are kept.
    /// The mnemonic is used transiently and never persisted.
    /// Use: Low (after seal / restore). Scope: local wallet rows.
    /// </summary>
    Task EnsureDerivedAsync(string mnemonic);

    /// <summary>
    /// Derives and upserts wallet rows for <paramref name="symbols"/>; existing rows are kept.
    /// Use: Low (add-wallet flow). Scope: local wallet rows.
    /// </summary>
    Task EnsureDerivedAsync(string mnemonic, IEnumerable<string> symbols);
}
