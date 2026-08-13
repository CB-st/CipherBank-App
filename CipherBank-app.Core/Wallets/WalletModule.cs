// <copyright file="WalletModule.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Wallets;

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
