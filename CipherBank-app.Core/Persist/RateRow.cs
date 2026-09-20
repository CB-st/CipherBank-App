// <copyright file="RateRow.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Persist;

/// <summary>A cached market rate. <see cref="Symbol"/> normalizes to uppercase at construction.</summary>
public sealed record RateRow(string Symbol, decimal Usd, decimal Change24h, long UpdatedAtMs)
{
    private readonly string _symbol = NormalizeSymbol(Symbol);

    /// <summary>
    /// Initializes a new instance of the <see cref="RateRow"/> class from a persisted snapshot
    /// entity. Use: High (every rates read). Scope: RatesCache projections.
    /// </summary>
    public RateRow(Persist.Entities.RateSnapshotEntity entity)
        : this(entity.Symbol, entity.Usd, entity.Change24H, entity.UpdatedAtMs)
    {
    }

    /// <summary>Gets the asset symbol, always uppercase invariant.</summary>
    public string Symbol
    {
        get => _symbol;
        init => _symbol = NormalizeSymbol(value);
    }

    /// <summary>Maps a one-unit inverse quote to its persisted USD rate.</summary>
    public static RateRow FromQuote(PublicQuote quote, long updatedAtMs)
    {
        ArgumentNullException.ThrowIfNull(quote);
        return new RateRow(
            quote.InputCurrency,
            quote.Rate,
            Change24h: 0m,
            updatedAtMs);
    }

    /// <summary>
    /// Trims and uppercases a nonblank market symbol.
    /// Use: High (market cache/query boundary). Scope: Core market persistence.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="symbol"/> is blank.</exception>
    internal static string NormalizeSymbol(string symbol)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        return symbol.Trim().ToUpperInvariant();
    }
}
