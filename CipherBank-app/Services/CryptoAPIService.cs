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
/// Production implementation of ICryptoAPIService using HTTP client.
/// Retrieves cryptocurrency market data from the CipherBank API.
/// </summary>
public class CryptoAPIService : ICryptoApiService
{
    private readonly ILogger<CryptoAPIService> _logger;
    private readonly HttpClient _http;
    private readonly IAuthService _auth;

    private const string PricesEndpoint = "api/v1/crypto/prices";
    private const string PriceEndpoint = "api/v1/crypto/price";
    private const string HistoryEndpoint = "api/v1/crypto/history";
    private const string SearchEndpoint = "api/v1/crypto/search";

    public CryptoAPIService(ILogger<CryptoAPIService> logger, HttpClient http, IAuthService auth)
    {
        _logger = logger;
        _http = http;
        _auth = auth;
    }

    public async Task<List<CryptoCurrency>> GetCryptoPricesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching all cryptocurrency prices from API");

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await _http.GetAsync(PricesEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var cryptos = await response.Content.ReadFromJsonAsync<List<CryptoCurrency>>(cancellationToken: cancellationToken);

            if (cryptos == null)
            {
                _logger.LogWarning("API returned null response for crypto prices");
                return [];
            }

            _logger.LogInformation("Retrieved {Count} cryptocurrency prices from API", cryptos.Count);
            return cryptos;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching crypto prices: {StatusCode}", ex.StatusCode);
            throw new InvalidOperationException("Failed to retrieve cryptocurrency prices from server", ex);
        }
    }

    public async Task<CryptoCurrency> GetCryptoPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        _logger.LogDebug("Fetching price for {Symbol} from API", symbol);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var endpoint = $"{PriceEndpoint}/{Uri.EscapeDataString(symbol.ToUpperInvariant())}";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var crypto = await response.Content.ReadFromJsonAsync<CryptoCurrency>(cancellationToken: cancellationToken);

            if (crypto == null)
            {
                _logger.LogWarning("API returned null response for {Symbol}", symbol);
                throw new KeyNotFoundException($"Cryptocurrency '{symbol}' not found");
            }

            _logger.LogInformation("Retrieved price for {Symbol}: {Price}", symbol, crypto.CurrentPrice);
            return crypto;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Cryptocurrency {Symbol} not found", symbol);
            throw new KeyNotFoundException($"Cryptocurrency '{symbol}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching price for {Symbol}: {StatusCode}", symbol, ex.StatusCode);
            throw new InvalidOperationException($"Failed to retrieve price for {symbol} from server", ex);
        }
    }

    public async Task<PriceHistory> GetPriceHistoryAsync(string symbol, string period, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        _logger.LogDebug("Fetching price history for {Symbol} over {Period} from API", symbol, period);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var endpoint = $"{HistoryEndpoint}/{Uri.EscapeDataString(symbol.ToUpperInvariant())}?period={Uri.EscapeDataString(period)}";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var history = await response.Content.ReadFromJsonAsync<PriceHistory>(cancellationToken: cancellationToken);

            if (history == null)
            {
                _logger.LogWarning("API returned null response for {Symbol} history", symbol);
                throw new KeyNotFoundException($"Price history for '{symbol}' not found");
            }

            _logger.LogInformation("Retrieved {Count} price points for {Symbol} over {Period}",
                history.PricePoints.Count, symbol, period);
            return history;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Price history for {Symbol} not found", symbol);
            throw new KeyNotFoundException($"Price history for '{symbol}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error fetching history for {Symbol}: {StatusCode}", symbol, ex.StatusCode);
            throw new InvalidOperationException($"Failed to retrieve price history for {symbol} from server", ex);
        }
    }

    public async Task<List<CryptoCurrency>> SearchCryptoAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        _logger.LogDebug("Searching cryptocurrencies for '{Query}' from API", query);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var endpoint = $"{SearchEndpoint}?q={Uri.EscapeDataString(query)}";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var results = await response.Content.ReadFromJsonAsync<List<CryptoCurrency>>(cancellationToken: cancellationToken);

            if (results == null)
            {
                _logger.LogWarning("API returned null response for search query '{Query}'", query);
                return [];
            }

            _logger.LogInformation("Search for '{Query}' returned {Count} results", query, results.Count);
            return results;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error searching for '{Query}': {StatusCode}", query, ex.StatusCode);
            throw new InvalidOperationException($"Failed to search cryptocurrencies from server", ex);
        }
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        // Check if token needs refresh
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
}
