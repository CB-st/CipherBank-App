// <copyright file="IndicativeQuoteMapper.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using CipherBank_app.Models;
using CipherBank_app.V1;

namespace CipherBank_app.Services;

/// <summary>
/// Maps public PriceCache quotes into app <see cref="QuoteDto"/> locks.
/// Expiry is client-synthesized until product <c>/quote/lock</c> exists.
/// </summary>
public static class IndicativeQuoteMapper
{
    /// <summary>Default client-side indicative TTL (matches Expo Convert).</summary>
    public static readonly int DefaultTtlMs = 15_000;

    /// <summary>
    /// Builds an indicative lock DTO from a public quote using <see cref="DefaultTtlMs"/>.
    /// </summary>
    public static QuoteDto ToQuoteDto(PublicQuote quote, long nowMs)
        => ToQuoteDto(quote, nowMs, DefaultTtlMs);

    /// <summary>
    /// Builds an indicative lock DTO from a public quote.
    /// </summary>
    /// <param name="quote">Public /iquote or /quote result.</param>
    /// <param name="nowMs">Unix epoch milliseconds.</param>
    /// <param name="ttlMs">Client TTL.</param>
    /// <returns>Quote DTO usable by Convert countdown UX.</returns>
    public static QuoteDto ToQuoteDto(PublicQuote quote, long nowMs, int ttlMs)
    {
        ArgumentNullException.ThrowIfNull(quote);

        return new QuoteDto
        {
            From = quote.InputCurrency,
            To = quote.OutputCurrency,
            Rate = quote.Rate.ToString(CultureInfo.InvariantCulture),
            ExpiresAt = nowMs + ttlMs,
        };
    }
}
