// <copyright file="WalletRegistry.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Wallets;

/// <summary>UI create mode for a wallet module.</summary>
public enum WalletUiMode
{
    Derive,
    Watch,
    Managed,
    Unmanaged,
}

/// <summary>Portfolio source after create.</summary>
public enum WalletSource
{
    Local,
    Watch,
    Server,
}

/// <summary>Modular wallet registry.</summary>
public static class WalletRegistry
{
    private static readonly Dictionary<string, WalletModule> Modules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BTC"] = new WalletModule
        {
            Symbol = "BTC",
            AddModes = new[] { WalletUiMode.Derive, WalletUiMode.Watch },
            CanDerive = true,
            UsesServerWallets = false,
            Notes = "BIP84 native segwit from on-device BIP39",
        },
        ["ETH"] = new WalletModule
        {
            Symbol = "ETH",
            AddModes = new[] { WalletUiMode.Derive, WalletUiMode.Watch },
            CanDerive = true,
            UsesServerWallets = false,
            Notes = "BIP44 m/44'/60'/0'/0/i from on-device BIP39",
        },
        ["LTC"] = new WalletModule
        {
            Symbol = "LTC",
            AddModes = new[] { WalletUiMode.Derive, WalletUiMode.Watch },
            CanDerive = true,
            UsesServerWallets = false,
            Notes = "BIP84 native segwit m/84'/2'/0'/0/i",
        },
        ["DOGE"] = new WalletModule
        {
            Symbol = "DOGE",
            AddModes = new[] { WalletUiMode.Derive, WalletUiMode.Watch },
            CanDerive = true,
            UsesServerWallets = false,
            Notes = "BIP44 m/44'/3'/0'/0/i P2PKH",
        },
        ["XMR"] = new WalletModule
        {
            Symbol = "XMR",
            AddModes = new[] { WalletUiMode.Managed, WalletUiMode.Unmanaged, WalletUiMode.Watch },
            CanDerive = false,
            UsesServerWallets = true,
            Notes = "Hybrid: managed/unmanaged via /wallets API — native derive deferred",
        },
    };

    public static WalletModule Get(string symbol)
    {
        string sym = symbol.ToUpperInvariant();
        if (Modules.TryGetValue(sym, out WalletModule? mod))
        {
            return mod;
        }

        return new WalletModule
        {
            Symbol = sym,
            AddModes = new[] { WalletUiMode.Watch },
            CanDerive = AddressDerive.IsDerivable(sym),
            UsesServerWallets = false,
            Notes = "No dedicated module — watch address only",
        };
    }

    public static IReadOnlyList<WalletModule> All()
        => Modules.Values.OrderBy(m => m.Symbol).ToList();
}

/// <summary>Per-asset light-wallet module (Cora registry.ts).</summary>
public sealed class WalletModule
{
    public required string Symbol { get; init; }

    public required IReadOnlyList<WalletUiMode> AddModes { get; init; }

    public bool CanDerive { get; init; }

    public bool UsesServerWallets { get; init; }

    public string? Notes { get; init; }

    public WalletSource SourceFor(WalletUiMode mode)
        => mode switch
        {
            WalletUiMode.Watch => WalletSource.Watch,
            WalletUiMode.Managed => WalletSource.Server,
            WalletUiMode.Unmanaged => UsesServerWallets ? WalletSource.Server : WalletSource.Local,
            _ => WalletSource.Local,
        };
}
