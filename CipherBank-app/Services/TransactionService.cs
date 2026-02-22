using System;
using System.Collections.Generic;
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
public class TransactionService : ITransactionService
{
    private readonly ILogger<TransactionService> _logger;
    private readonly HttpClient _http;
    private readonly IAuthService _auth;

    private const string TransactionsEndpoint = "api/v1/transactions";
    private const string PurchaseEndpoint = "api/v1/transactions/purchase";
    private const string SendEndpoint = "api/v1/transactions/send";

    public TransactionService(ILogger<TransactionService> logger, HttpClient http, IAuthService auth)
    {
        _logger = logger;
        _http = http;
        _auth = auth;
    }

    public async Task<List<Transaction>> GetTransactionHistoryAsync(string walletId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(walletId);

        _logger.LogDebug("Fetching transaction history for wallet {WalletId} from API", walletId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var endpoint = $"{TransactionsEndpoint}?walletId={Uri.EscapeDataString(walletId)}";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var transactions = await response.Content.ReadFromJsonAsync<List<Transaction>>(cancellationToken: cancellationToken);

            if (transactions == null)
            {
                _logger.LogWarning("API returned null response for wallet {WalletId} transactions", walletId);
                return [];
            }

            _logger.LogInformation("Retrieved {Count} transactions for wallet {WalletId}",
                transactions.Count, walletId);
            return transactions;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Wallet {WalletId} not found", walletId);
            throw new KeyNotFoundException($"Wallet '{walletId}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching transactions for wallet {WalletId}: {StatusCode}",
                walletId, ex.StatusCode);
            throw new InvalidOperationException("Failed to retrieve transaction history from server", ex);
        }
    }

    public async Task<Transaction> PurchaseCryptoAsync(string symbol, decimal amount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        _logger.LogDebug("Processing purchase of {Amount} {Symbol} via API", amount, symbol);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var request = new PurchaseRequest(symbol.ToUpperInvariant(), amount);
            var response = await _http.PostAsJsonAsync(PurchaseEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var transaction = await response.Content.ReadFromJsonAsync<Transaction>(cancellationToken: cancellationToken);

            if (transaction == null)
            {
                _logger.LogWarning("API returned null response for purchase of {Amount} {Symbol}",
                    amount, symbol);
                throw new InvalidOperationException($"Failed to complete purchase of {amount} {symbol}");
            }

            _logger.LogInformation(
                "Purchase completed: {Amount} {Symbol} with fee {Fee}. Transaction ID: {TransactionId}",
                transaction.Amount, transaction.CryptoSymbol, transaction.FeeAmount, transaction.Id);

            return transaction;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogWarning("Invalid purchase request for {Amount} {Symbol}", amount, symbol);
            throw new ArgumentException($"Invalid purchase request", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.PaymentRequired)
        {
            _logger.LogWarning("Insufficient funds for purchase of {Amount} {Symbol}", amount, symbol);
            throw new InvalidOperationException("Insufficient funds for purchase", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error processing purchase of {Amount} {Symbol}: {StatusCode}",
                amount, symbol, ex.StatusCode);
            throw new InvalidOperationException("Failed to process purchase from server", ex);
        }
    }

    public async Task<Transaction> SendCryptoAsync(string fromWalletId, string toAddress, decimal amount, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fromWalletId);
        ArgumentException.ThrowIfNullOrWhiteSpace(toAddress);
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        // Basic address validation (additional validation done server-side)
        if (toAddress.Length < 20 || toAddress.Length > 100)
        {
            _logger.LogWarning("Invalid destination address format: {Address}", toAddress);
            throw new ArgumentException("Invalid destination address format", nameof(toAddress));
        }

        _logger.LogDebug("Processing send from wallet {WalletId} to {ToAddress} of {Amount} via API",
            fromWalletId, toAddress, amount);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var request = new SendRequest(fromWalletId, toAddress, amount);
            var response = await _http.PostAsJsonAsync(SendEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var transaction = await response.Content.ReadFromJsonAsync<Transaction>(cancellationToken: cancellationToken);

            if (transaction == null)
            {
                _logger.LogWarning("API returned null response for send from wallet {WalletId}",
                    fromWalletId);
                throw new InvalidOperationException($"Failed to complete send transaction");
            }

            _logger.LogInformation(
                "Send initiated: {Amount} {Symbol} to {ToAddress}. Fee: {Fee}. Transaction ID: {TransactionId}",
                transaction.Amount, transaction.CryptoSymbol, toAddress,
                transaction.FeeAmount, transaction.Id);

            return transaction;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.BadRequest)
        {
            _logger.LogWarning("Invalid send request from wallet {WalletId}", fromWalletId);
            throw new ArgumentException("Invalid send request", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Wallet {WalletId} not found", fromWalletId);
            throw new KeyNotFoundException($"Wallet '{fromWalletId}' not found", ex);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity)
        {
            _logger.LogWarning("Insufficient balance in wallet {WalletId}", fromWalletId);
            throw new InvalidOperationException("Insufficient balance for transaction", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error processing send from wallet {WalletId}: {StatusCode}",
                fromWalletId, ex.StatusCode);
            throw new InvalidOperationException("Failed to process send from server", ex);
        }
    }

    public async Task<TransactionStatus> GetTransactionStatusAsync(string transactionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(transactionId);

        _logger.LogDebug("Fetching status for transaction {TransactionId} from API", transactionId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var endpoint = $"{TransactionsEndpoint}/{Uri.EscapeDataString(transactionId)}/status";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<StatusResponse>(cancellationToken: cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("API returned null response for transaction {TransactionId} status",
                    transactionId);
                throw new InvalidOperationException($"Failed to retrieve status for transaction '{transactionId}'");
            }

            _logger.LogInformation("Transaction {TransactionId} status: {Status}",
                transactionId, result.Status);
            return result.Status;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Transaction {TransactionId} not found", transactionId);
            throw new KeyNotFoundException($"Transaction '{transactionId}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching status for transaction {TransactionId}: {StatusCode}",
                transactionId, ex.StatusCode);
            throw new InvalidOperationException("Failed to retrieve transaction status from server", ex);
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (await _auth.IsTokenExpiredAsync())
        {
            var token = await _auth.GetStoredTokenAsync();
            if (token != null)
            {
                _logger.LogInformation("Token expired, attempting refresh");
                await _auth.RefreshAsync(token.RefreshToken, cancellationToken);
            }
            else
            {
                _logger.LogWarning("No stored token available for refresh");
                throw new UnauthorizedAccessException("Authentication required. Please log in.");
            }
        }
    }

    // Internal DTOs for API communication
    private record PurchaseRequest(string Symbol, decimal Amount);
    private record SendRequest(string FromWalletId, string ToAddress, decimal Amount);
    private record StatusResponse(TransactionStatus Status);
}
