// <copyright file="WalletCardItem.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Globalization;

namespace CipherBank_app.Models;

/// <summary>
/// Presentation model pairing a wallet with its current market data for the deck card.
/// </summary>
public record WalletCardItem(Wallet Wallet, decimal UsdValue, decimal PercentChange24h)
{
    public string Symbol => Wallet.CryptoSymbol;

    public string Name => Wallet.CryptoName;

    public string FormattedBalance => Wallet.FormattedBalance;

    public string FormattedUsdValue => $"${UsdValue.ToString("N2", CultureInfo.InvariantCulture)}";

    public bool IsPriceUp => PercentChange24h >= 0;

    public string FormattedPercentChange =>
        $"{(PercentChange24h >= 0 ? "+" : string.Empty)}{PercentChange24h.ToString("F2", CultureInfo.InvariantCulture)}%";

    /// <summary>Builds a card from a wallet and its fetched market price.</summary>
    public static WalletCardItem FromWallet(Wallet wallet, CryptoCurrency crypto) =>
        new(wallet, wallet.Balance * crypto.CurrentPrice, crypto.PercentChange24h);

    /// <summary>Builds a card with zeroed market data when the price fetch fails.</summary>
    public static WalletCardItem WithoutPrice(Wallet wallet) =>
        new(wallet, 0m, 0m);
}
