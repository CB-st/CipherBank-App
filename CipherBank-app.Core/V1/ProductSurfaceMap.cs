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

    public static Wallet ToWallet(CreateWalletResultDto created) => ToWallet(created, TimeProvider.System);

    /// <summary>Maps a create-wallet result using <paramref name="clock"/> for the creation time.</summary>
    /// <param name="created">The create-wallet response.</param>
    /// <param name="clock">Time source for the wallet's creation timestamp.</param>
    /// <returns>The new wallet with a zero balance.</returns>
    public static Wallet ToWallet(CreateWalletResultDto created, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        ArgumentNullException.ThrowIfNull(created);
        return new Wallet(
            created.WalletId,
            created.Symbol,
            string.IsNullOrWhiteSpace(created.Label) ? created.Symbol : created.Label,
            0m,
            created.Address ?? string.Empty,
            clock.GetUtcNow());
    }

    public static WalletCardItem ToWalletCard(HoldingDto holding)
    {
        ArgumentNullException.ThrowIfNull(holding);
        return new WalletCardItem(
            ToWallet(holding),
            ParseDecimal(holding.UsdValue),
            ParseDecimal(holding.Change24HPct));
    }

    private static decimal ParseDecimal(string value)
        => decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal parsed)
            ? parsed
            : 0m;
}
