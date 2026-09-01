// <copyright file="PersistOptionsBindingTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.Tests.Configuration;

public sealed class PersistOptionsBindingTests
{
    /// <summary>
    /// Embedded appsettings binds Persistence.DatabaseName and the two demo payee seeds.
    /// Use: Medium. Scope: persist options contract.
    /// </summary>
    [Fact]
    public void EmbeddedAppSettings_BindPersistenceDatabaseNameAndDefaultRecipients()
    {
        PersistenceOptions options = EmbeddedAppSettings.BindPersistence();
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
        SyncSchedulerOptions options = EmbeddedAppSettings.BindOptions<SyncSchedulerOptions>(
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
}
