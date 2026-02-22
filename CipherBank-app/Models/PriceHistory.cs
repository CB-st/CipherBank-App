using System;
using System.Collections.Generic;
using System.Linq;

namespace CipherBank_app.Models;

/// <summary>
/// Represents historical price data for a cryptocurrency.
/// </summary>
/// <param name="Symbol">The cryptocurrency symbol.</param>
/// <param name="PricePoints">The list of price points in the history.</param>
/// <param name="StartDate">The start of the price history period.</param>
/// <param name="EndDate">The end of the price history period.</param>
public record PriceHistory(
    string Symbol,
    List<PricePoint> PricePoints,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate)
{
    /// <summary>
    /// Gets the highest price in the period.
    /// </summary>
    public decimal HighPrice => PricePoints.Count > 0 ? PricePoints.Max(p => p.Price) : 0;

    /// <summary>
    /// Gets the lowest price in the period.
    /// </summary>
    public decimal LowPrice => PricePoints.Count > 0 ? PricePoints.Min(p => p.Price) : 0;

    /// <summary>
    /// Gets the average price in the period.
    /// </summary>
    public decimal AveragePrice => PricePoints.Count > 0 ? PricePoints.Average(p => p.Price) : 0;

    /// <summary>
    /// Gets the price change over the period.
    /// </summary>
    public decimal PriceChange => PricePoints.Count >= 2
        ? PricePoints.Last().Price - PricePoints.First().Price
        : 0;

    /// <summary>
    /// Gets the percentage change over the period.
    /// </summary>
    public decimal PercentChange => PricePoints.Count >= 2 && PricePoints.First().Price != 0
        ? (PriceChange / PricePoints.First().Price) * 100
        : 0;
}
