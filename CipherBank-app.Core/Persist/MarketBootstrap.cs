// <copyright file="MarketBootstrap.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Services;

namespace CipherBank_app.Persist;

/// <summary>Hydrates and refreshes the local USD-rate snapshot.</summary>
public static class MarketBootstrap
{
    /// <summary>Maximum age of a cached rate before it is refreshed.</summary>
    public static readonly TimeSpan MaxRateAge = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Gets cached rates for the requested symbols and refreshes the entire set when
    /// the cache is incomplete or any row is stale, using <see cref="TimeProvider.System"/>.
    /// Use: High (home rates). Scope: MarketBootstrap consumers.
    /// </summary>
    public static Task HydrateAndRefreshAsync(
        IRatesCache cache,
        IPublicQuoteService publicQuotes,
        IEnumerable<string> symbols,
        CancellationToken ct)
        => HydrateAndRefreshAsync(cache, publicQuotes, symbols, null, ct);

    /// <summary>
    /// Gets cached rates for the requested symbols and refreshes the entire set when
    /// the cache is incomplete or any row is stale.
    /// Use: High (home rates). Scope: MarketBootstrap consumers.
    /// </summary>
    public static async Task HydrateAndRefreshAsync(
        IRatesCache cache,
        IPublicQuoteService publicQuotes,
        IEnumerable<string> symbols,
        TimeProvider? timeProvider,
        CancellationToken ct)
    {
        TimeProvider clock = timeProvider ?? TimeProvider.System;
        DateTimeOffset now = clock.GetUtcNow();

        string[] requestedSymbols = symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (requestedSymbols.Length == 0)
        {
            return;
        }

        IReadOnlyList<RateRow> cachedRows = await cache.GetAsync(requestedSymbols, ct).ConfigureAwait(false);
        if (cachedRows.Count == requestedSymbols.Length
            && cachedRows.All(row => IsFresh(row, now)))
        {
            return;
        }

        long nowMs = now.ToUnixTimeMilliseconds();
        List<RateRow> refreshedRows = new List<RateRow>(requestedSymbols.Length);
        foreach (string? symbol in requestedSymbols)
        {
            PublicQuote quote = await publicQuotes
                .GetInverseQuoteAsync(symbol, 1m, "USD", ct)
                .ConfigureAwait(false);
            refreshedRows.Add(RateRow.FromQuote(quote, nowMs));
        }

        await cache.UpsertAsync(refreshedRows, ct).ConfigureAwait(false);
    }

    /// <summary>
    /// True when the stored unix-ms timestamp is not in the future and is within <see cref="MaxRateAge"/>.
    /// Use: High (hydrate). Scope: MarketBootstrap.
    /// </summary>
    private static bool IsFresh(RateRow row, DateTimeOffset now)
    {
        DateTimeOffset updatedAt = DateTimeOffset.FromUnixTimeMilliseconds(row.UpdatedAtMs);
        return updatedAt <= now && now - updatedAt <= MaxRateAge;
    }
}
