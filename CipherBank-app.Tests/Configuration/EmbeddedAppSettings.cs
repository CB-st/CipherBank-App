// <copyright file="EmbeddedAppSettings.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Tests.Configuration;

/// <summary>Binds Core's embedded appsettings JSONC resources for persist unit tests.</summary>
internal static class EmbeddedAppSettings
{
    internal static PersistenceOptions BindPersistence(string? environment = null)
        => BindOptions<PersistenceOptions>(environment);

    internal static T BindOptions<T>(string? environment = null)
        where T : class
    {
        IConfigurationRoot config = Load(environment);
        ServiceCollection services = new();
        services.AddRequiredOptions(config, Activator.CreateInstance<T>());
        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<T>>().Value;
    }

    internal static IConfigurationRoot Load(string? environment = null)
        => CipherBankDefaultsConfiguration.Build(environment);
}
