// <copyright file="CipherBankDefaultsConfiguration.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace CipherBank_app.Configuration;

/// <summary>Loads repository-owned default configuration embedded in Core.</summary>
public static class CipherBankDefaultsConfiguration
{
    private const string BaseResourceName = "CipherBank_app.Config.appsettings.json";
    private const string WindowsResourceName = "CipherBank_app.Config.appsettings.Windows.json";

    /// <summary>
    /// Maps compile-time host facts to the repository overlay order.
    /// Use: High (MAUI startup). Scope: process configuration.
    /// </summary>
    /// <returns>A configuration root owned by the caller.</returns>
    public static IConfigurationRoot BuildForHost(bool isDevelopment, bool isWindows)
        => Build(isDevelopment ? "Development" : "Production", isWindows);

    /// <summary>Builds repository defaults without optional overlays.</summary>
    /// <returns>A configuration root owned by the caller.</returns>
    public static IConfigurationRoot Build() => Build(null, false);

    /// <summary>Builds repository defaults with one optional environment overlay.</summary>
    /// <returns>A configuration root owned by the caller.</returns>
    public static IConfigurationRoot Build(string? environment) => Build(environment, false);

    /// <summary>Builds repository defaults with the optional Windows overlay.</summary>
    /// <returns>A configuration root owned by the caller.</returns>
    public static IConfigurationRoot Build(bool windowsOverlay) => Build(null, windowsOverlay);

    /// <summary>
    /// Builds base defaults and then applies optional environment and Windows overlays.
    /// Use: High (host startup and composition tests). Scope: process configuration.
    /// </summary>
    /// <returns>A configuration root owned by the caller.</returns>
    public static IConfigurationRoot Build(
        string? environment,
        bool windowsOverlay)
    {
        Assembly assembly = typeof(CipherBankDefaultsConfiguration).Assembly;
        ConfigurationBuilder builder = new ConfigurationBuilder();
        builder.AddJsonStream(OpenRequiredResource(assembly, BaseResourceName));
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
        if (stream is not null)
        {
            builder.AddJsonStream(stream);
        }
    }
}
