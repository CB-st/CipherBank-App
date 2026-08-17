// <copyright file="PersistOptionsBindingTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Reflection;
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
    /// Embedded appsettings binds Persistence.DatabaseName through IOptions.
    /// Use: Medium. Scope: persist options contract.
    /// </summary>
    [Fact]
    public void EmbeddedAppSettings_BindPersistenceDatabaseName()
    {
        IConfiguration config = LoadEmbeddedAppSettings();
        PersistenceOptions options = BindOptions<PersistenceOptions>(config, PersistenceOptions.SectionName);
        options.DatabaseName.Should().Be("cipherbank.db");
        Path.GetFileName(options.DatabaseName).Should().Be(options.DatabaseName);
    }

    /// <summary>
    /// Empty SyncScheduler section leaves the CPU-derived mobile cap in [1, 2].
    /// Use: Medium. Scope: persist options contract.
    /// </summary>
    [Fact]
    public void EmbeddedAppSettings_BindSyncSchedulerWithinBounds()
    {
        IConfiguration config = LoadEmbeddedAppSettings();
        SyncSchedulerOptions options = BindOptions<SyncSchedulerOptions>(
            config,
            SyncSchedulerOptions.SectionName);
        options.MaxConcurrency.Should().BeInRange(
            SyncSchedulerOptions.MinConcurrency,
            SyncSchedulerOptions.DefaultMaxConcurrencyCap);
    }

    private static T BindOptions<T>(IConfiguration config, string sectionName)
        where T : class, new()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddOptions<T>().Bind(config.GetSection(sectionName));
        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<T>>().Value;
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
