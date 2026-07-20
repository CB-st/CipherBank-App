// <copyright file="MockPublicQuoteService.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Services.Mocks;

/// <summary>
/// In-memory stand-in for <see cref="IPublicQuoteService"/> used in DEBUG builds.
/// </summary>
public sealed class MockPublicQuoteService : IPublicQuoteService
{
    private static readonly Dictionary<string, decimal> UsdPrices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["BTC"] = 63204.18m,
        ["XMR"] = 160.00m,
        ["USD"] = 1.00m,
    };

    public Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<IReadOnlyList<string>> GetCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<string> currencies = ["BTC", "XMR", "USD"];
        return Task.FromResult(currencies);
    }

    public Task<PublicQuote> GetInverseQuoteAsync(
        string inputSymbol,
        decimal inputAmount,
        string outputSymbol,
        CancellationToken cancellationToken = default)
    {
        var input = Normalize(inputSymbol);
        var output = Normalize(outputSymbol);
        var inputUsd = ToUsd(input, inputAmount);
        var outputAmount = FromUsd(output, inputUsd);
        return Task.FromResult(new PublicQuote(input, inputAmount, output, outputAmount));
    }

    public Task<PublicQuote> GetQuoteAsync(
        string inputSymbol,
        decimal outputAmount,
        string outputSymbol,
        CancellationToken cancellationToken = default)
    {
        var input = Normalize(inputSymbol);
        var output = Normalize(outputSymbol);
        var outputUsd = ToUsd(output, outputAmount);
        var inputAmount = FromUsd(input, outputUsd);
        return Task.FromResult(new PublicQuote(input, inputAmount, output, outputAmount));
    }

    private static string Normalize(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        var upper = symbol.Trim().ToUpperInvariant();
        return upper switch
        {
            "BITCOIN" => "BTC",
            "MONERO" => "XMR",
            _ => upper,
        };
    }

    private static decimal ToUsd(string symbol, decimal amount)
    {
        if (!UsdPrices.TryGetValue(symbol, out var price))
        {
            throw new ArgumentException($"Unsupported mock currency '{symbol}'.", nameof(symbol));
        }

        return amount * price;
    }

    private static decimal FromUsd(string symbol, decimal usd)
    {
        if (!UsdPrices.TryGetValue(symbol, out var price) || price == 0m)
        {
            throw new ArgumentException($"Unsupported mock currency '{symbol}'.", nameof(symbol));
        }

        return usd / price;
    }
}
