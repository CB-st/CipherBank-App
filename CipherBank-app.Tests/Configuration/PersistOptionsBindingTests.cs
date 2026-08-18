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
    /// Themed embedded config binds Persistence.DatabaseName through IOptions.
    /// Use: Medium. Scope: persist options contract.
    /// </summary>
    [Fact]
    public void EmbeddedAppSettings_BindPersistenceDatabaseName()
    {
        IConfiguration config = CipherBankDefaultsConfiguration.Build();
        PersistenceOptions options = BindOptions<PersistenceOptions>(config, PersistenceOptions.SectionName);
        options.DatabaseName.Should().Be("cipherbank.db");
        Path.GetFileName(options.DatabaseName).Should().Be(options.DatabaseName);
    }

    /// <summary>
    /// Themed SyncScheduler overlay omits MaxConcurrency, so the bind keeps MinConcurrency.
    /// Use: Medium. Scope: persist options contract.
    /// </summary>
    [Fact]
    public void EmbeddedAppSettings_BindSyncSchedulerDefaultsToMinConcurrency()
    {
        IConfiguration config = CipherBankDefaultsConfiguration.Build();
        SyncSchedulerOptions options = BindOptions<SyncSchedulerOptions>(
            config,
            SyncSchedulerOptions.SectionName);
        options.MaxConcurrency.Should().Be(SyncSchedulerOptions.MinConcurrency);
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
