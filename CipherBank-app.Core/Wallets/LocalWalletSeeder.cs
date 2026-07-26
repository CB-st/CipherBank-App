// <copyright file="LocalWalletSeeder.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Persist;

namespace CipherBank_app.Wallets;

/// <summary>Seeds derived wallet rows after custody seal (Cora ensureDerivedWallets).</summary>
public interface ILocalWalletSeeder
{
    Task EnsureDerivedAsync(string mnemonic);

    Task EnsureDerivedAsync(string mnemonic, IEnumerable<string> symbols);
}

/// <inheritdoc />
public sealed class LocalWalletSeeder : ILocalWalletSeeder
{
    private static readonly string[] DefaultSymbols = { "BTC", "ETH" };
    private readonly IWalletRepository _wallets;

    public LocalWalletSeeder(IWalletRepository wallets)
        => _wallets = wallets;

    public Task EnsureDerivedAsync(string mnemonic)
        => EnsureDerivedAsync(mnemonic, DefaultSymbols);

    public async Task EnsureDerivedAsync(string mnemonic, IEnumerable<string> symbols)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        IReadOnlyList<LocalWalletRow> existing = await _wallets.ListAsync().ConfigureAwait(false);
        foreach (string sym in symbols)
        {
            WalletModule module = WalletRegistry.Get(sym);
            if (!module.CanDerive)
            {
                continue;
            }

            if (existing.Any(w => w.Symbol.Equals(sym, StringComparison.OrdinalIgnoreCase)
                                  && w.Kind.Equals("derived", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            DerivedAddress? derived = AddressDerive.Derive(sym, mnemonic);
            if (derived is null)
            {
                continue;
            }

            await _wallets.UpsertAsync(new LocalWalletRow(
                Guid.NewGuid().ToString("N"),
                sym.ToUpperInvariant(),
                $"{sym.ToUpperInvariant()} Primary",
                derived.Address,
                derived.Path,
                derived.AccountIndex,
                "derived",
                DateTimeOffset.UtcNow)).ConfigureAwait(false);
        }
    }
}
