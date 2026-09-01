// <copyright file="CryptoCurrency.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Globalization;

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
    Uri? IconUrl)
{
    public bool IsPriceUp => PercentChange24h >= 0;

    public string FormattedPrice => $"${CurrentPrice.ToString("N2", CultureInfo.InvariantCulture)}";

    public string FormattedPercentChange =>
        $"{(PercentChange24h >= 0 ? "+" : string.Empty)}{PercentChange24h.ToString("F2", CultureInfo.InvariantCulture)}%";
}
