// <copyright file="TransactionService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Models;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services;

/// <summary>
/// Production implementation of ITransactionService using HTTP client.
/// Manages cryptocurrency transactions via the CipherBank API.
/// </summary>
public sealed partial class TransactionService : ITransactionService
{
    private const string TransactionsEndpoint = "api/v1/transactions";
    private const string PurchaseEndpoint = "api/v1/transactions/purchase";
    private const string SendEndpoint = "api/v1/transactions/send";

    private readonly ILogger<TransactionService> _logger;
    private readonly HttpClient _http;

    public TransactionService(ILogger<TransactionService> logger, HttpClient http)
    {
        _logger = logger;
        _http = http;
    }

    public async Task<List<Transaction>> GetTransactionHistoryAsync(string walletId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(walletId);

        LogFetchingTransactionHistory(_logger, walletId);

        try
        {
            var endpoint = $"{TransactionsEndpoint}?walletId={Uri.EscapeDataString(walletId)}";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var transactions = await response.Content.ReadFromJsonAsync<List<Transaction>>(cancellationToken: cancellationToken);

            if (transactions == null)
            {
                LogNullResponseForTransactions(_logger, walletId);
                return [];
            }

            LogRetrievedTransactions(_logger, transactions.Count, walletId);
            return transactions;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            LogWalletNotFound(_logger, walletId);
            throw new KeyNotFoundException($"Wallet '{walletId}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorFetchingTransactions(_logger, ex, walletId, ex.StatusCode);
            throw new InvalidOperationException("Failed to retrieve transaction history from server", ex);
        }
    }

    public async Task<Transaction> PurchaseCryptoAsync(string symbol, decimal amount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(amount));
        }

        LogProcessingPurchase(_logger, amount, symbol);

        try
        {
            PurchaseRequest request = new PurchaseRequest(symbol.ToUpperInvariant(), amount);
            var response = await _http.PostAsJsonAsync(PurchaseEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var transaction = await response.Content.ReadFromJsonAsync<Transaction>(cancellationToken: cancellationToken);

            if (transaction == null)
            {
                LogNullResponseForPurchase(_logger, amount, symbol);
                throw new InvalidOperationException($"Failed to complete purchase of {amount} {symbol}");
            }

            LogPurchaseCompleted(_logger, transaction.Amount, transaction.CryptoSymbol, transaction.FeeAmount, transaction.Id);

            return transaction;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            LogInvalidPurchaseRequest(_logger, amount, symbol);
            throw new ArgumentException("Invalid purchase request", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.PaymentRequired)
        {
            LogInsufficientFundsForPurchase(_logger, amount, symbol);
            throw new InvalidOperationException("Insufficient funds for purchase", ex);
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorProcessingPurchase(_logger, ex, amount, symbol, ex.StatusCode);
            throw new InvalidOperationException("Failed to process purchase from server", ex);
        }
    }

    public async Task<Transaction> SendCryptoAsync(string fromWalletId, string toAddress, decimal amount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromWalletId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toAddress);
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be positive", nameof(amount));
        }

        // Basic address validation (additional validation done server-side)
        if (toAddress.Length < 20 || toAddress.Length > 100)
        {
            LogInvalidDestinationAddress(_logger, toAddress);
            throw new ArgumentException("Invalid destination address format", nameof(toAddress));
        }

        LogProcessingSend(_logger, fromWalletId, toAddress, amount);

        try
        {
            SendRequest request = new SendRequest(fromWalletId, toAddress, amount);
            var response = await _http.PostAsJsonAsync(SendEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var transaction = await response.Content.ReadFromJsonAsync<Transaction>(cancellationToken: cancellationToken);

            if (transaction == null)
            {
                LogNullResponseForSend(_logger, fromWalletId);
                throw new InvalidOperationException("Failed to complete send transaction");
            }

            LogSendInitiated(_logger, transaction.Amount, transaction.CryptoSymbol, toAddress, transaction.FeeAmount, transaction.Id);

            return transaction;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.BadRequest)
        {
            LogInvalidSendRequest(_logger, fromWalletId);
            throw new ArgumentException("Invalid send request", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            LogWalletNotFound(_logger, fromWalletId);
            throw new KeyNotFoundException($"Wallet '{fromWalletId}' not found", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.UnprocessableEntity)
        {
            LogInsufficientBalance(_logger, fromWalletId);
            throw new InvalidOperationException("Insufficient balance for transaction", ex);
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorProcessingSend(_logger, ex, fromWalletId, ex.StatusCode);
            throw new InvalidOperationException("Failed to process send from server", ex);
        }
    }

    public async Task<TransactionStatus> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        LogFetchingTransactionStatus(_logger, transactionId);

        try
        {
            var endpoint = $"{TransactionsEndpoint}/{Uri.EscapeDataString(transactionId)}/status";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<StatusResponse>(cancellationToken: cancellationToken);

            if (result == null)
            {
                LogNullResponseForTransactionStatus(_logger, transactionId);
                throw new InvalidOperationException($"Failed to retrieve status for transaction '{transactionId}'");
            }

            LogTransactionStatus(_logger, transactionId, result.Status);
            return result.Status;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            LogTransactionNotFound(_logger, transactionId);
            throw new KeyNotFoundException($"Transaction '{transactionId}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorFetchingTransactionStatus(_logger, ex, transactionId, ex.StatusCode);
            throw new InvalidOperationException("Failed to retrieve transaction status from server", ex);
        }
    }

    // Internal DTOs for API communication
    private sealed record PurchaseRequest(string Symbol, decimal Amount);

    private sealed record SendRequest(string FromWalletId, string ToAddress, decimal Amount);

    private sealed record StatusResponse(TransactionStatus Status);

#pragma warning disable SA1201 // Elements should appear in the correct order - LoggerMessage partial methods must be in the class
    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching transaction history for wallet {WalletId} from API")]
    private static partial void LogFetchingTransactionHistory(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response for wallet {WalletId} transactions")]
    private static partial void LogNullResponseForTransactions(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {Count} transactions for wallet {WalletId}")]
    private static partial void LogRetrievedTransactions(ILogger logger, int count, string walletId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Wallet {WalletId} not found")]
    private static partial void LogWalletNotFound(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error fetching transactions for wallet {WalletId}: {StatusCode}")]
    private static partial void LogHttpErrorFetchingTransactions(ILogger logger, Exception ex, string walletId, HttpStatusCode? statusCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing purchase of {Amount} {Symbol} via API")]
    private static partial void LogProcessingPurchase(ILogger logger, decimal amount, string symbol);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response for purchase of {Amount} {Symbol}")]
    private static partial void LogNullResponseForPurchase(ILogger logger, decimal amount, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Purchase completed: {Amount} {Symbol} with fee {Fee}. Transaction ID: {TransactionId}")]
    private static partial void LogPurchaseCompleted(ILogger logger, decimal amount, string symbol, decimal fee, string transactionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid purchase request for {Amount} {Symbol}")]
    private static partial void LogInvalidPurchaseRequest(ILogger logger, decimal amount, string symbol);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Insufficient funds for purchase of {Amount} {Symbol}")]
    private static partial void LogInsufficientFundsForPurchase(ILogger logger, decimal amount, string symbol);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error processing purchase of {Amount} {Symbol}: {StatusCode}")]
    private static partial void LogHttpErrorProcessingPurchase(ILogger logger, Exception ex, decimal amount, string symbol, HttpStatusCode? statusCode);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid destination address format: {Address}")]
    private static partial void LogInvalidDestinationAddress(ILogger logger, string address);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Processing send from wallet {WalletId} to {ToAddress} of {Amount} via API")]
    private static partial void LogProcessingSend(ILogger logger, string walletId, string toAddress, decimal amount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response for send from wallet {WalletId}")]
    private static partial void LogNullResponseForSend(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Send initiated: {Amount} {Symbol} to {ToAddress}. Fee: {Fee}. Transaction ID: {TransactionId}")]
    private static partial void LogSendInitiated(ILogger logger, decimal amount, string symbol, string toAddress, decimal fee, string transactionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid send request from wallet {WalletId}")]
    private static partial void LogInvalidSendRequest(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Insufficient balance in wallet {WalletId}")]
    private static partial void LogInsufficientBalance(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error processing send from wallet {WalletId}: {StatusCode}")]
    private static partial void LogHttpErrorProcessingSend(ILogger logger, Exception ex, string walletId, HttpStatusCode? statusCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching status for transaction {TransactionId} from API")]
    private static partial void LogFetchingTransactionStatus(ILogger logger, string transactionId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response for transaction {TransactionId} status")]
    private static partial void LogNullResponseForTransactionStatus(ILogger logger, string transactionId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Transaction {TransactionId} status: {Status}")]
    private static partial void LogTransactionStatus(ILogger logger, string transactionId, TransactionStatus status);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Transaction {TransactionId} not found")]
    private static partial void LogTransactionNotFound(ILogger logger, string transactionId);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error fetching status for transaction {TransactionId}: {StatusCode}")]
    private static partial void LogHttpErrorFetchingTransactionStatus(ILogger logger, Exception ex, string transactionId, HttpStatusCode? statusCode);
#pragma warning restore SA1201 // Elements should appear in the correct order
}
