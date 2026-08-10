// <copyright file="CipherBankDefaultsConfiguration.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace CipherBank_app.Configuration;

/// <summary>Loads repository-owned default configuration embedded in Core.</summary>
public static class CipherBankDefaultsConfiguration
{
    private static readonly string[] ResourceNames =
    [
        "CipherBank_app.Config.security.cryptography.json",
        "CipherBank_app.Config.dispatch.sync-scheduler.json",
        "CipherBank_app.Config.persistence.database.json",
        "CipherBank_app.Config.ui.cora-lines.json",
        "CipherBank_app.Config.ui.carousel.json",
    ];

    /// <summary>Builds the default configuration in deterministic theme order.</summary>
    public static IConfigurationRoot Build()
    {
        var builder = new ConfigurationBuilder();
        var assembly = typeof(CipherBankDefaultsConfiguration).Assembly;
        foreach (var resourceName in ResourceNames)
        {
            builder.AddJsonStream(OpenRequiredResource(assembly, resourceName));
        }

        return builder.Build();
    }

    private static Stream OpenRequiredResource(Assembly assembly, string resourceName)
        => assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded configuration resource '{resourceName}'.");
}
