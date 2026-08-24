// <copyright file="MockCryptoAPIService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using CipherBank_app.Models;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services.Mocks;

/// <summary>
/// Mock implementation of ICryptoAPIService for development and testing.
/// Provides realistic cryptocurrency market data without making actual API calls.
/// </summary>
public sealed partial class MockCryptoAPIService : ICryptoApiService
{
    // Simulated latency range in milliseconds
    private const int MinLatencyMs = 100;
    private const int MaxLatencyMs = 500;

    private static readonly List<CryptoCurrency> MockCryptos =
    [
        new("BTC", "Bitcoin", 97500.00m, 1250.50m, 1.30m, 1920000000000m, 45000000000m, "https://assets.coingecko.com/coins/images/1/large/bitcoin.png"),
        new("ETH", "Ethereum", 3450.00m, -45.25m, -1.29m, 415000000000m, 18000000000m, "https://assets.coingecko.com/coins/images/279/large/ethereum.png"),
        new("BNB", "BNB", 685.00m, 12.30m, 1.83m, 102000000000m, 2100000000m, "https://assets.coingecko.com/coins/images/825/large/bnb-icon2_2x.png"),
        new("SOL", "Solana", 195.00m, 8.45m, 4.53m, 92000000000m, 5500000000m, "https://assets.coingecko.com/coins/images/4128/large/solana.png"),
        new("XRP", "XRP", 2.85m, 0.15m, 5.56m, 162000000000m, 8900000000m, "https://assets.coingecko.com/coins/images/44/large/xrp-symbol-white-128.png"),
        new("ADA", "Cardano", 1.05m, -0.03m, -2.78m, 37000000000m, 1200000000m, "https://assets.coingecko.com/coins/images/975/large/cardano.png"),
        new("AVAX", "Avalanche", 42.50m, 1.80m, 4.42m, 17500000000m, 890000000m, "https://assets.coingecko.com/coins/images/12559/large/Avalanche_Circle_RedWhite_Trans.png"),
        new("DOGE", "Dogecoin", 0.385m, 0.025m, 6.94m, 57000000000m, 4200000000m, "https://assets.coingecko.com/coins/images/5/large/dogecoin.png"),
        new("DOT", "Polkadot", 8.75m, -0.22m, -2.45m, 11500000000m, 420000000m, "https://assets.coingecko.com/coins/images/12171/large/polkadot.png"),
        new("LINK", "Chainlink", 25.30m, 0.95m, 3.90m, 15800000000m, 680000000m, "https://assets.coingecko.com/coins/images/877/large/chainlink-new-logo.png"),
    ];

    private readonly ILogger<MockCryptoAPIService> _logger;

    public MockCryptoAPIService(ILogger<MockCryptoAPIService> logger)
    {
        _logger = logger;
        LogInitialized(_logger, MockCryptos.Count);
    }

    public async Task<List<CryptoCurrency>> GetCryptoPricesAsync(CancellationToken cancellationToken = default)
    {
        LogGettingAllPrices(_logger);
        await SimulateNetworkDelayAsync(cancellationToken);

        // Add slight price variations to simulate real-time data
        var cryptos = MockCryptos.Select(c => AddPriceVariation(c)).ToList();

        LogReturnedPrices(_logger, cryptos.Count);
        return cryptos;
    }

    public async Task<CryptoCurrency> GetCryptoPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        LogGettingPriceForSymbol(_logger, symbol);
        await SimulateNetworkDelayAsync(cancellationToken);

        var crypto = MockCryptos.FirstOrDefault(c =>
            c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

        if (crypto == null)
        {
            LogSymbolNotFound(_logger, symbol);
            throw new KeyNotFoundException($"Cryptocurrency with symbol '{symbol}' not found");
        }

        var result = AddPriceVariation(crypto);
        LogReturnedPriceForSymbol(_logger, result.Symbol, result.CurrentPrice);
        return result;
    }

    public async Task<PriceHistory> GetPriceHistoryAsync(string symbol, string period, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        LogGettingPriceHistory(_logger, symbol, period);
        await SimulateNetworkDelayAsync(cancellationToken);

        var crypto = MockCryptos.FirstOrDefault(c =>
            c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Cryptocurrency with symbol '{symbol}' not found");

        var (points, startDate) = GeneratePriceHistory(crypto.CurrentPrice, period);
        var endDate = DateTimeOffset.UtcNow;

        var history = new PriceHistory(symbol.ToUpperInvariant(), points, startDate, endDate);

        LogGeneratedPriceHistory(_logger, points.Count, symbol, period);
        return history;
    }

    public async Task<List<CryptoCurrency>> SearchCryptoAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        LogSearchingCrypto(_logger, query);
        await SimulateNetworkDelayAsync(cancellationToken);

        var results = MockCryptos
            .Where(c => c.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       c.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(AddPriceVariation)
            .ToList();

        LogSearchResults(_logger, query, results.Count);
        return results;
    }

    private static CryptoCurrency AddPriceVariation(CryptoCurrency crypto)
    {
        // Add up to +/- 0.5% price variation to simulate real-time updates
        var variation = (decimal)((RandomNumberGenerator.GetInt32(0, 10000) / 10000.0 * 0.01) - 0.005);
        var newPrice = crypto.CurrentPrice * (1 + variation);
        var newChange = crypto.PriceChange24h + (crypto.CurrentPrice * variation);
        var newPercent = crypto.PercentChange24h + (variation * 100);

        return crypto with
        {
            CurrentPrice = Math.Round(newPrice, crypto.CurrentPrice < 1 ? 6 : 2),
            PriceChange24h = Math.Round(newChange, crypto.CurrentPrice < 1 ? 6 : 2),
            PercentChange24h = Math.Round(newPercent, 2),
        };
    }

    private static (List<PricePoint> Points, DateTimeOffset StartDate) GeneratePriceHistory(decimal basePrice, string period)
    {
        var now = DateTimeOffset.UtcNow;
        var points = new List<PricePoint>();

        var (intervalMinutes, totalPoints) = period.ToLowerInvariant() switch
        {
            "1h" => (1, 60),
            "1d" => (15, 96),
            "7d" => (60, 168),
            "30d" => (240, 180),
            "1y" => (1440, 365),
            _ => (60, 168), // Default to 7 days
        };

        var startDate = now.AddMinutes(-intervalMinutes * totalPoints);
        var currentPrice = basePrice * 0.95m; // Start 5% lower

        for (int i = 0; i < totalPoints; i++)
        {
            var timestamp = startDate.AddMinutes(intervalMinutes * i);
            var variation = (decimal)((RandomNumberGenerator.GetInt32(0, 10000) / 10000.0 * 0.02) - 0.01); // +/- 1%
            currentPrice *= 1 + variation;

            // Trend slightly upward to end near base price
            currentPrice += (basePrice - currentPrice) * 0.01m;

            var volume = (decimal)((RandomNumberGenerator.GetInt32(0, 10000) / 10000.0 * 1000000) + 500000);

            points.Add(new PricePoint(
                timestamp,
                Math.Round(currentPrice, basePrice < 1 ? 6 : 2),
                Math.Round(volume, 2)));
        }

        return (points, startDate);
    }

    private static async Task SimulateNetworkDelayAsync(CancellationToken cancellationToken)
    {
        int delay = RandomNumberGenerator.GetInt32(MinLatencyMs, MaxLatencyMs);
        await Task.Delay(delay, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "MockCryptoAPIService initialized with {Count} cryptocurrencies")]
    private static partial void LogInitialized(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting all crypto prices (mock)")]
    private static partial void LogGettingAllPrices(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Returned {Count} cryptocurrency prices")]
    private static partial void LogReturnedPrices(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting crypto price for {Symbol} (mock)")]
    private static partial void LogGettingPriceForSymbol(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Cryptocurrency {Symbol} not found")]
    private static partial void LogSymbolNotFound(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Returned price for {Symbol}: {Price}")]
    private static partial void LogReturnedPriceForSymbol(ILogger logger, string symbol, decimal price);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting price history for {Symbol} over {Period} (mock)")]
    private static partial void LogGettingPriceHistory(ILogger logger, string symbol, string period);

    [LoggerMessage(Level = LogLevel.Information, Message = "Generated {Count} price points for {Symbol} over {Period}")]
    private static partial void LogGeneratedPriceHistory(ILogger logger, int count, string symbol, string period);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Searching cryptocurrencies for '{Query}' (mock)")]
    private static partial void LogSearchingCrypto(ILogger logger, string query);

    [LoggerMessage(Level = LogLevel.Information, Message = "Search for '{Query}' returned {Count} results")]
    private static partial void LogSearchResults(ILogger logger, string query, int count);
}
