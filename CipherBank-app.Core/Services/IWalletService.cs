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
    Task<List<Wallet>> GetWalletsAsync(CancellationToken cancellationToken = default);

    Task<Wallet> GetWalletAsync(string id, CancellationToken cancellationToken = default);

    Task<decimal> GetWalletBalanceAsync(string id, CancellationToken cancellationToken = default);

    Task<Wallet> CreateWalletAsync(string cryptoSymbol, CancellationToken cancellationToken = default);
}
