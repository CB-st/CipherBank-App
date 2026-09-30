// <copyright file="CertificatePinPolicyTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
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
    public void EnsureReleasePinsAreConfigured_RejectsShippedPlaceholderPins()
    {
        CertificatePinPolicy.IsPlaceholderPin(CertificatePinPolicy.ProductionPin).Should().BeTrue();
        CertificatePinPolicy.IsPlaceholderPin(CertificatePinPolicy.BackupPin).Should().BeTrue();
        CertificatePinPolicy.IsPlaceholderPin(CertificatePinPolicy.SandboxPin).Should().BeTrue();
        CertificatePinPolicy.IsPlaceholderPin(CertificatePinPolicy.SandboxBackupPin).Should().BeTrue();

        Action act = CertificatePinPolicy.EnsureReleasePinsAreConfigured;
        act.Should().Throw<InvalidOperationException>().WithMessage("*REPLACE_WITH_*");
    }

    [Fact]
    public void ToAndroidNetworkSecurityPin_StripsSha256Prefix() =>
        CertificatePinPolicy.ToAndroidNetworkSecurityPin(
                "sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=")
            .Should()
            .Be("YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=");

    [Fact]
    public void ToAndroidNetworkSecurityPin_RejectsDigestThatOmitsThePrefix()
    {
        Action act = () => CertificatePinPolicy.ToAndroidNetworkSecurityPin(
            "YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AndroidNetworkSecurityConfig_PinDigestsMatchPolicyWithoutSha256Prefix()
    {
        string path = Path.Combine(
            FindRepositoryRoot(),
            "CipherBank-app",
            "Platforms",
            "Android",
            "Resources",
            "xml",
            "network_security_config.xml");
        XDocument document = XDocument.Load(path);
        Dictionary<string, string[]> pinsByHost = new Dictionary<string, string[]>(StringComparer.Ordinal);

        foreach (XElement domainConfig in document.Descendants("domain-config"))
        {
            string host = domainConfig.Element("domain")!.Value.Trim();
            string[] digests = domainConfig.Descendants("pin")
                .Select(pin =>
                {
                    pin.Attribute("digest")!.Value.Should().Be("SHA-256");
                    string digest = pin.Value.Trim();
                    digest.Should().NotContain("sha256/");
                    return digest;
                })
                .ToArray();
            pinsByHost[host] = digests;
        }

        pinsByHost["api.cipherbank.money"].Should().Equal(
            CertificatePinPolicy.ToAndroidNetworkSecurityPin(CertificatePinPolicy.ProductionPin),
            CertificatePinPolicy.ToAndroidNetworkSecurityPin(CertificatePinPolicy.BackupPin));
        pinsByHost["api.sandbox.cipherbank.money"].Should().Equal(
            CertificatePinPolicy.ToAndroidNetworkSecurityPin(CertificatePinPolicy.SandboxPin),
            CertificatePinPolicy.ToAndroidNetworkSecurityPin(CertificatePinPolicy.SandboxBackupPin));

        static string FindRepositoryRoot()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "CipherBank-app.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Could not locate CipherBank-app.sln from the test output directory.");
        }
    }
}
