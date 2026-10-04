// <copyright file="ProductSurfaceMap.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;
using CipherBank_app.Models;

namespace CipherBank_app.V1;

/// <summary>Maps product /v1 DTOs onto the leftover Shell UI models.</summary>
public static class ProductSurfaceMap
{
    public static CryptoCurrency ToCryptoCurrency(HoldingDto holding)
    {
        ArgumentNullException.ThrowIfNull(holding);
        return new CryptoCurrency(
            holding.Symbol,
            string.IsNullOrWhiteSpace(holding.Name) ? holding.Symbol : holding.Name,
            ParseDecimal(holding.UsdValue),
            0m,
            ParseDecimal(holding.Change24HPct),
            0m,
            0m,
            null);
    }

    public static Wallet ToWallet(HoldingDto holding)
    {
        ArgumentNullException.ThrowIfNull(holding);
        return new Wallet(
            holding.Symbol,
            holding.Symbol,
            string.IsNullOrWhiteSpace(holding.Name) ? holding.Symbol : holding.Name,
            ParseDecimal(holding.Balance),
            string.Empty,
            DateTimeOffset.UnixEpoch);
    }

    public static WalletCardItem ToWalletCard(HoldingDto holding)
    {
        ArgumentNullException.ThrowIfNull(holding);
        return new WalletCardItem(
            ToWallet(holding),
            ParseDecimal(holding.UsdValue),
            ParseDecimal(holding.Change24HPct));
    }

    public static Wallet ToWallet(CreateWalletResultDto created)
    {
        ArgumentNullException.ThrowIfNull(created);
        return new Wallet(
            created.WalletId,
            created.Symbol,
            string.IsNullOrWhiteSpace(created.Label) ? created.Symbol : created.Label,
            0m,
            created.Address ?? string.Empty,
            DateTimeOffset.UtcNow);
    }

    private static decimal ParseDecimal(string value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed)
            ? parsed
            : 0m;
}
