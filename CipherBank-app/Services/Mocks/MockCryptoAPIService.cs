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
/// Mock implementation of ICryptoAPIService for development and testing.
/// Provides realistic cryptocurrency market data without making actual API calls.
/// </summary>
public class MockCryptoAPIService : ICryptoApiService
{
    private readonly ILogger<MockCryptoAPIService> _logger;
    private readonly Random _random = new();

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
        new("LINK", "Chainlink", 25.30m, 0.95m, 3.90m, 15800000000m, 680000000m, "https://assets.coingecko.com/coins/images/877/large/chainlink-new-logo.png")
    ];

    public MockCryptoAPIService(ILogger<MockCryptoAPIService> logger)
    {
        _logger = logger;
        _logger.LogInformation("MockCryptoAPIService initialized with {Count} cryptocurrencies", MockCryptos.Count);
    }

    public async Task<List<CryptoCurrency>> GetCryptoPricesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all crypto prices (mock)");
        await SimulateNetworkDelayAsync(cancellationToken);

        // Add slight price variations to simulate real-time data
        var cryptos = MockCryptos.Select(c => AddPriceVariation(c)).ToList();

        _logger.LogInformation("Returned {Count} cryptocurrency prices", cryptos.Count);
        return cryptos;
    }

    public async Task<CryptoCurrency> GetCryptoPriceAsync(string symbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);

        _logger.LogDebug("Getting crypto price for {Symbol} (mock)", symbol);
        await SimulateNetworkDelayAsync(cancellationToken);

        var crypto = MockCryptos.FirstOrDefault(c =>
            c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));

        if (crypto == null)
        {
            _logger.LogWarning("Cryptocurrency {Symbol} not found", symbol);
            throw new KeyNotFoundException($"Cryptocurrency with symbol '{symbol}' not found");
        }

        var result = AddPriceVariation(crypto);
        _logger.LogInformation("Returned price for {Symbol}: {Price}", result.Symbol, result.CurrentPrice);
        return result;
    }

    public async Task<PriceHistory> GetPriceHistoryAsync(string symbol, string period, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        ArgumentException.ThrowIfNullOrWhiteSpace(period);

        _logger.LogDebug("Getting price history for {Symbol} over {Period} (mock)", symbol, period);
        await SimulateNetworkDelayAsync(cancellationToken);

        var crypto = MockCryptos.FirstOrDefault(c =>
            c.Symbol.Equals(symbol, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Cryptocurrency with symbol '{symbol}' not found");

        var (points, startDate) = GeneratePriceHistory(crypto.CurrentPrice, period);
        var endDate = DateTimeOffset.UtcNow;

        var history = new PriceHistory(symbol.ToUpperInvariant(), points, startDate, endDate);

        _logger.LogInformation("Generated {Count} price points for {Symbol} over {Period}",
            points.Count, symbol, period);
        return history;
    }

    public async Task<List<CryptoCurrency>> SearchCryptoAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);

        _logger.LogDebug("Searching cryptocurrencies for '{Query}' (mock)", query);
        await SimulateNetworkDelayAsync(cancellationToken);

        var results = MockCryptos
            .Where(c => c.Symbol.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                       c.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(AddPriceVariation)
            .ToList();

        _logger.LogInformation("Search for '{Query}' returned {Count} results", query, results.Count);
        return results;
    }

    private CryptoCurrency AddPriceVariation(CryptoCurrency crypto)
    {
        // Add up to +/- 0.5% price variation to simulate real-time updates
        var variation = (decimal)(_random.NextDouble() * 0.01 - 0.005);
        var newPrice = crypto.CurrentPrice * (1 + variation);
        var newChange = crypto.PriceChange24h + (crypto.CurrentPrice * variation);
        var newPercent = crypto.PercentChange24h + (variation * 100);

        return crypto with
        {
            CurrentPrice = Math.Round(newPrice, crypto.CurrentPrice < 1 ? 6 : 2),
            PriceChange24h = Math.Round(newChange, crypto.CurrentPrice < 1 ? 6 : 2),
            PercentChange24h = Math.Round(newPercent, 2)
        };
    }

    private (List<PricePoint> points, DateTimeOffset startDate) GeneratePriceHistory(decimal basePrice, string period)
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
            _ => (60, 168) // Default to 7 days
        };

        var startDate = now.AddMinutes(-intervalMinutes * totalPoints);
        var currentPrice = basePrice * 0.95m; // Start 5% lower

        for (int i = 0; i < totalPoints; i++)
        {
            var timestamp = startDate.AddMinutes(intervalMinutes * i);
            var variation = (decimal)(_random.NextDouble() * 0.02 - 0.01); // +/- 1%
            currentPrice *= (1 + variation);

            // Trend slightly upward to end near base price
            currentPrice += (basePrice - currentPrice) * 0.01m;

            var volume = (decimal)(_random.NextDouble() * 1000000 + 500000);

            points.Add(new PricePoint(
                timestamp,
                Math.Round(currentPrice, basePrice < 1 ? 6 : 2),
                Math.Round(volume, 2)));
        }

        return (points, startDate);
    }

    private async Task SimulateNetworkDelayAsync(CancellationToken cancellationToken)
    {
        int delay = RandomNumberGenerator.GetInt32(MinLatencyMs, MaxLatencyMs);
        await Task.Delay(delay, cancellationToken);
    }
}
