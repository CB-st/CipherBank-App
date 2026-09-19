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

    [Fact]
    public void AddRequiredOptions_MissingClassNamedSectionThrows()
    {
        ServiceCollection services = new();
        ConfigurationManager configuration = new();

        Action bind = () => services.AddRequiredOptions<PersistenceOptions>(configuration);

        bind.Should().Throw<InvalidOperationException>();
    }
}
