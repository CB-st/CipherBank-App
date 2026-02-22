using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Service for managing cryptocurrency transactions.
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// Gets the transaction history for a specific wallet.
    /// </summary>
    /// <param name="walletId">The wallet ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of transactions.</returns>
    Task<List<Transaction>> GetTransactionHistoryAsync(string walletId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Purchases cryptocurrency with fiat currency.
    /// </summary>
    /// <param name="symbol">The cryptocurrency symbol to purchase.</param>
    /// <param name="amount">The amount of cryptocurrency to purchase.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The purchase transaction.</returns>
    Task<Transaction> PurchaseCryptoAsync(string symbol, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends cryptocurrency from one wallet to another address.
    /// </summary>
    /// <param name="fromWalletId">The source wallet ID.</param>
    /// <param name="toAddress">The destination blockchain address.</param>
    /// <param name="amount">The amount to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The send transaction.</returns>
    Task<Transaction> SendCryptoAsync(string fromWalletId, string toAddress, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a transaction.
    /// </summary>
    /// <param name="transactionId">The transaction ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transaction status.</returns>
    Task<TransactionStatus> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken = default);
}
