// <copyright file="ConfigurationTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Animations;
using CipherBank_app.Configuration;
using CipherBank_app.Custody;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CipherBank_app.Tests.Configuration;

public sealed class ConfigurationTests
{
    [Fact]
    public void EmbeddedDefaults_BindSecurityAndDispatchThemes()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build();

        CryptographyOptions? cryptography = configuration
            .GetSection(nameof(CryptographyOptions))
            .Get<CryptographyOptions>();
        SyncSchedulerOptions? scheduler = configuration
            .GetSection(nameof(SyncSchedulerOptions))
            .Get<SyncSchedulerOptions>();

        cryptography.Should().NotBeNull();
        cryptography!.IsValid().Should().BeTrue();
        cryptography.MatchesPersistedProfile().Should().BeTrue();
        scheduler.Should().NotBeNull();
        scheduler!.MaxConcurrency.Should().Be(0);
        scheduler.Resolve().Should().BeInRange(
            SyncSchedulerOptions.MinConcurrency,
            SyncSchedulerOptions.MaxAllowedConcurrency);
    }

    [Fact]
    public void CryptographyOptions_NonDefaultKeySize_DoesNotMatchPersistedProfile()
    {
        CryptographyOptions options = CryptographyOptions.Default;
        options.KeySizeBytes = CryptographyOptions.Aes128KeySizeBytes;
        options.IsValid().Should().BeTrue();
        options.MatchesPersistedProfile().Should().BeFalse();
    }

    [Fact]
    public void Build_Default_UsesProductionDatabaseName()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build();

        configuration["PersistenceOptions:DatabaseName"].Should().Be("cipherbank.db");
    }

    [Fact]
    public void Build_Development_KeepsDatabaseNameWhenOverlayOmitsIt()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build("Development");

        configuration["PersistenceOptions:DatabaseName"].Should().Be("cipherbank.db");
    }

    [Fact]
    public void Build_Production_KeepsDefaultDatabaseName()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build("Production");

        configuration["PersistenceOptions:DatabaseName"].Should().Be("cipherbank.db");
    }

    [Fact]
    public void Build_WindowsOverlay_KeepsDatabaseNameWhenOverlayOmitsIt()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build(windowsOverlay: true);

        configuration["PersistenceOptions:DatabaseName"].Should().Be("cipherbank.db");
    }

    [Fact]
    public void AesGcmCryptoBox_ConfiguredDefaults_RoundTrips()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build();
        CryptographyOptions options = configuration
            .GetSection(nameof(CryptographyOptions))
            .Get<CryptographyOptions>()!;
        AesGcmCryptoBox cryptoBox = new(options);

        string sealedBlob = cryptoBox.Seal("alpha beta gamma", "123456");

        cryptoBox.Open(sealedBlob, "123456").Should().Be("alpha beta gamma");
    }

    [Fact]
    public void ClassNamedSections_BindCoraAndCarousel()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.Build();

        configuration.GetSection(nameof(CoraOptions)).Get<CoraOptions>()!.Fallback.Should().Be("CipherBank.");
        configuration.GetSection(nameof(CarouselLayoutConfig)).Get<CarouselLayoutConfig>()!.Stride.Should().Be(220);
        CryptographyOptions.SectionName.Should().Be(nameof(CryptographyOptions));
        CoraOptions.SectionName.Should().Be(nameof(CoraOptions));
        CarouselLayoutConfig.SectionName.Should().Be(nameof(CarouselLayoutConfig));
        new CarouselLayoutConfig().Should().Be(CarouselLayoutConfig.Default);
        configuration.GetSection("Cora").Exists().Should().BeFalse();
        configuration.GetSection("Carousel").Exists().Should().BeFalse();
        configuration.GetSection("Cryptography").Exists().Should().BeFalse();
    }

    [Fact]
    public void CryptographyOptions_NonDefaultProfile_FailsValidation()
    {
        Dictionary<string, string?> values = new Dictionary<string, string?>
        {
            ["CryptographyOptions:KeySizeBytes"] = "16",
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(CipherBankDefaultsConfiguration.Build())
            .AddInMemoryCollection(values)
            .Build();
        ServiceCollection services = new ServiceCollection();
        services.AddCipherBankCoreOptions(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Action act = () => _ = provider.GetRequiredService<IOptions<CryptographyOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void SyncSchedulerOptions_NonDefaultConcurrency_FailsValidation()
    {
        Dictionary<string, string?> values = new Dictionary<string, string?>
        {
            ["SyncSchedulerOptions:MaxConcurrency"] = "99",
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddConfiguration(CipherBankDefaultsConfiguration.Build())
            .AddInMemoryCollection(values)
            .Build();
        ServiceCollection services = new ServiceCollection();
        services.AddCipherBankCoreOptions(configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        Action act = () => _ = provider.GetRequiredService<IOptions<SyncSchedulerOptions>>().Value;

        act.Should().Throw<OptionsValidationException>();
    }

    [Fact]
    public void PersistenceOptions_PathDatabaseName_FailsValidator()
    {
        PersistenceOptionsValidator validator = new();
        PersistenceOptions options = new() { DatabaseName = "../cipherbank.db" };

        ValidateOptionsResult result = validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
        result.FailureMessage.Should().Contain("DatabaseName must not contain a path.");
    }

    [Fact]
    public void UserPreferenceDefaultsOptions_InvalidLayout_FailsValidator()
    {
        UserPreferenceDefaultsOptionsValidator validator = new();
        UserPreferenceDefaultsOptions options = new() { AssetsLayout = "stacked" };

        ValidateOptionsResult result = validator.Validate(Options.DefaultName, options);

        result.Failed.Should().BeTrue();
    }
}
