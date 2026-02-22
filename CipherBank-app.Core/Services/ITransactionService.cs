using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// Service for managing cryptocurrency transactions.
/// </summary>
public interface ITransactionService
{
    Task<List<Transaction>> GetTransactionHistoryAsync(string walletId, CancellationToken cancellationToken = default);
    Task<Transaction> PurchaseCryptoAsync(string symbol, decimal amount, CancellationToken cancellationToken = default);
    Task<Transaction> SendCryptoAsync(string fromWalletId, string toAddress, decimal amount, CancellationToken cancellationToken = default);
    Task<TransactionStatus> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken = default);
}
