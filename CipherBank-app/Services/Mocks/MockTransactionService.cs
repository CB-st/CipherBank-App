// <copyright file="MockTransactionService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Models;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services.Mocks;

/// <summary>
/// Mock implementation of ITransactionService for development and testing.
/// Maintains an in-memory collection of transactions with simulated operations.
/// </summary>
public sealed partial class MockTransactionService : ITransactionService
{
    // Simulated latency range in milliseconds
    private const int MinLatencyMs = 200;
    private const int MaxLatencyMs = 800;

    // Fee percentages for different operations
    private const decimal PurchaseFeePercent = 0.015m; // 1.5%
    private const decimal SendFeePercent = 0.001m; // 0.1%

    private readonly ILogger<MockTransactionService> _logger;
    private readonly MockWalletService _walletService;
    private readonly List<Transaction> _transactions;

    public MockTransactionService(ILogger<MockTransactionService> logger, MockWalletService walletService)
    {
        _logger = logger;
        _walletService = walletService;

        // Initialize with some mock transaction history
        _transactions = GenerateMockTransactionHistory();

        LogInitialized(_logger, _transactions.Count);
    }

    public async Task<List<Transaction>> GetTransactionHistoryAsync(string walletId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(walletId);

        LogGettingTransactionHistory(_logger, walletId);
        await SimulateNetworkDelayAsync(cancellationToken);

        // Get wallet to verify it exists
        var wallet = await _walletService.GetWalletAsync(walletId, cancellationToken);

        var transactions = _transactions
            .Where(t => t.CryptoSymbol.Equals(wallet.CryptoSymbol, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(t => t.Timestamp)
            .ToList();

        LogReturnedTransactions(_logger, transactions.Count, walletId);
        return transactions;
    }

    public async Task<Transaction> PurchaseCryptoAsync(string symbol, decimal amount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(amount));
        }

        LogProcessingPurchase(_logger, amount, symbol);
        await SimulateNetworkDelayAsync(cancellationToken);

        var normalizedSymbol = symbol.ToUpperInvariant();
        var fee = amount * PurchaseFeePercent;

        // Find or create wallet
        var wallet = _walletService.GetWalletBySymbol(normalizedSymbol);
        string toAddress;

        if (wallet == null)
        {
            // Create wallet automatically for purchase
            wallet = await _walletService.CreateWalletAsync(normalizedSymbol, cancellationToken);
        }

        toAddress = wallet.Address;

        // Update wallet balance
        var newBalance = wallet.Balance + amount;
        _walletService.UpdateWalletBalance(wallet.Id, newBalance);

        var transaction = new Transaction(
            GenerateTransactionId(),
            TransactionType.Purchase,
            amount,
            normalizedSymbol,
            null, // No from address for purchases
            toAddress,
            DateTimeOffset.UtcNow,
            TransactionStatus.Confirmed,
            fee);

        _transactions.Add(transaction);

        LogPurchaseCompleted(_logger, amount, normalizedSymbol, fee, transaction.Id);

        return transaction;
    }

    public async Task<Transaction> SendCryptoAsync(string fromWalletId, string toAddress, decimal amount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromWalletId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toAddress);
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(amount));
        }

        LogProcessingSend(_logger, amount, fromWalletId, toAddress);
        await SimulateNetworkDelayAsync(cancellationToken);

        // Validate source wallet and balance
        var wallet = await _walletService.GetWalletAsync(fromWalletId, cancellationToken);
        var fee = amount * SendFeePercent;
        var totalAmount = amount + fee;

        if (wallet.Balance < totalAmount)
        {
            LogInsufficientBalance(_logger, fromWalletId, totalAmount, wallet.Balance);
            throw new InvalidOperationException(
                $"Insufficient balance. Required: {totalAmount:F8} {wallet.CryptoSymbol}, Available: {wallet.Balance:F8} {wallet.CryptoSymbol}");
        }

        // Validate destination address format (basic validation)
        if (!IsValidAddress(toAddress, wallet.CryptoSymbol))
        {
            LogInvalidAddress(_logger, toAddress);
            throw new ArgumentException($"Invalid {wallet.CryptoSymbol} address format", nameof(toAddress));
        }

        // Deduct from wallet
        var newBalance = wallet.Balance - totalAmount;
        _walletService.UpdateWalletBalance(wallet.Id, newBalance);

        // Simulate transaction being initially pending, then confirmed
        var transaction = new Transaction(
            GenerateTransactionId(),
            TransactionType.Send,
            amount,
            wallet.CryptoSymbol,
            wallet.Address,
            toAddress,
            DateTimeOffset.UtcNow,
            TransactionStatus.Pending, // Start as pending
            fee);

        _transactions.Add(transaction);

        // Simulate confirmation after a short delay (in real scenario, would poll blockchain)
        _ = Task.Run(
            async () =>
            {
                await Task.Delay(3000, CancellationToken.None);
                var index = _transactions.FindIndex(t => t.Id == transaction.Id);
                if (index >= 0)
                {
                    _transactions[index] = _transactions[index] with { Status = TransactionStatus.Confirmed };
                    LogTransactionConfirmed(_logger, transaction.Id);
                }
            },
            CancellationToken.None);

        LogSendInitiated(_logger, amount, wallet.CryptoSymbol, toAddress, fee, transaction.Id);

        return transaction;
    }

    public async Task<TransactionStatus> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        LogGettingTransactionStatus(_logger, transactionId);
        await Task.Delay(50, cancellationToken); // Minimal delay for status check

        var transaction = _transactions.FirstOrDefault(t => t.Id == transactionId);
        if (transaction == null)
        {
            LogTransactionNotFound(_logger, transactionId);
            throw new KeyNotFoundException($"Transaction with ID '{transactionId}' not found");
        }

        LogTransactionStatus(_logger, transactionId, transaction.Status);
        return transaction.Status;
    }

    private static List<Transaction> GenerateMockTransactionHistory()
    {
        var transactions = new List<Transaction>();
        var now = DateTimeOffset.UtcNow;

        // Bitcoin transactions
        transactions.Add(new Transaction(
            GenerateTransactionId(),
            TransactionType.Purchase,
            0.10000000m,
            "BTC",
            null,
            "bc1q" + GenerateRandomString(39),
            now.AddDays(-30),
            TransactionStatus.Confirmed,
            0.00150000m));

        transactions.Add(new Transaction(
            GenerateTransactionId(),
            TransactionType.Purchase,
            0.25000000m,
            "BTC",
            null,
            "bc1q" + GenerateRandomString(39),
            now.AddDays(-20),
            TransactionStatus.Confirmed,
            0.00375000m));

        transactions.Add(new Transaction(
            GenerateTransactionId(),
            TransactionType.Receive,
            0.17483921m,
            "BTC",
            "bc1q" + GenerateRandomString(39),
            "bc1q" + GenerateRandomString(39),
            now.AddDays(-10),
            TransactionStatus.Confirmed,
            0.00000000m));

        // Ethereum transactions
        transactions.Add(new Transaction(
            GenerateTransactionId(),
            TransactionType.Purchase,
            2.00000000m,
            "ETH",
            null,
            "0x" + GenerateRandomHex(40),
            now.AddDays(-25),
            TransactionStatus.Confirmed,
            0.03000000m));

        transactions.Add(new Transaction(
            GenerateTransactionId(),
            TransactionType.Purchase,
            1.50000000m,
            "ETH",
            null,
            "0x" + GenerateRandomHex(40),
            now.AddDays(-15),
            TransactionStatus.Confirmed,
            0.02250000m));

        transactions.Add(new Transaction(
            GenerateTransactionId(),
            TransactionType.Send,
            0.50000000m,
            "ETH",
            "0x" + GenerateRandomHex(40),
            "0x" + GenerateRandomHex(40),
            now.AddDays(-5),
            TransactionStatus.Confirmed,
            0.00050000m));

        // Solana transactions
        transactions.Add(new Transaction(
            GenerateTransactionId(),
            TransactionType.Purchase,
            100.00000000m,
            "SOL",
            null,
            GenerateRandomString(44),
            now.AddDays(-12),
            TransactionStatus.Confirmed,
            1.50000000m));

        transactions.Add(new Transaction(
            GenerateTransactionId(),
            TransactionType.Receive,
            25.50000000m,
            "SOL",
            GenerateRandomString(44),
            GenerateRandomString(44),
            now.AddDays(-3),
            TransactionStatus.Confirmed,
            0.00000000m));

        // Dogecoin transactions
        transactions.Add(new Transaction(
            GenerateTransactionId(),
            TransactionType.Purchase,
            10000.00000000m,
            "DOGE",
            null,
            "D" + GenerateRandomString(33),
            now.AddDays(-6),
            TransactionStatus.Confirmed,
            150.00000000m));

        transactions.Add(new Transaction(
            GenerateTransactionId(),
            TransactionType.Receive,
            500.00000000m,
            "DOGE",
            "D" + GenerateRandomString(33),
            "D" + GenerateRandomString(33),
            now.AddDays(-1),
            TransactionStatus.Confirmed,
            0.00000000m));

        return transactions;
    }

    private static string GenerateTransactionId() => $"tx_{Guid.NewGuid():N}"[..20];

    private static string GenerateRandomString(int length)
    {
        const string chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
    }

    private static string GenerateRandomHex(int length)
    {
        const string hexChars = "0123456789abcdef";
        return new string(Enumerable.Range(0, length)
            .Select(_ => hexChars[RandomNumberGenerator.GetInt32(hexChars.Length)]).ToArray());
    }

    private static bool IsValidAddress(string address, string symbol) => symbol.ToUpperInvariant() switch
    {
        "BTC" => address.StartsWith("bc1", StringComparison.Ordinal) || address.StartsWith('1') || address.StartsWith('3'),
        "ETH" => address.StartsWith("0x", StringComparison.Ordinal) && address.Length == 42,
        "SOL" => address.Length >= 32 && address.Length <= 44,
        "DOGE" => address.StartsWith('D') && address.Length >= 26,
        _ => address.Length >= 20, // Generic validation
    };

    private static async Task SimulateNetworkDelayAsync(CancellationToken cancellationToken)
    {
        var delay = RandomNumberGenerator.GetInt32(MinLatencyMs, MaxLatencyMs);
        await Task.Delay(delay, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "MockTransactionService initialized with {Count} transactions")]
    private static partial void LogInitialized(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting transaction history for wallet {WalletId} (mock)")]
    private static partial void LogGettingTransactionHistory(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Returned {Count} transactions for wallet {WalletId}")]
    private static partial void LogReturnedTransactions(ILogger logger, int count, string walletId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing purchase of {Amount} {Symbol} (mock)")]
    private static partial void LogProcessingPurchase(ILogger logger, decimal amount, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Purchase completed: {Amount} {Symbol} with fee {Fee}. Transaction ID: {TransactionId}")]
    private static partial void LogPurchaseCompleted(ILogger logger, decimal amount, string symbol, decimal fee, string transactionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing send of {Amount} from wallet {WalletId} to {ToAddress} (mock)")]
    private static partial void LogProcessingSend(ILogger logger, decimal amount, string walletId, string toAddress);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Insufficient balance in wallet {WalletId}. Required: {Required}, Available: {Available}")]
    private static partial void LogInsufficientBalance(ILogger logger, string walletId, decimal required, decimal available);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid destination address format: {Address}")]
    private static partial void LogInvalidAddress(ILogger logger, string address);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transaction {TransactionId} confirmed")]
    private static partial void LogTransactionConfirmed(ILogger logger, string transactionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Send initiated: {Amount} {Symbol} to {ToAddress}. Fee: {Fee}. Transaction ID: {TransactionId}")]
    private static partial void LogSendInitiated(ILogger logger, decimal amount, string symbol, string toAddress, decimal fee, string transactionId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting status for transaction {TransactionId} (mock)")]
    private static partial void LogGettingTransactionStatus(ILogger logger, string transactionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transaction {TransactionId} not found")]
    private static partial void LogTransactionNotFound(ILogger logger, string transactionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transaction {TransactionId} status: {Status}")]
    private static partial void LogTransactionStatus(ILogger logger, string transactionId, TransactionStatus status);
}
