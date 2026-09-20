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
    Task<List<Transaction>> GetTransactionHistoryAsync(string walletId, CancellationToken cancellationToken);

    Task<Transaction> PurchaseCryptoAsync(string symbol, decimal amount, CancellationToken cancellationToken);

    Task<Transaction> SendCryptoAsync(string fromWalletId, string toAddress, decimal amount, CancellationToken cancellationToken);

    Task<TransactionStatus> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken);
}
