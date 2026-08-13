// <copyright file="CipherBankDefaultsConfiguration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace CipherBank_app.Configuration;

/// <summary>Loads repository-owned default configuration embedded in Core.</summary>
public static class CipherBankDefaultsConfiguration
{
    private static readonly string[] ResourceNames =
    [
        "CipherBank_app.Config.appsettings.json",
    ];

    /// <summary>Builds the default configuration in deterministic theme order.</summary>
    public static IConfigurationRoot Build()
    {
        ConfigurationBuilder builder = new ConfigurationBuilder();
        Assembly assembly = typeof(CipherBankDefaultsConfiguration).Assembly;
        foreach (string resourceName in ResourceNames)
        {
            builder.AddJsonStream(OpenRequiredResource(assembly, resourceName));
        }

        return builder.Build();
    }

    private static Stream OpenRequiredResource(Assembly assembly, string resourceName)
        => assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded configuration resource '{resourceName}'.");
}
