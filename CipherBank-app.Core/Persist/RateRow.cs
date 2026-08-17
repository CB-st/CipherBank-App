// <copyright file="RateRow.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;

namespace CipherBank_app.Persist;

/// <summary>A cached market rate.</summary>
public sealed record RateRow(string Symbol, decimal Usd, decimal Change24h, long UpdatedAtMs)
{
    /// <summary>Maps a one-unit inverse quote to its persisted USD rate.</summary>
    public static RateRow FromQuote(PublicQuote quote, long updatedAtMs)
    {
        ArgumentNullException.ThrowIfNull(quote);
        return new RateRow(
            quote.InputCurrency.ToUpperInvariant(),
            quote.Rate,
            Change24h: 0m,
            updatedAtMs);
    }
}
