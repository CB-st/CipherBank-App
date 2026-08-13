// <copyright file="WalletService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
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
/// Production implementation of IWalletService using HTTP client.
/// Manages cryptocurrency wallets via the CipherBank API.
/// </summary>
public sealed partial class WalletService : IWalletService
{
    private const string WalletsEndpoint = "api/v1/wallets";

    private readonly ILogger<WalletService> _logger;
    private readonly HttpClient _http;

    public WalletService(ILogger<WalletService> logger, HttpClient http)
    {
        _logger = logger;
        _http = http;
    }

    public async Task<List<Wallet>> GetWalletsAsync(CancellationToken cancellationToken = default)
    {
        LogFetchingAllWallets(_logger);

        try
        {
            var response = await _http.GetAsync(WalletsEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var wallets = await response.Content.ReadFromJsonAsync<List<Wallet>>(cancellationToken: cancellationToken);

            if (wallets == null)
            {
                LogNullResponseForWallets(_logger);
                return [];
            }

            LogRetrievedWallets(_logger, wallets.Count);
            return wallets;
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorFetchingWallets(_logger, ex, ex.StatusCode);
            throw new InvalidOperationException("Failed to retrieve wallets from server", ex);
        }
    }

    public async Task<Wallet> GetWalletAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        LogFetchingWallet(_logger, id);

        try
        {
            var endpoint = $"{WalletsEndpoint}/{Uri.EscapeDataString(id)}";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var wallet = await response.Content.ReadFromJsonAsync<Wallet>(cancellationToken: cancellationToken);

            if (wallet == null)
            {
                LogNullResponseForWallet(_logger, id);
                throw new KeyNotFoundException($"Wallet '{id}' not found");
            }

            LogRetrievedWallet(_logger, wallet.Id, wallet.Balance, wallet.CryptoSymbol);
            return wallet;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            LogWalletNotFound(_logger, id);
            throw new KeyNotFoundException($"Wallet '{id}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorFetchingWallet(_logger, ex, id, ex.StatusCode);
            throw new InvalidOperationException("Failed to retrieve wallet from server", ex);
        }
    }

    public async Task<decimal> GetWalletBalanceAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        LogFetchingBalance(_logger, id);

        try
        {
            var endpoint = $"{WalletsEndpoint}/{Uri.EscapeDataString(id)}/balance";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BalanceResponse>(cancellationToken: cancellationToken);

            if (result == null)
            {
                LogNullResponseForBalance(_logger, id);
                throw new InvalidOperationException($"Failed to retrieve balance for wallet '{id}'");
            }

            LogWalletBalance(_logger, id, result.Balance);
            return result.Balance;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            LogWalletNotFound(_logger, id);
            throw new KeyNotFoundException($"Wallet '{id}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorFetchingBalance(_logger, ex, id, ex.StatusCode);
            throw new InvalidOperationException("Failed to retrieve wallet balance from server", ex);
        }
    }

    public async Task<Wallet> CreateWalletAsync(string cryptoSymbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cryptoSymbol);

        LogCreatingWallet(_logger, cryptoSymbol);

        try
        {
            var request = new CreateWalletRequest(cryptoSymbol.ToUpperInvariant());
            var response = await _http.PostAsJsonAsync(WalletsEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var wallet = await response.Content.ReadFromJsonAsync<Wallet>(cancellationToken: cancellationToken);

            if (wallet == null)
            {
                LogNullResponseForCreateWallet(_logger, cryptoSymbol);
                throw new InvalidOperationException($"Failed to create wallet for {cryptoSymbol}");
            }

            LogWalletCreated(_logger, wallet.Id, wallet.CryptoSymbol, wallet.Address);
            return wallet;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Conflict)
        {
            LogWalletAlreadyExists(_logger, cryptoSymbol);
            throw new InvalidOperationException($"Wallet for {cryptoSymbol} already exists", ex);
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorCreatingWallet(_logger, ex, cryptoSymbol, ex.StatusCode);
            throw new InvalidOperationException("Failed to create wallet from server", ex);
        }
    }

    // Internal DTOs for API communication
    private sealed record BalanceResponse(decimal Balance);

    private sealed record CreateWalletRequest(string CryptoSymbol);

#pragma warning disable SA1201 // Elements should appear in the correct order - LoggerMessage partial methods must be in the class
    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching all wallets from API")]
    private static partial void LogFetchingAllWallets(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response for wallets")]
    private static partial void LogNullResponseForWallets(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {Count} wallets from API")]
    private static partial void LogRetrievedWallets(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error fetching wallets: {StatusCode}")]
    private static partial void LogHttpErrorFetchingWallets(ILogger logger, Exception ex, HttpStatusCode? statusCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching wallet {WalletId} from API")]
    private static partial void LogFetchingWallet(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response for wallet {WalletId}")]
    private static partial void LogNullResponseForWallet(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved wallet {WalletId} with balance {Balance} {Symbol}")]
    private static partial void LogRetrievedWallet(ILogger logger, string walletId, decimal balance, string symbol);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Wallet {WalletId} not found")]
    private static partial void LogWalletNotFound(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error fetching wallet {WalletId}: {StatusCode}")]
    private static partial void LogHttpErrorFetchingWallet(ILogger logger, Exception ex, string walletId, HttpStatusCode? statusCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching balance for wallet {WalletId} from API")]
    private static partial void LogFetchingBalance(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response for wallet {WalletId} balance")]
    private static partial void LogNullResponseForBalance(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Wallet {WalletId} balance: {Balance}")]
    private static partial void LogWalletBalance(ILogger logger, string walletId, decimal balance);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error fetching balance for wallet {WalletId}: {StatusCode}")]
    private static partial void LogHttpErrorFetchingBalance(ILogger logger, Exception ex, string walletId, HttpStatusCode? statusCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Creating wallet for {Symbol} via API")]
    private static partial void LogCreatingWallet(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response when creating wallet for {Symbol}")]
    private static partial void LogNullResponseForCreateWallet(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created wallet {WalletId} for {Symbol} at address {Address}")]
    private static partial void LogWalletCreated(ILogger logger, string walletId, string symbol, string address);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Wallet for {Symbol} already exists")]
    private static partial void LogWalletAlreadyExists(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error creating wallet for {Symbol}: {StatusCode}")]
    private static partial void LogHttpErrorCreatingWallet(ILogger logger, Exception ex, string symbol, HttpStatusCode? statusCode);
#pragma warning restore SA1201 // Elements should appear in the correct order
}
