// <copyright file="RateRow.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Persist.Entities;

namespace CipherBank_app.Persist;

/// <summary>A cached market rate. <see cref="Symbol"/> normalizes to uppercase at construction.</summary>
public sealed record RateRow(AssetSymbol Symbol, decimal Usd, decimal Change24H, long UpdatedAtMs)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RateRow"/> class from a persisted snapshot
    /// entity. Use: High (every rates read). Scope: SqliteRateSnapshotStore projections.
    /// </summary>
    public RateRow(RateSnapshotEntity entity)
        : this(AssetSymbol.Parse(entity.Symbol), entity.Usd, entity.Change24H, entity.UpdatedAtMs)
    {
    }

    /// <summary>Maps a one-unit inverse quote to its persisted USD rate.</summary>
    public static RateRow FromQuote(PublicQuote quote, long updatedAtMs)
    {
        ArgumentNullException.ThrowIfNull(quote);
        return new RateRow(
            quote.InputCurrency,
            quote.Rate,
            Change24H: 0m,
            updatedAtMs);
    }
}
