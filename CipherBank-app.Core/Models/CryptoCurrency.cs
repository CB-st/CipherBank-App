// <copyright file="CryptoCurrency.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;

namespace CipherBank_app.Models;

/// <summary>
/// Represents a cryptocurrency with its current market data.
/// </summary>
public record CryptoCurrency(
    AssetSymbol Symbol,
    string Name,
    decimal CurrentPrice,
    decimal PriceChange24H,
    decimal PercentChange24H,
    decimal MarketCap,
    decimal Volume24H,
    string IconUrl)
{
    public bool IsPriceUp => PercentChange24H >= 0;

    public string FormattedPrice => $"${CurrentPrice.ToString("N2", CultureInfo.InvariantCulture)}";

    public string FormattedPercentChange =>
        $"{(PercentChange24H >= 0 ? "+" : string.Empty)}{PercentChange24H.ToString("F2", CultureInfo.InvariantCulture)}%";
}
