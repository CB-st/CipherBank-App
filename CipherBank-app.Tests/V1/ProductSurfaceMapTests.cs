// <copyright file="ProductSurfaceMapTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.V1;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.V1;

public sealed class ProductSurfaceMapTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 10, 30, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ToCryptoCurrency_ParsesInvariantValues_AndFallsBackToSymbolForName()
    {
        HoldingDto holding = new() { Symbol = "BTC", Name = " ", UsdValue = "1234.5", Change24HPct = "-2.25" };

        var crypto = ProductSurfaceMap.ToCryptoCurrency(holding);

        crypto.Symbol.ToString().Should().Be("BTC");
        crypto.Name.Should().Be("BTC");
        crypto.CurrentPrice.Should().Be(1234.5m);
        crypto.PercentChange24H.Should().Be(-2.25m);
        crypto.IconUrl.Should().BeNull();
    }

    [Fact]
    public void ToWallet_FromHolding_UsesNameBalanceAndEpoch()
    {
        HoldingDto holding = new() { Symbol = "ETH", Name = "Ether", Balance = "not-a-number" };

        var wallet = ProductSurfaceMap.ToWallet(holding);

        wallet.Id.Should().Be("ETH");
        wallet.CryptoName.Should().Be("Ether");
        wallet.Balance.Should().Be(0m);
        wallet.Address.Should().BeEmpty();
        wallet.CreatedAt.Should().Be(DateTimeOffset.UnixEpoch);
    }

    [Fact]
    public void ToWalletCard_CarriesUsdValueAndChange()
    {
        HoldingDto holding = new() { Symbol = "BTC", Name = "Bitcoin", Balance = "0.5", UsdValue = "30000", Change24HPct = "1.5" };

        var card = ProductSurfaceMap.ToWalletCard(holding);

        card.Wallet.Balance.Should().Be(0.5m);
        card.UsdValue.Should().Be(30000m);
        card.PercentChange24H.Should().Be(1.5m);
    }

    [Fact]
    public void ToWallet_FromCreateResult_UsesClockAndLabelFallback()
    {
        CreateWalletResultDto created = new() { WalletId = "w-1", Symbol = "BTC", Label = string.Empty, Address = null };

        var wallet = ProductSurfaceMap.ToWallet(created, new FixedClock(FixedNow));

        wallet.Id.Should().Be("w-1");
        wallet.CryptoName.Should().Be("BTC");
        wallet.Balance.Should().Be(0m);
        wallet.Address.Should().BeEmpty();
        wallet.CreatedAt.Should().Be(FixedNow);
    }

    [Fact]
    public void ToWallet_FromCreateResult_DefaultClockAndLabel()
    {
        CreateWalletResultDto created = new() { WalletId = "w-2", Symbol = "ETH", Label = "Savings", Address = "0xabc" };
        DateTimeOffset before = DateTimeOffset.UtcNow;

        var wallet = ProductSurfaceMap.ToWallet(created);

        wallet.CryptoName.Should().Be("Savings");
        wallet.Address.Should().Be("0xabc");
        wallet.CreatedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void Mappers_RejectNullInputs()
    {
        FluentActions.Invoking(() => ProductSurfaceMap.ToCryptoCurrency(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => ProductSurfaceMap.ToWallet((HoldingDto)null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => ProductSurfaceMap.ToWalletCard(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => ProductSurfaceMap.ToWallet(new CreateWalletResultDto(), null!)).Should().Throw<ArgumentNullException>();
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
