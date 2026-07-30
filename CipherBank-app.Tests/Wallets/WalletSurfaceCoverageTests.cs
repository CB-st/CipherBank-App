// <copyright file="WalletSurfaceCoverageTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Pos;
using CipherBank_app.Wallets;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Wallets;

/// <summary>
/// Coverage for wallet URI/derive/validate helpers and null NFC presentment.
/// Use: High (CI). Scope: Core wallet/POS surfaces with low Coverlet hit rates.
/// </summary>
public sealed class WalletSurfaceCoverageTests
{
    private const string KnownMnemonic =
        "abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon abandon about";

    /// <summary>
    /// Exercises PaymentUri schemes (UTXO, account, fiat) and Shorten.
    /// Use: High. Scope: PaymentUri.
    /// </summary>
    [Fact]
    public void PaymentUri_CoversSchemesAndShorten()
    {
        PaymentUri.Build("BTC", "  ").Should().BeEmpty();
        PaymentUri.Build("LTC", "ltc1qtest", "1", "lab", "hi")
            .Should().StartWith("litecoin:ltc1qtest?")
            .And.Contain("amount=1")
            .And.Contain("label=lab")
            .And.Contain("message=hi");
        PaymentUri.Build("ETH", "0xabc", "0.1").Should().Be("ethereum:0xabc?value=0.1");
        PaymentUri.Build("XMR", "4abc", null).Should().Be("monero:4abc");
        PaymentUri.Build("USD", "user-1").Should().Contain("cipherbank:receive/USD");
        PaymentUri.Build("DOGE", "Dtest").Should().StartWith("dogecoin:Dtest");
        PaymentUri.Shorten("short").Should().Be("short");
        PaymentUri.Shorten("abcdefghijklmnopqrstuvwxyz", 4, 3).Should().Be("abcd…xyz");
    }

    /// <summary>
    /// Exercises AddressValidate branches including invalid catch paths.
    /// Use: High. Scope: AddressValidate.
    /// </summary>
    [Fact]
    public void AddressValidate_CoversSymbolsAndFailures()
    {
        AddressValidate.IsValid("BTC", string.Empty).Should().BeFalse();
        AddressValidate.IsValid("BTC", "not-valid").Should().BeFalse();
        AddressValidate.IsValid("LTC", "not-valid").Should().BeFalse();
        AddressValidate.IsValid("DOGE", "not-valid").Should().BeFalse();
        AddressValidate.IsValid("XMR", new string('a', 100)).Should().BeTrue();
        AddressValidate.IsValid("XMR", "short").Should().BeFalse();
        AddressValidate.IsValid("XYZ", "long-enough").Should().BeTrue();
        AddressValidate.IsValid("XYZ", "tiny").Should().BeFalse();
    }

    /// <summary>
    /// Exercises AddressDerive dispatch including LTC/DOGE and non-derivable symbols.
    /// Use: High. Scope: AddressDerive.
    /// </summary>
    [Fact]
    public void AddressDerive_CoversDispatchAndLtcDoge()
    {
        AddressDerive.IsDerivable("btc").Should().BeTrue();
        AddressDerive.IsDerivable("XMR").Should().BeFalse();
        AddressDerive.Derive("XMR", KnownMnemonic).Should().BeNull();

        DerivedAddress? viaDispatch = AddressDerive.Derive("BTC", KnownMnemonic, 0);
        viaDispatch.Should().NotBeNull();
        viaDispatch!.Address.Should().StartWith("bc1");

        DerivedAddress ltc = AddressDerive.DeriveLtc(KnownMnemonic, 0);
        ltc.Address.Should().NotBeNullOrWhiteSpace();
        ltc.Path.Should().Contain("84'/2'");

        DerivedAddress doge = AddressDerive.DeriveDoge(KnownMnemonic, 0);
        doge.Address.Should().NotBeNullOrWhiteSpace();
        doge.Path.Should().Contain("44'/3'");
    }

    /// <summary>
    /// Exercises WalletModule.SourceFor mapping.
    /// Use: Medium. Scope: WalletModule.
    /// </summary>
    [Fact]
    public void WalletModule_SourceFor_MapsModes()
    {
        var local = new WalletModule
        {
            Symbol = "BTC",
            AddModes = [WalletUiMode.Watch, WalletUiMode.Unmanaged],
            CanDerive = true,
            UsesServerWallets = false,
        };
        local.SourceFor(WalletUiMode.Watch).Should().Be(WalletSource.Watch);
        local.SourceFor(WalletUiMode.Managed).Should().Be(WalletSource.Server);
        local.SourceFor(WalletUiMode.Unmanaged).Should().Be(WalletSource.Local);

        var server = new WalletModule
        {
            Symbol = "XMR",
            AddModes = [WalletUiMode.Managed],
            CanDerive = false,
            UsesServerWallets = true,
        };
        server.SourceFor(WalletUiMode.Unmanaged).Should().Be(WalletSource.Server);
    }

    /// <summary>
    /// Exercises QR PNG generation for a receive URI payload.
    /// Use: Medium. Scope: QrCodeGenerator.
    /// </summary>
    [Fact]
    public void QrCodeGenerator_ProducesPngBytes()
    {
        byte[] png = QrCodeGenerator.ToPngBytes("bitcoin:bc1qtest");
        png.Should().NotBeEmpty();
        png[0].Should().Be(0x89); // PNG magic
    }

    /// <summary>
    /// Exercises NullNfcPresentmentService failure path.
    /// Use: Medium. Scope: NullNfcPresentmentService.
    /// </summary>
    [Fact]
    public async Task NullNfcPresentmentService_ReportsUnsupported()
    {
        var nfc = new NullNfcPresentmentService();
        nfc.IsSupported.Should().BeFalse();
        nfc.LastError.Should().Contain("Android");
        bool ok = await nfc.PresentAsync(new NfcPresentmentPayload(), CancellationToken.None);
        ok.Should().BeFalse();
        nfc.LastError.Should().Contain("Simulate");
        bool ok2 = await nfc.PresentAsync(new NfcPresentmentPayload(), TimeSpan.FromSeconds(1), CancellationToken.None);
        ok2.Should().BeFalse();
    }
}
