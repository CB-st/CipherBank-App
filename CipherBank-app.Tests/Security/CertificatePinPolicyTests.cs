// <copyright file="CertificatePinPolicyTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using CipherBank_app.Security;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Security;

public sealed class CertificatePinPolicyTests
{
    [Theory]
    [InlineData("api.cipherbank.money")]
    [InlineData("sub.api.cipherbank.money")]
    [InlineData("api.sandbox.cipherbank.money")]
    public void RequiresPinning_RecognizesOwnedHosts(string host) =>
        CertificatePinPolicy.RequiresPinning(host).Should().BeTrue();

    [Fact]
    public void ComputeSpkiSha256Pin_UsesStableSha256Format()
    {
        string pin = CertificatePinPolicy.ComputeSpkiSha256Pin(
            Encoding.ASCII.GetBytes("cipherbank-test-spki"));

        pin.Should().Be("sha256/ISiAtvm91VBP2VBIgpTKAfPeV/2RH/sai0RFOhdu55o=");
    }

    [Fact]
    public void TryComputeSpkiSha256PinFromCertificateDer_MatchesSubjectPublicKeyInfoExport()
    {
        using RSA rsa = RSA.Create(2048);
        CertificateRequest request = new(
            "CN=cipherbank-pin-test",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        using X509Certificate2 certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(1));
        byte[] certificateDer = certificate.Export(X509ContentType.Cert);
        string expectedPin = CertificatePinPolicy.ComputeSpkiSha256Pin(
            rsa.ExportSubjectPublicKeyInfo());

        CertificatePinPolicy.TryComputeSpkiSha256PinFromCertificateDer(
                certificateDer,
                out string? pin)
            .Should()
            .BeTrue();
        pin.Should().Be(expectedPin);
    }

    [Theory]
    [InlineData("sha256/REPLACE_WITH_PRODUCTION_PIN=", true)]
    [InlineData("REPLACE_WITH_BACKUP_PIN=", true)]
    [InlineData("sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=", false)]
    [InlineData("", false)]
    public void IsPlaceholderPin_DetectsReplaceWithMarker(string pin, bool isPlaceholder) =>
        CertificatePinPolicy.IsPlaceholderPin(pin).Should().Be(isPlaceholder);

    [Fact]
    public void IsPlaceholderPin_RejectsNull()
    {
        Action act = () => CertificatePinPolicy.IsPlaceholderPin(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void EnsurePinsAreNotPlaceholders_ThrowsWhenAnyPinContainsTheMarker()
    {
        Action act = () => CertificatePinPolicy.EnsurePinsAreNotPlaceholders(
            "sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=",
            "sha256/REPLACE_WITH_BACKUP_PIN=");

        act.Should().Throw<InvalidOperationException>().WithMessage("*REPLACE_WITH_*");
    }

    [Fact]
    public void EnsurePinsAreNotPlaceholders_AllowsPinsWithoutTheMarker()
    {
        Action act = () => CertificatePinPolicy.EnsurePinsAreNotPlaceholders(
            "sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=",
            "sha256/AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=");

        act.Should().NotThrow();
    }

    [Fact]
    public void EnsurePinsAreNotPlaceholders_ThrowsOnMalformedPin()
    {
        Action act = () => CertificatePinPolicy.EnsurePinsAreNotPlaceholders(
            "sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=",
            "sha256/BACKUP_PIN_HERE=");

        act.Should().Throw<InvalidOperationException>().WithMessage("*Base64 SHA-256 digest*");
    }

    [Theory]
    [InlineData("sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=", true)]
    [InlineData("sha256/ISiAtvm91VBP2VBIgpTKAfPeV/2RH/sai0RFOhdu55o=", true)]
    [InlineData("YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=", false)] // missing sha256/ prefix
    [InlineData("SHA256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=", false)] // prefix is case-sensitive
    [InlineData("sha256/BACKUP_PIN_HERE=", false)] // prose placeholder, not Base64
    [InlineData("sha256/REPLACE_WITH_PRODUCTION_PIN=", false)]
    [InlineData("sha256/AAAA", false)] // 3-byte digest
    [InlineData("sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2FuihgAAAA=", false)] // 35-byte digest
    [InlineData("sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=\n", false)] // trailing newline
    [InlineData("sha256/YLh1dUR9y6Kja30RrAn7 JKnbQG/uEtLMkBgFF2Fuihg=", false)] // embedded space
    [InlineData("sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihh=", false)] // non-canonical padding bits
    [InlineData("sha256/", false)]
    [InlineData("", false)]
    public void IsWellFormedSpkiSha256Pin_RequiresPrefixAndBase64Sha256Digest(string pin, bool expected) =>
        CertificatePinPolicy.IsWellFormedSpkiSha256Pin(pin).Should().Be(expected);

    [Fact]
    public void IsWellFormedSpkiSha256Pin_RejectsNull()
    {
        Action act = () => CertificatePinPolicy.IsWellFormedSpkiSha256Pin(null!);
        act.Should().Throw<ArgumentNullException>();
    }
}
