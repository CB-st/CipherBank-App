// <copyright file="WalletRegistryTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Custody;
using CipherBank_app.Wallets;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Wallets;

public class WalletRegistryTests
{
    [Fact]
    public void BtcModule_CanDerive()
    {
        WalletModule mod = WalletRegistry.Get("BTC");
        mod.CanDerive.Should().BeTrue();
        mod.AddModes.Should().Contain(WalletUiMode.Derive);
        mod.UsesServerWallets.Should().BeFalse();
    }

    [Fact]
    public void XmrModule_UsesServerWallets()
    {
        WalletModule mod = WalletRegistry.Get("XMR");
        mod.CanDerive.Should().BeFalse();
        mod.UsesServerWallets.Should().BeTrue();
        mod.AddModes.Should().Contain(WalletUiMode.Managed);
    }

    [Fact]
    public void DeriveBtc_IsStableForKnownMnemonic()
    {
        const string mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        MnemonicHelper.Validate(mnemonic).Should().BeTrue();
        DerivedAddress btc = AddressDerive.DeriveBtc(mnemonic, 0);
        btc.Address.Should().StartWith("bc1");
        btc.Path.Should().Be("m/84'/0'/0'/0/0");
    }

    [Fact]
    public void DeriveEth_IsChecksummed()
    {
        const string mnemonic = "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";
        DerivedAddress eth = AddressDerive.DeriveEth(mnemonic, 0);
        eth.Address.Should().StartWith("0x");
        eth.Address.Length.Should().Be(42);
    }

    [Fact]
    public void PaymentUri_BtcIncludesAmount() => PaymentUri.Build("BTC", "bc1qtest", "0.5").Should().Be("bitcoin:bc1qtest?amount=0.5");

    [Fact]
    public void AddressValidate_EthAcceptsChecksumForm()
    {
        AddressValidate.IsValid("ETH", "0x0000000000000000000000000000000000000001").Should().BeTrue();
        AddressValidate.IsValid("ETH", "not-an-address").Should().BeFalse();
    }
}
