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
}
