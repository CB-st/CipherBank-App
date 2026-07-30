// <copyright file="LocalWalletSeeder.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;
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
        IReadOnlyList<LocalWalletRow> existing = await _wallets.ListAsync().ConfigureAwait(false);
        foreach (var sym in symbols)
        {
            WalletModule module = WalletRegistry.Get(sym);
            if (!module.CanDerive)
            {
                continue;
            }

            DerivedAddress? derived = AddressDerive.Derive(sym, mnemonic);
            if (derived is null)
            {
                continue;
            }

            LocalWalletRow? existingDerived = existing.FirstOrDefault(w =>
                w.Symbol.Equals(sym, StringComparison.OrdinalIgnoreCase)
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

            await _wallets.UpsertAsync(new LocalWalletRow(
                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                sym.ToUpperInvariant(),
                $"{sym.ToUpperInvariant()} Primary",
                derived.Address,
                derived.Path,
                derived.AccountIndex,
                "derived",
                _timeProvider.GetUtcNow())).ConfigureAwait(false);
        }
    }
}
