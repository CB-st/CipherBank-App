// <copyright file="WalletSurfaceCoverageTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Models;
using CipherBank_app.Pos;
using CipherBank_app.Wallets;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Wallets;

/// <summary>Coverlet pad for wallet URI/derive/validate + null NFC. Use: High (CI). Scope: Core wallets/POS.</summary>
public sealed class WalletSurfaceCoverageTests
{
    private const string KnownMnemonic =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    /// <summary>PaymentUri schemes and Shorten. Use: High. Scope: PaymentUri.</summary>
    [Fact]
    public void PaymentUri_CoversSchemesAndShorten()
    {
        PaymentUri.Build("BTC", "  ").Should().BeEmpty();
        PaymentUri.Build("LTC", "ltc1qtest", "1", "lab", "hi")
            .Should().StartWith("litecoin:ltc1qtest?")
            .And.Contain("amount=1")
            .And.Contain("label=lab")
            .And.Contain("message=hi");
        PaymentUri.Build("ETH", "0xabc", "0.1").Should().Be("ethereum:0xabc?value=100000000000000000");
        PaymentUri.Build("XMR", "4abc", null).Should().Be("monero:4abc");
        PaymentUri.Build("USD", "user-1").Should().Contain("cipherbank:receive/USD");
        PaymentUri.Build("DOGE", "Dtest").Should().StartWith("dogecoin:Dtest");
        PaymentUri.Shorten("short").Should().Be("short");
        PaymentUri.Shorten("abcdefghijklmnopqrstuvwxyz", 4, 3).Should().Be("abcd…xyz");
    }

    /// <summary>AddressValidate branches. Use: High. Scope: AddressValidate.</summary>
    [Fact]
    public void AddressValidate_CoversSymbolsAndFailures()
    {
        AddressValidate.IsValid("BTC", string.Empty).Should().BeFalse();
        AddressValidate.IsValid("BTC", "   ").Should().BeFalse();
        AddressValidate.IsValid(" ", "long-enough").Should().BeFalse();
        AddressValidate.IsValid(null!, "long-enough").Should().BeFalse();
        AddressValidate.IsValid("BTC", "not-valid").Should().BeFalse();
        AddressValidate.IsValid("LTC", "not-valid").Should().BeFalse();
        AddressValidate.IsValid("DOGE", "not-valid").Should().BeFalse();
        AddressValidate.IsValid("XMR", new string('a', 100)).Should().BeTrue();
        AddressValidate.IsValid("XMR", "short").Should().BeFalse();
        AddressValidate.IsValid("XYZ", "long-enough").Should().BeTrue();
        AddressValidate.IsValid("XYZ", "tiny").Should().BeFalse();
    }

    /// <summary>AddressDerive dispatch including LTC/DOGE. Use: High. Scope: AddressDerive.</summary>
    [Fact]
    public void AddressDerive_CoversDispatchAndLtcDoge()
    {
        AddressDerive.IsDerivable("btc").Should().BeTrue();
        AddressDerive.IsDerivable("XMR").Should().BeFalse();
        AddressDerive.Derive("XMR", KnownMnemonic).Should().BeNull();

        DerivedAddress? viaDispatch = AddressDerive.Derive("BTC", KnownMnemonic, 0);
        viaDispatch.Should().NotBeNull();
        viaDispatch!.Address.Should().StartWith("bc1");

        DerivedAddress ltc = AddressDerive.DeriveRequired(AssetSymbol.Ltc, KnownMnemonic, 0);
        ltc.Address.Should().NotBeNullOrWhiteSpace();
        ltc.Path.Should().Contain("84'/2'");

        DerivedAddress doge = AddressDerive.DeriveRequired(AssetSymbol.Doge, KnownMnemonic, 0);
        doge.Address.Should().NotBeNullOrWhiteSpace();
        doge.Path.Should().Contain("44'/3'");
    }

    /// <summary>Server-backed creates follow the module flag. Use: Medium. Scope: IWalletModule.</summary>
    [Fact]
    public void WalletModule_UsesServerWallets_OnlyForMonero()
    {
        WalletRegistry.Get(AssetSymbol.Btc).UsesServerWallets.Should().BeFalse();
        WalletRegistry.Get(AssetSymbol.Xmr).UsesServerWallets.Should().BeTrue();
        WalletRegistry.Get(AssetSymbol.Xmr).AddModes.Should().Contain(WalletUiMode.Unmanaged);
    }

    /// <summary>QR PNG generation. Use: Medium. Scope: QrCodeGenerator.</summary>
    [Fact]
    public void QrCodeGenerator_ProducesPngBytes()
    {
        QrPng png = QrCodeGenerator.ToPng("bitcoin:bc1qtest");
        png.Bytes.Length.Should().BeGreaterThan(0);
        png.Bytes.Span[0].Should().Be(0x89);
        QrPng.MediaType.Should().Be("image/png");
    }

    /// <summary>NullNfcPresentmentService failure path. Use: Medium. Scope: NullNfcPresentmentService.</summary>
    [Fact]
    public async Task NullNfcPresentmentService_ReportsUnsupported()
    {
        NullNfcPresentmentService nfc = new NullNfcPresentmentService();
        nfc.IsSupported.Should().BeFalse();
        nfc.LastError.Should().Contain("Android");
        bool ok = await nfc.PresentAsync(new NfcPresentmentPayload(), CancellationToken.None);
        ok.Should().BeFalse();
        nfc.LastError.Should().Contain("Simulate");
        bool ok2 = await nfc.PresentAsync(new NfcPresentmentPayload(), TimeSpan.FromSeconds(1), CancellationToken.None);
        ok2.Should().BeFalse();
    }
}
