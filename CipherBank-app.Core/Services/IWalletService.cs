// <copyright file="IWalletService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Service for managing cryptocurrency wallets.
/// </summary>
public interface IWalletService
{
    /// <summary>Returns all wallets owned by the current user.</summary>
    /// <returns>The user's wallets.</returns>
    Task<List<Wallet>> GetWalletsAsync() => GetWalletsAsync(CancellationToken.None);

    /// <summary>Returns all wallets owned by the current user.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's wallets.</returns>
    Task<List<Wallet>> GetWalletsAsync(CancellationToken cancellationToken);

    /// <summary>Returns the wallet identified by <paramref name="id"/>.</summary>
    /// <param name="id">Wallet identifier.</param>
    /// <returns>The requested wallet.</returns>
    Task<Wallet> GetWalletAsync(string id) => GetWalletAsync(id, CancellationToken.None);

    /// <summary>Returns the wallet identified by <paramref name="id"/>.</summary>
    /// <param name="id">Wallet identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The requested wallet.</returns>
    Task<Wallet> GetWalletAsync(string id, CancellationToken cancellationToken);

    /// <summary>Returns the current balance for the wallet identified by <paramref name="id"/>.</summary>
    /// <param name="id">Wallet identifier.</param>
    /// <returns>The wallet balance.</returns>
    Task<decimal> GetWalletBalanceAsync(string id) =>
        GetWalletBalanceAsync(id, CancellationToken.None);

    /// <summary>Returns the current balance for the wallet identified by <paramref name="id"/>.</summary>
    /// <param name="id">Wallet identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The wallet balance.</returns>
    Task<decimal> GetWalletBalanceAsync(string id, CancellationToken cancellationToken);

    /// <summary>Creates a wallet for <paramref name="cryptoSymbol"/>.</summary>
    /// <param name="cryptoSymbol">Asset for the new wallet.</param>
    /// <returns>The created wallet.</returns>
    Task<Wallet> CreateWalletAsync(AssetSymbol cryptoSymbol) =>
        CreateWalletAsync(cryptoSymbol, CancellationToken.None);

    /// <summary>Creates a wallet for <paramref name="cryptoSymbol"/>.</summary>
    /// <param name="cryptoSymbol">Asset for the new wallet.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created wallet.</returns>
    Task<Wallet> CreateWalletAsync(
        AssetSymbol cryptoSymbol,
        CancellationToken cancellationToken);
}
