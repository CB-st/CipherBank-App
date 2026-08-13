// <copyright file="CryptoAPIService.cs" company="CipherBank">
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
/// Production implementation of ICryptoAPIService using HTTP client.
/// Retrieves cryptocurrency market data from the CipherBank API.
/// </summary>
public sealed partial class CryptoAPIService : ICryptoApiService
{
    private const string PricesEndpoint = "api/v1/crypto/prices";
    private const string PriceEndpoint = "api/v1/crypto/price";
    private const string HistoryEndpoint = "api/v1/crypto/history";
    private const string SearchEndpoint = "api/v1/crypto/search";

    private readonly ILogger<CryptoAPIService> _logger;
    private readonly HttpClient _http;

    public CryptoAPIService(ILogger<CryptoAPIService> logger, HttpClient http)
    {
        _logger = logger;
        _http = http;
    }

    public async Task<List<CryptoCurrency>> GetCryptoPricesAsync(CancellationToken cancellationToken = default)
    {
        LogFetchingAllPrices(_logger);

        try
        {
            var response = await _http.GetAsync(PricesEndpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var cryptos = await response.Content.ReadFromJsonAsync<List<CryptoCurrency>>(cancellationToken: cancellationToken);

            if (cryptos == null)
            {
                LogNullResponseForPrices(_logger);
                return [];
            }

            LogRetrievedPrices(_logger, cryptos.Count);
            return cryptos;
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorFetchingPrices(_logger, ex, ex.StatusCode);
            throw new InvalidOperationException("Failed to retrieve cryptocurrency prices from server", ex);
        }
    }

    public async Task<CryptoCurrency> GetCryptoPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        LogFetchingPriceForSymbol(_logger, symbol);

        try
        {
            var endpoint = $"{PriceEndpoint}/{Uri.EscapeDataString(symbol.ToUpperInvariant())}";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var crypto = await response.Content.ReadFromJsonAsync<CryptoCurrency>(cancellationToken: cancellationToken);

            if (crypto == null)
            {
                LogNullResponseForSymbol(_logger, symbol);
                throw new KeyNotFoundException($"Cryptocurrency '{symbol}' not found");
            }

            LogRetrievedPriceForSymbol(_logger, symbol, crypto.CurrentPrice);
            return crypto;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            LogSymbolNotFound(_logger, symbol);
            throw new KeyNotFoundException($"Cryptocurrency '{symbol}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorFetchingSymbolPrice(_logger, ex, symbol, ex.StatusCode);
            throw new InvalidOperationException($"Failed to retrieve price for {symbol} from server", ex);
        }
    }

    public async Task<PriceHistory> GetPriceHistoryAsync(string symbol, string period, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        LogFetchingHistory(_logger, symbol, period);

        try
        {
            var endpoint = $"{HistoryEndpoint}/{Uri.EscapeDataString(symbol.ToUpperInvariant())}?period={Uri.EscapeDataString(period)}";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var history = await response.Content.ReadFromJsonAsync<PriceHistory>(cancellationToken: cancellationToken);

            if (history == null)
            {
                LogNullResponseForHistory(_logger, symbol);
                throw new KeyNotFoundException($"Price history for '{symbol}' not found");
            }

            LogRetrievedHistory(_logger, history.PricePoints.Count, symbol, period);
            return history;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            LogHistoryNotFound(_logger, symbol);
            throw new KeyNotFoundException($"Price history for '{symbol}' not found", ex);
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorFetchingHistory(_logger, ex, symbol, ex.StatusCode);
            throw new InvalidOperationException($"Failed to retrieve price history for {symbol} from server", ex);
        }
    }

    public async Task<List<CryptoCurrency>> SearchCryptoAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        LogSearchingCrypto(_logger, query);

        try
        {
            var endpoint = $"{SearchEndpoint}?q={Uri.EscapeDataString(query)}";
            var response = await _http.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            var results = await response.Content.ReadFromJsonAsync<List<CryptoCurrency>>(cancellationToken: cancellationToken);

            if (results == null)
            {
                LogNullResponseForSearch(_logger, query);
                return [];
            }

            LogSearchResults(_logger, query, results.Count);
            return results;
        }
        catch (HttpRequestException ex)
        {
            LogHttpErrorSearching(_logger, ex, query, ex.StatusCode);
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
