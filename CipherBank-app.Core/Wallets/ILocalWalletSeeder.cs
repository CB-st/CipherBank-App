// <copyright file="ILocalWalletSeeder.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Wallets;

/// <summary>Seeds derived wallet rows after custody seal (Cora ensureDerivedWallets).</summary>
public interface ILocalWalletSeeder
{
    Task EnsureDerivedAsync(string mnemonic);

    Task EnsureDerivedAsync(string mnemonic, IEnumerable<AssetSymbol> symbols);
}
