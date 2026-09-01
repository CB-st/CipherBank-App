// <copyright file="WalletCardItemTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Models;

public class WalletCardItemTests
{
    [Fact]
    public void FromWallet_ComputesUsdValueAndPassesPercent()
    {
        WalletCardItem card = WalletCardItem.FromWallet(MakeWallet(2m), MakeCrypto(price: 50000m, percent: 3.5m));

        card.UsdValue.Should().Be(100000m);
        card.PercentChange24h.Should().Be(3.5m);
        card.IsPriceUp.Should().BeTrue();
        card.Symbol.Should().Be("BTC");
        card.FormattedBalance.Should().Be("2.00000000 BTC");
        card.Name.Should().Be("Bitcoin");
        card.FormattedUsdValue.Should().Be("$100,000.00");
    }

    [Fact]
    public void FromWallet_NegativeChange_IsNotPriceUp()
    {
        WalletCardItem card = WalletCardItem.FromWallet(MakeWallet(1m), MakeCrypto(price: 10m, percent: -2.25m));

        card.IsPriceUp.Should().BeFalse();
        card.FormattedPercentChange.Should().Be("-2.25%");
    }

    [Fact]
    public void WithoutPrice_ZeroesMarketData()
    {
        WalletCardItem card = WalletCardItem.WithoutPrice(MakeWallet(5m));

        card.UsdValue.Should().Be(0m);
        card.PercentChange24h.Should().Be(0m);
        card.FormattedPercentChange.Should().Be("+0.00%");
    }

    private static Wallet MakeWallet(decimal balance) =>
        new("w1", "BTC", "Bitcoin", balance, "addr-1", DateTimeOffset.UnixEpoch);

    private static CryptoCurrency MakeCrypto(decimal price, decimal percent) =>
        new("BTC", "Bitcoin", price, PriceChange24h: 0, percent, MarketCap: 0, Volume24h: 0, IconUrl: null);
}
