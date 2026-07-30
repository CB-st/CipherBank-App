// <copyright file="PriceHistory.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Models;

/// <summary>
/// Represents historical price data for a cryptocurrency.
/// </summary>
public record PriceHistory(
    string Symbol,
    List<PricePoint> PricePoints,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate)
{
    private const decimal PercentScale = 100m;

    public decimal HighPrice => PricePoints.Count > 0 ? PricePoints.Max(p => p.Price) : 0;

    public decimal LowPrice => PricePoints.Count > 0 ? PricePoints.Min(p => p.Price) : 0;

    public decimal AveragePrice => PricePoints.Count > 0 ? PricePoints.Average(p => p.Price) : 0;

    public decimal PriceChange => PricePoints.Count >= 2
        ? PricePoints[^1].Price - PricePoints[0].Price
        : 0;

    public decimal PercentChange => PricePoints.Count >= 2 && PricePoints[0].Price != 0
        ? (PriceChange / PricePoints[0].Price) * PercentScale
        : 0;
}
