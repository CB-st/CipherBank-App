// <copyright file="IWalletService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Service for managing cryptocurrency wallets.
/// </summary>
public interface IWalletService
{
    Task<List<Wallet>> GetWalletsAsync(CancellationToken cancellationToken);

    /// <summary>Wallet list for callers with no ambient token. Use: High (Wallet tab). Scope: IWalletService consumers.</summary>
    Task<List<Wallet>> GetWalletsAsync() => GetWalletsAsync(CancellationToken.None);

    Task<Wallet> GetWalletAsync(string id, CancellationToken cancellationToken);

    /// <summary>Single wallet for callers with no ambient token. Use: Medium (Wallet detail). Scope: IWalletService consumers.</summary>
    Task<Wallet> GetWalletAsync(string id) => GetWalletAsync(id, CancellationToken.None);

    Task<decimal> GetWalletBalanceAsync(string id, CancellationToken cancellationToken);

    /// <summary>Wallet balance for callers with no ambient token. Use: Medium (Wallet detail). Scope: IWalletService consumers.</summary>
    Task<decimal> GetWalletBalanceAsync(string id) => GetWalletBalanceAsync(id, CancellationToken.None);

    Task<Wallet> CreateWalletAsync(string cryptoSymbol, CancellationToken cancellationToken);

    /// <summary>Wallet creation for callers with no ambient token. Use: Low (add wallet). Scope: IWalletService consumers.</summary>
    Task<Wallet> CreateWalletAsync(string cryptoSymbol) => CreateWalletAsync(cryptoSymbol, CancellationToken.None);
}
