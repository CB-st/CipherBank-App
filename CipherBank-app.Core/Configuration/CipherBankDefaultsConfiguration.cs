// <copyright file="CipherBankDefaultsConfiguration.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace CipherBank_app.Configuration;

/// <summary>Loads repository-owned default configuration embedded in Core.</summary>
public static class CipherBankDefaultsConfiguration
{
    private const string WindowsResourceName = "CipherBank_app.Config.appsettings.Windows.json";

    private static readonly string[] RequiredResourceNames =
    [
        "CipherBank_app.Config.security.cryptography.json",
        "CipherBank_app.Config.dispatch.sync-scheduler.json",
        "CipherBank_app.Config.persistence.database.json",
        "CipherBank_app.Config.network.endpoints.json",
        "CipherBank_app.Config.ui.cora-lines.json",
        "CipherBank_app.Config.ui.carousel.json",
    ];

    /// <summary>
    /// Builds the default configuration, then optionally merges environment and Windows overlays.
    /// Use: High. Scope: host and test composition of embedded options.
    /// </summary>
    /// <param name="environment">
    /// Host environment name. When set, merges <c>appsettings.{environment}.json</c> if that
    /// embedded resource exists. Unknown names are skipped so Production does not invent a file.
    /// </param>
    /// <param name="windowsOverlay">
    /// When true, merges the Windows overlay after the environment overlay. Android and other
    /// non-Windows hosts must pass false.
    /// </param>
    /// <returns>
    /// A built configuration root. Caller owns the instance. Missing base resource throws
    /// <see cref="InvalidOperationException"/>; missing overlays are ignored.
    /// </returns>
    public static IConfigurationRoot Build(
        string? environment = null,
        bool windowsOverlay = false)
    {
        ConfigurationBuilder builder = new ConfigurationBuilder();
        Assembly assembly = typeof(CipherBankDefaultsConfiguration).Assembly;
        foreach (string resourceName in RequiredResourceNames)
        {
            builder.AddJsonStream(OpenRequiredResource(assembly, resourceName));
        }

        if (!string.IsNullOrWhiteSpace(environment))
        {
            TryAddOptionalResource(
                builder,
                assembly,
                $"CipherBank_app.Config.appsettings.{environment}.json");
        }

        if (windowsOverlay)
        {
            TryAddOptionalResource(builder, assembly, WindowsResourceName);
        }

        return builder.Build();
    }

    private static Stream OpenRequiredResource(Assembly assembly, string resourceName)
        => assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Missing embedded configuration resource '{resourceName}'.");

    private static void TryAddOptionalResource(
        IConfigurationBuilder builder,
        Assembly assembly,
        string resourceName)
    {
        Stream? stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            return;
        }

        builder.AddJsonStream(stream);
    }
}
