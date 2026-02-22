using System;

namespace CipherBank_app.Models;

/// <summary>
/// Represents a cryptocurrency with its current market data.
/// </summary>
/// <param name="Symbol">The trading symbol (e.g., BTC, ETH).</param>
/// <param name="Name">The full name of the cryptocurrency.</param>
/// <param name="CurrentPrice">The current price in USD.</param>
/// <param name="PriceChange24h">The absolute price change in the last 24 hours.</param>
/// <param name="PercentChange24h">The percentage price change in the last 24 hours.</param>
/// <param name="MarketCap">The total market capitalization in USD.</param>
/// <param name="Volume24h">The 24-hour trading volume in USD.</param>
/// <param name="IconUrl">URL to the cryptocurrency's icon/logo.</param>
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
    /// <summary>
    /// Gets whether the price change is positive.
    /// </summary>
    public bool IsPriceUp => PercentChange24h >= 0;

    /// <summary>
    /// Gets the formatted price string.
    /// </summary>
    public string FormattedPrice => CurrentPrice.ToString("C2");

    /// <summary>
    /// Gets the formatted percentage change string.
    /// </summary>
    public string FormattedPercentChange => $"{(PercentChange24h >= 0 ? "+" : "")}{PercentChange24h:F2}%";
}
