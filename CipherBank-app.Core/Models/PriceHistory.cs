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
    public decimal HighPrice => PricePoints.Count > 0 ? PricePoints.Max(p => p.Price) : 0;
    public decimal LowPrice => PricePoints.Count > 0 ? PricePoints.Min(p => p.Price) : 0;
    public decimal AveragePrice => PricePoints.Count > 0 ? PricePoints.Average(p => p.Price) : 0;

    public decimal PriceChange => PricePoints.Count >= 2
        ? PricePoints.Last().Price - PricePoints.First().Price
        : 0;

    public decimal PercentChange => PricePoints.Count >= 2 && PricePoints.First().Price != 0
        ? (PriceChange / PricePoints.First().Price) * 100
        : 0;
}
