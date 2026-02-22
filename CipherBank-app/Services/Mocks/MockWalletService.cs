// <copyright file="MockWalletService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

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
/// Mock implementation of IWalletService for development and testing.
/// Maintains an in-memory collection of wallets with simulated operations.
/// </summary>
public sealed partial class MockWalletService : IWalletService
{
    // Simulated latency range in milliseconds
    private const int MinLatencyMs = 100;
    private const int MaxLatencyMs = 400;

    private readonly ILogger<MockWalletService> _logger;
    private readonly List<Wallet> _wallets;

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
                DateTimeOffset.UtcNow.AddDays(-7)),
        ];

        LogInitialized(_logger, _wallets.Count);
    }

    public async Task<List<Wallet>> GetWalletsAsync(CancellationToken cancellationToken = default)
    {
        LogGettingAllWallets(_logger);
        await SimulateNetworkDelayAsync(cancellationToken);

        LogReturnedWallets(_logger, _wallets.Count);
        return _wallets.ToList();
    }

    public async Task<Wallet> GetWalletAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        LogGettingWallet(_logger, id);
        await SimulateNetworkDelayAsync(cancellationToken);

        var wallet = _wallets.FirstOrDefault(w => w.Id == id);
        if (wallet == null)
        {
            LogWalletNotFound(_logger, id);
            throw new KeyNotFoundException($"Wallet with ID '{id}' not found");
        }

        LogReturnedWallet(_logger, wallet.Id, wallet.Balance, wallet.CryptoSymbol);
        return wallet;
    }

    public async Task<decimal> GetWalletBalanceAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        LogGettingBalance(_logger, id);
        var wallet = await GetWalletAsync(id, cancellationToken);

        LogWalletBalance(_logger, id, wallet.Balance, wallet.CryptoSymbol);
        return wallet.Balance;
    }

    public async Task<Wallet> CreateWalletAsync(string cryptoSymbol, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cryptoSymbol);

        LogCreatingWallet(_logger, cryptoSymbol);
        await SimulateNetworkDelayAsync(cancellationToken);

        var normalizedSymbol = cryptoSymbol.ToUpperInvariant();

        // Check if wallet already exists
        if (_wallets.Any(w => w.CryptoSymbol.Equals(normalizedSymbol, StringComparison.OrdinalIgnoreCase)))
        {
            LogWalletAlreadyExists(_logger, normalizedSymbol);
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

        LogWalletCreated(_logger, wallet.Id, wallet.CryptoSymbol, wallet.Address);
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
            LogUpdatedBalance(_logger, walletId, newBalance);
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

    private static string GenerateWalletId() => Guid.NewGuid().ToString("N")[..16];

    private static string GenerateBitcoinAddress()
    {
        const string chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        var address = "bc1q" + new string(Enumerable.Range(0, 39)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
        return address;
    }

    private static string GenerateEthereumAddress()
    {
        const string hexChars = "0123456789abcdef";
        return "0x" + new string(Enumerable.Range(0, 40)
            .Select(_ => hexChars[RandomNumberGenerator.GetInt32(hexChars.Length)]).ToArray());
    }

    private static string GenerateSolanaAddress()
    {
        const string chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        return new string(Enumerable.Range(0, 44)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
    }

    private static string GenerateDogecoinAddress()
    {
        const string chars = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        return "D" + new string(Enumerable.Range(0, 33)
            .Select(_ => chars[RandomNumberGenerator.GetInt32(chars.Length)]).ToArray());
    }

    private static string GenerateAddress(string symbol) => symbol.ToUpperInvariant() switch
    {
        "BTC" => GenerateBitcoinAddress(),
        "ETH" => GenerateEthereumAddress(),
        "SOL" => GenerateSolanaAddress(),
        "DOGE" => GenerateDogecoinAddress(),
        _ => GenerateEthereumAddress(), // Default to ETH-style address
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
        _ => symbol,
    };

    private static async Task SimulateNetworkDelayAsync(CancellationToken cancellationToken)
    {
        var delay = RandomNumberGenerator.GetInt32(MinLatencyMs, MaxLatencyMs);
        await Task.Delay(delay, cancellationToken);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "MockWalletService initialized with {Count} wallets")]
    private static partial void LogInitialized(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting all wallets (mock)")]
    private static partial void LogGettingAllWallets(ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Returned {Count} wallets")]
    private static partial void LogReturnedWallets(ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting wallet {WalletId} (mock)")]
    private static partial void LogGettingWallet(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Wallet {WalletId} not found")]
    private static partial void LogWalletNotFound(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Returned wallet {WalletId} with balance {Balance} {Symbol}")]
    private static partial void LogReturnedWallet(ILogger logger, string walletId, decimal balance, string symbol);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Getting balance for wallet {WalletId} (mock)")]
    private static partial void LogGettingBalance(ILogger logger, string walletId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Wallet {WalletId} balance: {Balance} {Symbol}")]
    private static partial void LogWalletBalance(ILogger logger, string walletId, decimal balance, string symbol);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Creating wallet for {Symbol} (mock)")]
    private static partial void LogCreatingWallet(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Wallet for {Symbol} already exists")]
    private static partial void LogWalletAlreadyExists(ILogger logger, string symbol);

    [LoggerMessage(Level = LogLevel.Information, Message = "Created new wallet {WalletId} for {Symbol} at address {Address}")]
    private static partial void LogWalletCreated(ILogger logger, string walletId, string symbol, string address);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Updated wallet {WalletId} balance to {Balance}")]
    private static partial void LogUpdatedBalance(ILogger logger, string walletId, decimal balance);
}
