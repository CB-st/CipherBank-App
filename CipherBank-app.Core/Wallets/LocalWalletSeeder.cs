// <copyright file="LocalWalletSeeder.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using CipherBank_app.Models;
using CipherBank_app.Persist;

namespace CipherBank_app.Wallets;

/// <inheritdoc />
public sealed class LocalWalletSeeder : ILocalWalletSeeder
{
    private static readonly string[] DefaultSymbols = { "BTC", "ETH" };
    private readonly IWalletRepository _wallets;
    private readonly TimeProvider _timeProvider;

    public LocalWalletSeeder(IWalletRepository wallets)
        : this(wallets, TimeProvider.System)
    {
    }

    public LocalWalletSeeder(IWalletRepository wallets, TimeProvider timeProvider)
    {
        _wallets = wallets;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public Task EnsureDerivedAsync(string mnemonic)
        => EnsureDerivedAsync(mnemonic, DefaultSymbols);

    public Task EnsureDerivedAsync(string mnemonic, IEnumerable<string> symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        return EnsureDerivedCoreAsync(mnemonic, symbols);
    }

    /// <summary>
    /// Derives local wallets after argument validation; updates address/path when a derived row
    /// already exists for the symbol but belongs to a different seed (restore/replace).
    /// Use: Medium (EnsureDerivedAsync / FinishCustodySetup). Scope: this seeder.
    /// </summary>
    private async Task EnsureDerivedCoreAsync(string mnemonic, IEnumerable<string> symbols)
    {
        IReadOnlyList<LocalWalletDescriptor> existing = await _wallets.ListAsync().ConfigureAwait(false);
        foreach (string sym in symbols)
        {
            // CanDerive and a null derive are the same gate. TryDerive is the one check.
            if (WalletRegistry.Get(sym).TryDerive(mnemonic) is not DerivedAddress derived)
            {
                continue;
            }

            AssetSymbol symbol = new(sym);
            LocalWalletDescriptor? existingDerived = existing.FirstOrDefault(w =>
                w.Symbol == symbol
                && w.Kind.Equals("derived", StringComparison.OrdinalIgnoreCase));

            if (existingDerived is not null)
            {
                if (string.Equals(existingDerived.Address, derived.Address, StringComparison.Ordinal))
                {
                    continue;
                }

                await _wallets.UpsertAsync(existingDerived with
                {
                    Address = derived.Address,
                    Path = derived.Path,
                    AccountIndex = derived.AccountIndex,
                }).ConfigureAwait(false);
                continue;
            }

            await _wallets.UpsertAsync(new LocalWalletDescriptor(
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                symbol,
                $"{symbol} Primary",
                derived.Address,
                derived.Path,
                derived.AccountIndex,
                "derived",
                _timeProvider.GetUtcNow())).ConfigureAwait(false);
        }
    }
}
