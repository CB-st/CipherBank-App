// <copyright file="EmbeddedAppSettings.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CipherBank_app.Tests.Configuration;

/// <summary>Binds Core's themed embedded configuration for persist unit tests.</summary>
internal static class EmbeddedAppSettings
{
    internal static PersistenceOptions BindPersistence()
        => BindOptions<PersistenceOptions>(PersistenceOptions.SectionName);

    internal static T BindOptions<T>(string sectionName)
        where T : class, new()
    {
        IConfigurationRoot config = CipherBankDefaultsConfiguration.Build();
        ServiceCollection services = new ServiceCollection();
        services.AddOptions<T>().Bind(config.GetSection(sectionName));
        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IOptions<T>>().Value;
    }
}
