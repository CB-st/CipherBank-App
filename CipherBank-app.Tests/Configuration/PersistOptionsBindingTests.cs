// <copyright file="PersistOptionsBindingTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Reflection;
using CipherBank_app.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CipherBank_app.Tests.Configuration;

public sealed class PersistOptionsBindingTests
{
    /// <summary>
    /// Embedded appsettings already carries Persistence.DatabaseName for later IOptions bind.
    /// Use: Medium. Scope: persist options contract on M2.
    /// </summary>
    [Fact]
    public void EmbeddedAppSettings_BindPersistenceDatabaseName()
    {
        IConfiguration config = LoadEmbeddedAppSettings();
        PersistenceOptions options = new PersistenceOptions();
        config.GetSection(PersistenceOptions.SectionName).Bind(options);
        options.DatabaseName.Should().Be("cipherbank.db");
        Path.GetFileName(options.DatabaseName).Should().Be(options.DatabaseName);
    }

    /// <summary>
    /// Empty SyncScheduler section leaves the CPU-derived mobile cap in [1, 2].
    /// Use: Medium. Scope: persist options contract on M2.
    /// </summary>
    [Fact]
    public void EmbeddedAppSettings_BindSyncSchedulerWithinBounds()
    {
        IConfiguration config = LoadEmbeddedAppSettings();
        SyncSchedulerOptions options = new SyncSchedulerOptions();
        config.GetSection(SyncSchedulerOptions.SectionName).Bind(options);
        options.MaxConcurrency.Should().BeInRange(
            SyncSchedulerOptions.MinConcurrency,
            2);
    }

    private static IConfigurationRoot LoadEmbeddedAppSettings()
    {
        Assembly assembly = typeof(PersistenceOptions).Assembly;
        Stream stream = assembly.GetManifestResourceStream("CipherBank_app.Config.appsettings.json")
            ?? throw new InvalidOperationException(
                "Missing embedded configuration resource 'CipherBank_app.Config.appsettings.json'.");
        ConfigurationBuilder builder = new ConfigurationBuilder();
        builder.AddJsonStream(stream);
        return builder.Build();
    }
}
