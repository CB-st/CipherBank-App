// <copyright file="ConfigurationTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Configuration;
using CipherBank_app.Custody;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CipherBank_app.Tests.Configuration;

public sealed class ConfigurationTests
{
    [Fact]
    public void EmbeddedDefaults_BindSecurityAndDispatchThemes()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build();

        var cryptography = configuration
            .GetSection(CryptographyOptions.SectionName)
            .Get<CryptographyOptions>();
        var scheduler = configuration
            .GetSection(SyncSchedulerOptions.SectionName)
            .Get<SyncSchedulerOptions>();

        cryptography.Should().NotBeNull();
        cryptography!.IsValid().Should().BeTrue();
        scheduler.Should().NotBeNull();
        scheduler!.MaxConcurrency.Should().Be(2);
    }

    [Fact]
    public void AesGcmCryptoBox_ConfiguredDefaults_RoundTrips()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build();
        var options = configuration
            .GetSection(CryptographyOptions.SectionName)
            .Get<CryptographyOptions>()!;
        AesGcmCryptoBox cryptoBox = new(options);

        var sealedBlob = cryptoBox.Seal("alpha beta gamma", "123456");

        cryptoBox.Open(sealedBlob, "123456").Should().Be("alpha beta gamma");
    }
}
