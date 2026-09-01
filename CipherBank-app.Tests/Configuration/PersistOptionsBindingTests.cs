// <copyright file="PersistOptionsBindingTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace CipherBank_app.Tests.Configuration;

public sealed class PersistOptionsBindingTests
{
    /// <summary>
    /// Themed embedded config binds Persistence.DatabaseName and the two demo payee seeds.
    /// Use: Medium. Scope: persist options contract.
    /// </summary>
    [Fact]
    public void EmbeddedAppSettings_BindPersistenceDatabaseNameAndDefaultRecipients()
    {
        PersistenceOptions options = BindOptions<PersistenceOptions>(
            CipherBankDefaultsConfiguration.Build(),
            PersistenceOptions.SectionName);
        options.DatabaseName.Should().Be("cipherbank.db");
        Path.GetFileName(options.DatabaseName).Should().Be(options.DatabaseName);
        options.AreDefaultRecipientsValid().Should().BeTrue();
        options.DefaultRecipients.Should().HaveCount(2);
        options.DefaultRecipients[0].Id.Should().Be("seed:rent-4th-st");
        options.DefaultRecipients[0].Name.Should().Be("Rent — 4th St LLC");
        options.DefaultRecipients[1].Id.Should().Be("seed:utilities-co");
        options.DefaultRecipients[1].Name.Should().Be("Utilities Co");
    }

    /// <summary>
    /// Unbound SyncScheduler.MaxConcurrency stays 0 (unset); Resolve uses half the CPU count.
    /// Use: Medium. Scope: persist options contract.
    /// </summary>
    [Fact]
    public void EmbeddedAppSettings_UnboundSyncSchedulerResolvesHalfCores()
    {
        SyncSchedulerOptions options = BindOptions<SyncSchedulerOptions>(
            CipherBankDefaultsConfiguration.Build(),
            SyncSchedulerOptions.SectionName);
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

    private static T BindOptions<T>(IConfiguration config, string sectionName)
        where T : class, new()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddOptions<T>().Bind(config.GetSection(sectionName));
        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<T>>().Value;
    }
}
