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
/// Production implementation of IWalletService using HTTP client.
/// Manages cryptocurrency wallets via the CipherBank API.
/// </summary>
public class WalletService : IWalletService
{
    private readonly ILogger<WalletService> _logger;
    private readonly HttpClient _http;
    private readonly IAuthService _auth;

    private const string WalletsEndpoint = "api/v1/wallets";

    public WalletService(ILogger<WalletService> logger, HttpClient http, IAuthService auth)
    {
        _logger = logger;
        _http = http;
        _auth = auth;
    }

    public async Task<List<Wallet>> GetWalletsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching all wallets from API");

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await _http.GetAsync(WalletsEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var wallets = await response.Content.ReadFromJsonAsync<List<Wallet>>(cancellationToken: cancellationToken);

            if (wallets == null)
            {
                _logger.LogWarning("API returned null response for wallets");
                return [];
            }

            _logger.LogInformation("Retrieved {Count} wallets from API", wallets.Count);
            return wallets;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching wallets: {StatusCode}", ex.StatusCode);
            throw new InvalidOperationException("Failed to retrieve wallets from server", ex);
        }
    }

    public async Task<Wallet> GetWalletAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        _logger.LogDebug("Fetching wallet {WalletId} from API", id);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var endpoint = $"{WalletsEndpoint}/{Uri.EscapeDataString(id)}";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var wallet = await response.Content.ReadFromJsonAsync<Wallet>(cancellationToken: cancellationToken);

            if (wallet == null)
            {
                _logger.LogWarning("API returned null response for wallet {WalletId}", id);
                throw new KeyNotFoundException($"Wallet '{id}' not found");
            }

            _logger.LogInformation("Retrieved wallet {WalletId} with balance {Balance} {Symbol}",
                wallet.Id, wallet.Balance, wallet.CryptoSymbol);
            return wallet;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Wallet {WalletId} not found", id);
            throw new KeyNotFoundException($"Wallet '{id}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching wallet {WalletId}: {StatusCode}", id, ex.StatusCode);
            throw new InvalidOperationException($"Failed to retrieve wallet from server", ex);
        }
    }

    public async Task<decimal> GetWalletBalanceAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        _logger.LogDebug("Fetching balance for wallet {WalletId} from API", id);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var endpoint = $"{WalletsEndpoint}/{Uri.EscapeDataString(id)}/balance";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<BalanceResponse>(cancellationToken: cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("API returned null response for wallet {WalletId} balance", id);
                throw new InvalidOperationException($"Failed to retrieve balance for wallet '{id}'");
            }

            _logger.LogInformation("Wallet {WalletId} balance: {Balance}", id, result.Balance);
            return result.Balance;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Wallet {WalletId} not found", id);
            throw new KeyNotFoundException($"Wallet '{id}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching balance for wallet {WalletId}: {StatusCode}", id, ex.StatusCode);
            throw new InvalidOperationException($"Failed to retrieve wallet balance from server", ex);
        }
    }

    public async Task<Wallet> CreateWalletAsync(string cryptoSymbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cryptoSymbol);

        _logger.LogDebug("Creating wallet for {Symbol} via API", cryptoSymbol);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var request = new CreateWalletRequest(cryptoSymbol.ToUpperInvariant());
            var response = await _http.PostAsJsonAsync(WalletsEndpoint, request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var wallet = await response.Content.ReadFromJsonAsync<Wallet>(cancellationToken: cancellationToken);

            if (wallet == null)
            {
                _logger.LogWarning("API returned null response when creating wallet for {Symbol}", cryptoSymbol);
                throw new InvalidOperationException($"Failed to create wallet for {cryptoSymbol}");
            }

            _logger.LogInformation("Created wallet {WalletId} for {Symbol} at address {Address}",
                wallet.Id, wallet.CryptoSymbol, wallet.Address);
            return wallet;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            _logger.LogWarning("Wallet for {Symbol} already exists", cryptoSymbol);
            throw new InvalidOperationException($"Wallet for {cryptoSymbol} already exists", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error creating wallet for {Symbol}: {StatusCode}", cryptoSymbol, ex.StatusCode);
            throw new InvalidOperationException($"Failed to create wallet from server", ex);
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
    private record BalanceResponse(decimal Balance);
    private record CreateWalletRequest(string CryptoSymbol);
}
