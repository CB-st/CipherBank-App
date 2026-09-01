// <copyright file="EmbeddedAppSettings.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Reflection;
using CipherBank_app.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Tests.Configuration;

/// <summary>Binds Core's embedded appsettings.json for persist unit tests.</summary>
internal static class EmbeddedAppSettings
{
    internal static PersistenceOptions BindPersistence()
        => BindOptions<PersistenceOptions>(PersistenceOptions.SectionName);

    internal static T BindOptions<T>(string sectionName)
        where T : class, new()
    {
        IConfigurationRoot config = Load();
        ServiceCollection services = new ServiceCollection();
        services.AddOptions<T>().Bind(config.GetSection(sectionName));
        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<T>>().Value;
    }

    internal static IConfigurationRoot Load()
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
