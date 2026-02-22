using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CipherBank_app.Models;
using Microsoft.Extensions.Logging;

namespace CipherBank_app.Services.Mocks;

/// <summary>
/// Mock implementation of IWalletService for development and testing.
/// Maintains an in-memory collection of wallets with simulated operations.
/// </summary>
public class MockWalletService : IWalletService
{
    private readonly ILogger<MockWalletService> _logger;
    private readonly Random _random = new();
    private readonly List<Wallet> _wallets;

    // Simulated latency range in milliseconds
    private const int MinLatencyMs = 100;
    private const int MaxLatencyMs = 400;

    public MockWalletService(ILogger<MockWalletService> logger)
    {
        _logger = logger;

        // Initialize with some mock wallets
        _wallets =
        [
            new Wallet(
                GenerateWalletId(),
                "BTC",
                "Bitcoin",
                0.52483921m,
                GenerateBitcoinAddress(),
                DateTimeOffset.UtcNow.AddDays(-45)),

            new Wallet(
                GenerateWalletId(),
                "ETH",
                "Ethereum",
                3.84729184m,
                GenerateEthereumAddress(),
                DateTimeOffset.UtcNow.AddDays(-30)),

            new Wallet(
                GenerateWalletId(),
                "SOL",
                "Solana",
                125.50000000m,
                GenerateSolanaAddress(),
                DateTimeOffset.UtcNow.AddDays(-15)),

            new Wallet(
                GenerateWalletId(),
                "DOGE",
                "Dogecoin",
                10500.00000000m,
                GenerateDogecoinAddress(),
                DateTimeOffset.UtcNow.AddDays(-7))
        ];

        _logger.LogInformation("MockWalletService initialized with {Count} wallets", _wallets.Count);
    }

    public async Task<List<Wallet>> GetWalletsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all wallets (mock)");
        await SimulateNetworkDelayAsync(cancellationToken);

        _logger.LogInformation("Returned {Count} wallets", _wallets.Count);
        return _wallets.ToList();
    }

    public async Task<Wallet> GetWalletAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        _logger.LogDebug("Getting wallet {WalletId} (mock)", id);
        await SimulateNetworkDelayAsync(cancellationToken);

        var wallet = _wallets.FirstOrDefault(w => w.Id == id);
        if (wallet == null)
        {
            _logger.LogWarning("Wallet {WalletId} not found", id);
            throw new KeyNotFoundException($"Wallet with ID '{id}' not found");
        }

        _logger.LogInformation("Returned wallet {WalletId} with balance {Balance} {Symbol}",
            wallet.Id, wallet.Balance, wallet.CryptoSymbol);
        return wallet;
    }

    public async Task<decimal> GetWalletBalanceAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        _logger.LogDebug("Getting balance for wallet {WalletId} (mock)", id);
        var wallet = await GetWalletAsync(id, cancellationToken);

        _logger.LogInformation("Wallet {WalletId} balance: {Balance} {Symbol}",
            id, wallet.Balance, wallet.CryptoSymbol);
        return wallet.Balance;
    }

    public async Task<Wallet> CreateWalletAsync(string cryptoSymbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cryptoSymbol);

        _logger.LogDebug("Creating wallet for {Symbol} (mock)", cryptoSymbol);
        await SimulateNetworkDelayAsync(cancellationToken);

        var normalizedSymbol = cryptoSymbol.ToUpperInvariant();

        // Check if wallet already exists
        if (_wallets.Any(w => w.CryptoSymbol.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Wallet for {Symbol} already exists", normalizedSymbol);
            throw new InvalidOperationException($"Wallet for {normalizedSymbol} already exists");
        }

        var cryptoName = GetCryptoName(normalizedSymbol);
        var address = GenerateAddress(normalizedSymbol);

        var wallet = new Wallet(
            GenerateWalletId(),
            normalizedSymbol,
            cryptoName,
            0m,
            address,
            DateTimeOffset.UtcNow);

        _wallets.Add(wallet);

        _logger.LogInformation("Created new wallet {WalletId} for {Symbol} at address {Address}",
            wallet.Id, wallet.CryptoSymbol, wallet.Address);
        return wallet;
    }

    /// <summary>
    /// Internal method to update wallet balance after transactions.
    /// </summary>
    internal void UpdateWalletBalance(string walletId, decimal newBalance)
    {
        var index = _wallets.FindIndex(w => w.Id == walletId);
        if (index >= 0)
        {
            _wallets[index] = _wallets[index] with { Balance = newBalance };
            _logger.LogDebug("Updated wallet {WalletId} balance to {Balance}", walletId, newBalance);
        }
    }

    /// <summary>
    /// Internal method to get a wallet by symbol for transaction processing.
    /// </summary>
    internal Wallet? GetWalletBySymbol(string symbol)
    {
        return _wallets.FirstOrDefault(w =>
            w.CryptoSymbol.Equals(symbol, StringComparison.OrdinalIgnoreCase));
    }

    private string GenerateWalletId() => Guid.NewGuid().ToString("N")[..16];

    private string GenerateBitcoinAddress()
    {
        const string chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        var address = "bc1q" + new string(Enumerable.Range(0, 39)
            .Select(_ => chars[_random.Next(chars.Length)]).ToArray());
        return address;
    }

    private string GenerateEthereumAddress()
    {
        const string hexChars = "0123456789abcdef";
        return "0x" + new string(Enumerable.Range(0, 40)
            .Select(_ => hexChars[_random.Next(hexChars.Length)]).ToArray());
    }

    private string GenerateSolanaAddress()
    {
        const string chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        return new string(Enumerable.Range(0, 44)
            .Select(_ => chars[_random.Next(chars.Length)]).ToArray());
    }

    private string GenerateDogecoinAddress()
    {
        const string chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        return "D" + new string(Enumerable.Range(0, 33)
            .Select(_ => chars[_random.Next(chars.Length)]).ToArray());
    }

    private string GenerateAddress(string symbol) => symbol.ToUpperInvariant() switch
    {
        "BTC" => GenerateBitcoinAddress(),
        "ETH" => GenerateEthereumAddress(),
        "SOL" => GenerateSolanaAddress(),
        "DOGE" => GenerateDogecoinAddress(),
        _ => GenerateEthereumAddress() // Default to ETH-style address
    };

    private static string GetCryptoName(string symbol) => symbol.ToUpperInvariant() switch
    {
        "BTC" => "Bitcoin",
        "ETH" => "Ethereum",
        "BNB" => "BNB",
        "SOL" => "Solana",
        "XRP" => "XRP",
        "ADA" => "Cardano",
        "AVAX" => "Avalanche",
        "DOGE" => "Dogecoin",
        "DOT" => "Polkadot",
        "LINK" => "Chainlink",
        _ => symbol
    };

    private async Task SimulateNetworkDelayAsync(CancellationToken cancellationToken)
    {
        var delay = _random.Next(MinLatencyMs, MaxLatencyMs);
        await Task.Delay(delay, cancellationToken);
    }
}
