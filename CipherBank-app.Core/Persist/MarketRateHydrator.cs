// <copyright file="MarketRateHydrator.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Services;

namespace CipherBank_app.Persist;

/// <summary>Hydrates and refreshes the local USD-rate snapshot.</summary>
public sealed class MarketRateHydrator
{
    /// <summary>Maximum age of a cached rate before it is refreshed.</summary>
    public static readonly TimeSpan MaxRateAge = TimeSpan.FromMinutes(15);

    private readonly IRateSnapshotStore _cache;
    private readonly IPublicQuoteService _publicQuotes;
    private readonly TimeProvider _timeProvider;

    public MarketRateHydrator(
        IRateSnapshotStore cache,
        IPublicQuoteService publicQuotes,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(publicQuotes);
        ArgumentNullException.ThrowIfNull(timeProvider);
        _cache = cache;
        _publicQuotes = publicQuotes;
        _timeProvider = timeProvider;
    }

    /// <summary>
    /// Reuses a complete fresh snapshot or refreshes all requested symbols.
    /// Use: High (home rates). Scope: process-wide market data.
    /// </summary>
    public Task HydrateAndRefreshAsync(
        IEnumerable<AssetSymbol> symbols,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(symbols);
        return HydrateAndRefreshCoreAsync(symbols, ct);
    }

    private static bool IsFresh(RateRow row, DateTimeOffset now)
    {
        DateTimeOffset updatedAt = DateTimeOffset.FromUnixTimeMilliseconds(row.UpdatedAtMs);
        return updatedAt <= now && now - updatedAt <= MaxRateAge;
    }

    /// <summary>
    /// Performs cache freshness evaluation and remote hydration after synchronous validation.
    /// Use: High (home rates). Scope: process-wide market data.
    /// </summary>
    private async Task HydrateAndRefreshCoreAsync(
        IEnumerable<AssetSymbol> symbols,
        CancellationToken ct)
    {
        DateTimeOffset now = _timeProvider.GetUtcNow();
        AssetSymbol[] requestedSymbols = symbols
            .Where(symbol => symbol is not null)
            .Distinct()
            .ToArray();
        if (requestedSymbols.Length == 0)
        {
            return;
        }

        IReadOnlyList<RateRow> cachedRows = await _cache
            .GetAsync(requestedSymbols, ct)
            .ConfigureAwait(false);
        if (cachedRows.Count == requestedSymbols.Length
            && cachedRows.All(row => IsFresh(row, now)))
        {
            return;
        }

        long nowMs = now.ToUnixTimeMilliseconds();
        List<RateRow> refreshedRows = new(requestedSymbols.Length);
        AssetSymbol usd = new("USD");
        foreach (AssetSymbol symbol in requestedSymbols)
        {
            PublicQuote quote = await _publicQuotes
                .GetInverseQuoteAsync(symbol, 1m, usd, ct)
                .ConfigureAwait(false);
            refreshedRows.Add(RateRow.FromQuote(quote, nowMs));
        }

        await _cache.UpsertAsync(refreshedRows, ct).ConfigureAwait(false);
    }
}
