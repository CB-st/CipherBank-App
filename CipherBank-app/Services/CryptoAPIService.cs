// <copyright file="CryptoAPIService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Net;
using System.Net.Http.Json;
using CipherBank_app.Models;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services;

/// <summary>
/// Production implementation of ICryptoAPIService using HTTP client.
/// Retrieves cryptocurrency market data from the CipherBank API.
/// </summary>
public sealed partial class CryptoApiService(ILogger<CryptoApiService> logger, HttpClient http) : ICryptoApiService
{
    private const string PricesEndpoint = "api/v1/crypto/prices";
    private const string PriceEndpoint = "api/v1/crypto/price";
    private const string HistoryEndpoint = "api/v1/crypto/history";
    private const string SearchEndpoint = "api/v1/crypto/search";

    public async Task<List<CryptoCurrency>> GetCryptoPricesAsync(CancellationToken cancellationToken)
    {
        LogFetchingAllPrices(logger);

        try
        {
            HttpResponseMessage response = await http.GetAsync(PricesEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            List<CryptoCurrency>? cryptos = await response.Content.ReadFromJsonAsync<List<CryptoCurrency>>(cancellationToken: cancellationToken);

            if (cryptos == null)
            {
                LogNullResponseForPrices(logger);
                return [];
            }

            LogRetrievedPrices(logger, cryptos.Count);
            return cryptos;
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorFetchingPrices(logger, ex, ex.StatusCode);
            throw new InvalidOperationException("Failed to retrieve cryptocurrency prices from server", ex);
        }
    }

    public async Task<CryptoCurrency> GetCryptoPriceAsync(AssetSymbol symbol, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(symbol);

        LogFetchingPriceForSymbol(logger, symbol.Value);

        try
        {
            var endpoint = $"{PriceEndpoint}/{Uri.EscapeDataString(symbol.Value)}";
            HttpResponseMessage response = await http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            CryptoCurrency? crypto = await response.Content.ReadFromJsonAsync<CryptoCurrency>(cancellationToken: cancellationToken);

            if (crypto == null)
            {
                LogNullResponseForSymbol(logger, symbol.Value);
                throw new KeyNotFoundException($"Cryptocurrency '{symbol}' not found");
            }

            LogRetrievedPriceForSymbol(logger, symbol.Value, crypto.CurrentPrice);
            return crypto;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            LogSymbolNotFound(logger, symbol.Value);
            throw new KeyNotFoundException($"Cryptocurrency '{symbol}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorFetchingSymbolPrice(logger, ex, symbol.Value, ex.StatusCode);
            throw new InvalidOperationException($"Failed to retrieve price for {symbol} from server", ex);
        }
    }

    public async Task<PriceHistory> GetPriceHistoryAsync(AssetSymbol symbol, string period, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        LogFetchingHistory(logger, symbol.Value, period);

        try
        {
            var endpoint = $"{HistoryEndpoint}/{Uri.EscapeDataString(symbol.Value)}?period={Uri.EscapeDataString(period)}";
            HttpResponseMessage response = await http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            PriceHistory? history = await response.Content.ReadFromJsonAsync<PriceHistory>(cancellationToken: cancellationToken);

            if (history == null)
            {
                LogNullResponseForHistory(logger, symbol.Value);
                throw new KeyNotFoundException($"Price history for '{symbol}' not found");
            }

            LogRetrievedHistory(logger, history.PricePoints.Count, symbol.Value, period);
            return history;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            LogHistoryNotFound(logger, symbol.Value);
            throw new KeyNotFoundException($"Price history for '{symbol}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorFetchingHistory(logger, ex, symbol.Value, ex.StatusCode);
            throw new InvalidOperationException($"Failed to retrieve price history for {symbol} from server", ex);
        }
    }

    public async Task<List<CryptoCurrency>> SearchCryptoAsync(string query, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        LogSearchingCrypto(logger, query);

        try
        {
            var endpoint = $"{SearchEndpoint}?q={Uri.EscapeDataString(query)}";
            HttpResponseMessage response = await http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            List<CryptoCurrency>? results = await response.Content.ReadFromJsonAsync<List<CryptoCurrency>>(cancellationToken: cancellationToken);

            if (results == null)
            {
                LogNullResponseForSearch(logger, query);
                return [];
            }

            LogSearchResults(logger, query, results.Count);
            return results;
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorSearching(logger, ex, query, ex.StatusCode);
            throw new InvalidOperationException("Failed to search cryptocurrencies from server", ex);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching all cryptocurrency prices from API")]
    private static partial void LogFetchingAllPrices(ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response for crypto prices")]
    private static partial void LogNullResponseForPrices(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {Count} cryptocurrency prices from API")]
    private static partial void LogRetrievedPrices(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error fetching crypto prices: {StatusCode}")]
    private static partial void LogHttpErrorFetchingPrices(ILogger logger, Exception ex, HttpStatusCode? statusCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching price for {Symbol} from API")]
    private static partial void LogFetchingPriceForSymbol(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response for {Symbol}")]
    private static partial void LogNullResponseForSymbol(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved price for {Symbol}: {Price}")]
    private static partial void LogRetrievedPriceForSymbol(ILogger logger, string symbol, decimal price);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cryptocurrency {Symbol} not found")]
    private static partial void LogSymbolNotFound(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error fetching price for {Symbol}: {StatusCode}")]
    private static partial void LogHttpErrorFetchingSymbolPrice(ILogger logger, Exception ex, string symbol, HttpStatusCode? statusCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Fetching price history for {Symbol} over {Period} from API")]
    private static partial void LogFetchingHistory(ILogger logger, string symbol, string period);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response for {Symbol} history")]
    private static partial void LogNullResponseForHistory(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Retrieved {Count} price points for {Symbol} over {Period}")]
    private static partial void LogRetrievedHistory(ILogger logger, int count, string symbol, string period);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Price history for {Symbol} not found")]
    private static partial void LogHistoryNotFound(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error fetching history for {Symbol}: {StatusCode}")]
    private static partial void LogHttpErrorFetchingHistory(ILogger logger, Exception ex, string symbol, HttpStatusCode? statusCode);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Searching cryptocurrencies for '{Query}' from API")]
    private static partial void LogSearchingCrypto(ILogger logger, string query);

    [LoggerMessage(Level = LogLevel.Warning, Message = "API returned null response for search query '{Query}'")]
    private static partial void LogNullResponseForSearch(ILogger logger, string query);

    [LoggerMessage(Level = LogLevel.Information, Message = "Search for '{Query}' returned {Count} results")]
    private static partial void LogSearchResults(ILogger logger, string query, int count);

    [LoggerMessage(Level = LogLevel.Error, Message = "HTTP error searching for '{Query}': {StatusCode}")]
    private static partial void LogHttpErrorSearching(ILogger logger, Exception ex, string query, HttpStatusCode? statusCode);
}
