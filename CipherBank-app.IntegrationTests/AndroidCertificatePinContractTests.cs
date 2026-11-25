// <copyright file="AndroidCertificatePinContractTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Xml.Linq;
using CipherBank_app.Security;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.IntegrationTests;

public sealed class AndroidCertificatePinContractTests
{
    [Fact]
    public void NetworkSecurityConfig_MatchesPolicyPins()
    {
        // The project embeds the actual Android resource, so staged test artifacts need no checkout.
        using Stream xml = typeof(AndroidCertificatePinContractTests).Assembly
            .GetManifestResourceStream("Android.network_security_config.xml")!;
        XDocument configuration = XDocument.Load(xml);

        configuration.Root!.Name.Should().Be(XName.Get("network-security-config"));
        configuration.Root.Elements("domain-config").Should().HaveCount(2);
        configuration.Descendants("pin").Should().HaveCount(4);
        AssertHostPins(
            configuration,
            CertificatePinPolicy.ProductionHost,
            CertificatePinPolicy.ProductionPin,
            CertificatePinPolicy.BackupPin);
        AssertHostPins(
            configuration,
            CertificatePinPolicy.SandboxHost,
            CertificatePinPolicy.SandboxPin,
            CertificatePinPolicy.SandboxBackupPin);
    }

    [Fact]
    public void ToAndroidNetworkSecurityPin_StripsSha256Prefix() =>
        ToAndroidNetworkSecurityPin(
                "sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=")
            .Should()
            .Be("YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=");

    [Fact]
    public void ToAndroidNetworkSecurityPin_RejectsDigestThatOmitsThePrefix()
    {
        Action act = () => ToAndroidNetworkSecurityPin(
            "YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=");

        act.Should().Throw<ArgumentException>();
    }

    private static void AssertHostPins(XDocument configuration, string host, params string[] policyPins)
    {
        XElement domainConfig = configuration.Root!.Elements("domain-config")
            .Single(config => (string?)config.Element("domain") == host);
        domainConfig.Elements("domain").Should().ContainSingle()
            .Which.Attribute("includeSubdomains")!.Value.Should().Be("true");
        XElement[] pins = domainConfig.Elements("pin-set").Single().Elements("pin").ToArray();
        pins.Should().OnlyContain(pin => (string?)pin.Attribute("digest") == "SHA-256");
        pins.Select(pin => pin.Value).Should()
            .BeEquivalentTo(policyPins.Select(ToAndroidNetworkSecurityPin));
    }

    /// <summary>
    /// Converts a C# <c>sha256/</c> pin to the base64 digest Android's
    /// <c>network_security_config.xml</c> pin element expects.
    /// </summary>
    private static string ToAndroidNetworkSecurityPin(string policyPin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyPin);
        if (!policyPin.StartsWith(CertificatePinPolicy.SpkiSha256Prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "C# SPKI pins must start with sha256/. Android network_security_config.xml stores only the base64 digest because digest=\"SHA-256\" already names the algorithm.",
                nameof(policyPin));
        }

        return policyPin[CertificatePinPolicy.SpkiSha256Prefix.Length..];
    }
}
