// <copyright file="InMemoryPublicQuoteService.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Services;

/// <summary>
/// In-memory stand-in for <see cref="IPublicQuoteService"/> used in DEBUG builds.
/// </summary>
public sealed class InMemoryPublicQuoteService : IPublicQuoteService
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

    public Task<IReadOnlyList<AssetSymbol>> GetCurrenciesAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<AssetSymbol> currencies = ["BTC", "XMR", "USD"];
        return Task.FromResult(currencies);
    }

    public Task<PublicQuote> GetInverseQuoteAsync(
        AssetSymbol inputSymbol,
        decimal inputAmount,
        AssetSymbol outputSymbol,
        CancellationToken cancellationToken = default)
    {
        var input = Normalize(inputSymbol);
        var output = Normalize(outputSymbol);
        var inputUsd = ToUsd(input, inputAmount);
        var outputAmount = FromUsd(output, inputUsd);
        return Task.FromResult(new PublicQuote(input, inputAmount, output, outputAmount));
    }

    public Task<PublicQuote> GetQuoteAsync(
        AssetSymbol inputSymbol,
        decimal outputAmount,
        AssetSymbol outputSymbol,
        CancellationToken cancellationToken = default)
    {
        var input = Normalize(inputSymbol);
        var output = Normalize(outputSymbol);
        var outputUsd = ToUsd(output, outputAmount);
        var inputAmount = FromUsd(input, outputUsd);
        return Task.FromResult(new PublicQuote(input, inputAmount, output, outputAmount));
    }

    /// <summary>Applies provider-specific aliases after asset identity normalization.</summary>
    private static string Normalize(AssetSymbol symbol)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        return CurrencySymbolMap.ToAppSymbol(symbol.Value);
    }

    private static decimal ToUsd(string symbol, decimal amount)
    {
        if (!UsdPrices.TryGetValue(symbol, out var price))
        {
            throw new ArgumentException($"Unsupported in-memory currency '{symbol}'.", nameof(symbol));
        }

        return amount * price;
    }

    private static decimal FromUsd(string symbol, decimal usd)
    {
        if (!UsdPrices.TryGetValue(symbol, out var price) || price == 0m)
        {
            throw new ArgumentException($"Unsupported in-memory currency '{symbol}'.", nameof(symbol));
        }

        return usd / price;
    }
}
