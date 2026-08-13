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

        CryptographyOptions? cryptography = configuration
            .GetSection(CryptographyOptions.SectionName)
            .Get<CryptographyOptions>();
        SyncSchedulerOptions? scheduler = configuration
            .GetSection(SyncSchedulerOptions.SectionName)
            .Get<SyncSchedulerOptions>();

        cryptography.Should().NotBeNull();
        cryptography!.IsValid().Should().BeTrue();
        scheduler.Should().NotBeNull();
        scheduler!.MaxConcurrency.Should().Be(2);
    }

    [Fact]
    public void Build_Default_UsesProductionDatabaseName()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build();

        configuration["Persistence:DatabaseName"].Should().Be("cipherbank.db");
    }

    [Fact]
    public void Build_Development_KeepsThemedDatabaseNameWhenOverlayIsAbsent()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build("Development");

        configuration["Persistence:DatabaseName"].Should().Be("cipherbank.db");
    }

    [Fact]
    public void Build_Production_KeepsDefaultDatabaseName()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build("Production");

        configuration["Persistence:DatabaseName"].Should().Be("cipherbank.db");
    }

    [Fact]
    public void Build_WindowsOverlay_KeepsThemedDatabaseNameWhenOverlayIsAbsent()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build(windowsOverlay: true);

        configuration["Persistence:DatabaseName"].Should().Be("cipherbank.db");
    }

    [Fact]
    public void AesGcmCryptoBox_ConfiguredDefaults_RoundTrips()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build();
        CryptographyOptions options = configuration
            .GetSection(CryptographyOptions.SectionName)
            .Get<CryptographyOptions>()!;
        AesGcmCryptoBox cryptoBox = new(options);

        string sealedBlob = cryptoBox.Seal("alpha beta gamma", "123456");

        cryptoBox.Open(sealedBlob, "123456").Should().Be("alpha beta gamma");
    }
}
