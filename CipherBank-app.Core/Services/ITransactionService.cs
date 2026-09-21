// <copyright file="ITransactionService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Service for managing cryptocurrency transactions.
/// </summary>
public interface ITransactionService
{
    /// <summary>Returns transaction history for <paramref name="walletId"/>.</summary>
    /// <param name="walletId">Wallet identifier.</param>
    /// <returns>The wallet's transactions.</returns>
    Task<List<Transaction>> GetTransactionHistoryAsync(string walletId) =>
        GetTransactionHistoryAsync(walletId, CancellationToken.None);

    /// <summary>Returns transaction history for <paramref name="walletId"/>.</summary>
    /// <param name="walletId">Wallet identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The wallet's transactions.</returns>
    Task<List<Transaction>> GetTransactionHistoryAsync(
        string walletId,
        CancellationToken cancellationToken);

    /// <summary>Purchases <paramref name="amount"/> of <paramref name="symbol"/>.</summary>
    /// <param name="symbol">Asset to purchase.</param>
    /// <param name="amount">Asset amount to purchase.</param>
    /// <returns>The resulting transaction.</returns>
    Task<Transaction> PurchaseCryptoAsync(AssetSymbol symbol, decimal amount) =>
        PurchaseCryptoAsync(symbol, amount, CancellationToken.None);

    /// <summary>Purchases <paramref name="amount"/> of <paramref name="symbol"/>.</summary>
    /// <param name="symbol">Asset to purchase.</param>
    /// <param name="amount">Asset amount to purchase.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resulting transaction.</returns>
    Task<Transaction> PurchaseCryptoAsync(
        AssetSymbol symbol,
        decimal amount,
        CancellationToken cancellationToken);

    /// <summary>Sends an asset amount from a wallet to an address.</summary>
    /// <param name="fromWalletId">Source wallet identifier.</param>
    /// <param name="toAddress">Destination address.</param>
    /// <param name="amount">Asset amount to send.</param>
    /// <returns>The resulting transaction.</returns>
    Task<Transaction> SendCryptoAsync(
        string fromWalletId,
        string toAddress,
        decimal amount) =>
        SendCryptoAsync(fromWalletId, toAddress, amount, CancellationToken.None);

    /// <summary>Sends an asset amount from a wallet to an address.</summary>
    /// <param name="fromWalletId">Source wallet identifier.</param>
    /// <param name="toAddress">Destination address.</param>
    /// <param name="amount">Asset amount to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resulting transaction.</returns>
    Task<Transaction> SendCryptoAsync(
        string fromWalletId,
        string toAddress,
        decimal amount,
        CancellationToken cancellationToken);

    /// <summary>Returns the current status of <paramref name="transactionId"/>.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <returns>The transaction status.</returns>
    Task<TransactionStatus> GetTransactionStatusAsync(string transactionId) =>
        GetTransactionStatusAsync(transactionId, CancellationToken.None);

    /// <summary>Returns the current status of <paramref name="transactionId"/>.</summary>
    /// <param name="transactionId">Transaction identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction status.</returns>
    Task<TransactionStatus> GetTransactionStatusAsync(
        string transactionId,
        CancellationToken cancellationToken);
}
