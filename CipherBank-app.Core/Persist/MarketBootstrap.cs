// <copyright file="MarketBootstrap.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
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
    /// the cache is incomplete or any row is stale.
    /// </summary>
    public static Task HydrateAndRefreshAsync(
        IRatesCache cache,
        IPublicQuoteService publicQuotes,
        IEnumerable<string> symbols,
        CancellationToken ct)
        => HydrateAndRefreshAsync(cache, publicQuotes, symbols, null, ct);

    public static async Task HydrateAndRefreshAsync(
        IRatesCache cache,
        IPublicQuoteService publicQuotes,
        IEnumerable<string> symbols,
        TimeProvider? timeProvider,
        CancellationToken ct)
    {
        TimeProvider clock = timeProvider ?? TimeProvider.System;

        var requestedSymbols = symbols
            .Where(symbol => !string.IsNullOrWhiteSpace(symbol))
            .Select(symbol => symbol.ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (requestedSymbols.Length == 0)
        {
            return;
        }

        IReadOnlyList<RateRow> cachedRows = await cache.GetAsync(requestedSymbols, ct).ConfigureAwait(false);
        var nowMs = clock.GetUtcNow().ToUnixTimeMilliseconds();
        if (cachedRows.Count == requestedSymbols.Length
            && cachedRows.All(row => nowMs - row.UpdatedAtMs <= MaxRateAge.TotalMilliseconds))
        {
            return;
        }

        var refreshedRows = new List<RateRow>(requestedSymbols.Length);
        foreach (var symbol in requestedSymbols)
        {
            PublicQuote quote = await publicQuotes
                .GetInverseQuoteAsync(symbol, 1m, "USD", ct)
                .ConfigureAwait(false);
            refreshedRows.Add(ToRateRow(quote, nowMs));
        }

        await cache.UpsertAsync(refreshedRows, ct).ConfigureAwait(false);
    }

    /// <summary>Maps a one-unit inverse quote to its persisted USD rate.</summary>
    public static RateRow ToRateRow(PublicQuote quote, long updatedAtMs)
    {
        ArgumentNullException.ThrowIfNull(quote);
        return new RateRow(
            quote.InputCurrency.ToUpperInvariant(),
            (double)quote.Rate,
            Change24h: 0d,
            updatedAtMs);
    }
}
