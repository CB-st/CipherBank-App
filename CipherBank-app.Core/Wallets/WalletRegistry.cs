// <copyright file="WalletRegistry.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Wallets;

/// <summary>Modular wallet registry. Each supported asset is its own <see cref="IWalletModule"/>.</summary>
public static class WalletRegistry
{
    private static readonly IWalletModule[] Modules =
    [
        new BitcoinWalletModule(),
        new EthereumWalletModule(),
        new LitecoinWalletModule(),
        new DogecoinWalletModule(),
        new MoneroWalletModule(),
    ];

    /// <summary>Returns the module for <paramref name="symbol"/>, or a watch-only fallback.</summary>
    public static IWalletModule Get(AssetSymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        foreach (IWalletModule module in Modules)
        {
            if (module.Symbol == symbol)
            {
                return module;
            }
        }

        return new FallbackWalletModule(symbol);
    }

    /// <summary>Returns dedicated modules in ticker order. Fallbacks are not included.</summary>
    public static IReadOnlyList<IWalletModule> All()
        => Modules.OrderBy(module => module.Symbol.Value, StringComparer.Ordinal).ToList();
}
