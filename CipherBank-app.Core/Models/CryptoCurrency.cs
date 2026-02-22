namespace CipherBank_app.Models;

/// <summary>
/// Represents a cryptocurrency with its current market data.
/// </summary>
public record CryptoCurrency(
    string Symbol,
    string Name,
    decimal CurrentPrice,
    decimal PriceChange24h,
    decimal PercentChange24h,
    decimal MarketCap,
    decimal Volume24h,
    string IconUrl)
{
    public bool IsPriceUp => PercentChange24h >= 0;
    public string FormattedPrice => CurrentPrice.ToString("C2");
    public string FormattedPercentChange => $"{(PercentChange24h >= 0 ? "+" : "")}{PercentChange24h:F2}%";
}
