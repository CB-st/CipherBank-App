// <copyright file="PersistOptionsBindingTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CipherBank_app.Tests.Configuration;

public sealed class PersistOptionsBindingTests
{
    /// <summary>
    /// Production defaults contain no demo payees; Development explicitly opts into the stable demo rows.
    /// Use: Medium. Scope: persist options contract.
    /// </summary>
    [Fact]
    public void EmbeddedAppSettings_SeparatesProductionAndDevelopmentRecipients()
    {
        PersistenceOptions options = EmbeddedAppSettings.BindPersistence();
        PersistenceOptions development = EmbeddedAppSettings.BindPersistence("Development");

        options.DatabaseName.Should().Be("cipherbank.db");
        Path.GetFileName(options.DatabaseName).Should().Be(options.DatabaseName);
        options.AreDefaultRecipientsValid().Should().BeTrue();
        options.DefaultRecipients.Should().BeEmpty();
        development.DefaultRecipients.Should().HaveCount(2);
        development.DefaultRecipients[0].Id.Should().Be("seed:rent-4th-st");
        development.DefaultRecipients[1].Id.Should().Be("seed:utilities-co");
    }

    [Theory]
    [InlineData("")]
    [InlineData("../cipherbank.db")]
    [InlineData("/tmp/cipherbank.db")]
    public void PersistenceOptions_InvalidDatabaseName_FailsValidation(string databaseName)
    {
        PersistenceOptions options = new() { DatabaseName = databaseName };

        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void BuildForHost_DevelopmentFlag_SelectsDevelopmentOverlay()
    {
        IConfiguration configuration = CipherBankDefaultsConfiguration.BuildForHost(
            isDevelopment: true,
            isWindows: false);

        configuration.GetSection("PersistenceOptions:DefaultRecipients").GetChildren().Should().HaveCount(2);
    }

    /// <summary>
    /// Unbound SyncScheduler.MaxConcurrency stays 0 (unset); Resolve uses half the CPU count.
    /// Use: Medium. Scope: persist options contract.
    /// </summary>
    [Fact]
    public void EmbeddedAppSettings_UnboundSyncSchedulerResolvesHalfCores()
    {
        SyncSchedulerOptions options = EmbeddedAppSettings.BindOptions<SyncSchedulerOptions>();
        options.MaxConcurrency.Should().Be(0);
        int expected = Math.Clamp(
            (int)Math.Ceiling(Environment.ProcessorCount / 2.0),
            SyncSchedulerOptions.MinConcurrency,
            SyncSchedulerOptions.MaxAllowedConcurrency);
        options.Resolve().Should().Be(expected);
        options.Resolve().Should().BeInRange(
            SyncSchedulerOptions.MinConcurrency,
            SyncSchedulerOptions.MaxAllowedConcurrency);
    }

    [Fact]
    public void EmbeddedAppSettings_BindsValidUserPreferenceDefaults()
    {
        UserPreferenceDefaultsOptions options =
            EmbeddedAppSettings.BindOptions<UserPreferenceDefaultsOptions>();

        options.IsValid().Should().BeTrue();
        options.BaseCurrency.Should().Be("USD");
        options.HomeOrder.Should().Contain("holdings");
    }

    [Theory]
    [InlineData("stacked")]
    [InlineData("")]
    public void UserPreferenceDefaultsOptions_InvalidLayout_FailsValidation(string layout)
    {
        UserPreferenceDefaultsOptions options = CreateValidUserPreferenceDefaults();
        options.AssetsLayout = layout;

        options.IsValid().Should().BeFalse();
    }

    [Theory]
    [InlineData("wire")]
    [InlineData("")]
    public void UserPreferenceDefaultsOptions_InvalidSendSpeed_FailsValidation(string speed)
    {
        UserPreferenceDefaultsOptions options = CreateValidUserPreferenceDefaults();
        options.DefaultSendSpeed = speed;

        options.IsValid().Should().BeFalse();
    }

    [Theory]
    [InlineData("high-contrast")]
    [InlineData("")]
    public void UserPreferenceDefaultsOptions_InvalidAppearance_FailsValidation(string appearance)
    {
        UserPreferenceDefaultsOptions options = CreateValidUserPreferenceDefaults();
        options.Appearance = appearance;

        options.IsValid().Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UserPreferenceDefaultsOptions_BlankBaseCurrency_FailsValidation(string baseCurrency)
    {
        UserPreferenceDefaultsOptions options = CreateValidUserPreferenceDefaults();
        options.BaseCurrency = baseCurrency;

        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void UserPreferenceDefaultsOptions_NegativeLockIdle_FailsValidation()
    {
        UserPreferenceDefaultsOptions options = CreateValidUserPreferenceDefaults();
        options.LockIdleSeconds = -1;

        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void UserPreferenceDefaultsOptions_EmptyHomeOrder_FailsValidation()
    {
        UserPreferenceDefaultsOptions options = CreateValidUserPreferenceDefaults();
        options.HomeOrder.Clear();

        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void UserPreferenceDefaultsOptions_BlankHomeOrderEntry_FailsValidation()
    {
        UserPreferenceDefaultsOptions options = CreateValidUserPreferenceDefaults();
        options.HomeOrder[0] = " ";

        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void UserPreferenceDefaultsOptions_EmptyEnabledCurrencies_FailsValidation()
    {
        UserPreferenceDefaultsOptions options = CreateValidUserPreferenceDefaults();
        options.EnabledCurrencies.Clear();

        options.IsValid().Should().BeFalse();
    }

    [Fact]
    public void AddRequiredOptions_MissingClassNamedSectionThrows()
    {
        ServiceCollection services = new();
        ConfigurationManager configuration = new();

        Action bind = () => services.AddRequiredOptions(configuration, new PersistenceOptions());

        bind.Should().Throw<InvalidOperationException>();
    }

    private static UserPreferenceDefaultsOptions CreateValidUserPreferenceDefaults()
    {
        UserPreferenceDefaultsOptions options = new()
        {
            AssetsLayout = "separate",
            DefaultSendSpeed = "instant",
            Appearance = "dark",
            BaseCurrency = "USD",
            LockIdleSeconds = 120,
        };
        options.HomeOrder.Add("holdings");
        options.EnabledCurrencies.Add("USD");
        return options;
    }
}
