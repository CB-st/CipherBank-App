// <copyright file="NetworkOptions.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.Configuration;

/// <summary>Environment-keyed CipherBank network endpoints (config/network/endpoints.json).</summary>
public sealed class NetworkOptions
{
    public static string SectionName { get; } = "Network";

    /// <summary>Compile-time fallback when DI options are unavailable (unit / design-time).</summary>
    public static NetworkOptions Default { get; } = CreateDefault();

    public string DefaultEnvironment { get; set; } = "Sandbox";

    public Dictionary<string, NetworkEnvironmentOptions> Environments { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Resolves endpoints for an environment name (falls back to <see cref="DefaultEnvironment"/>).
    /// Use: High (SettingsService / HttpClient base addresses). Scope: NetworkOptions.
    /// </summary>
    public NetworkEnvironmentOptions Resolve(string? environment)
    {
        string key = string.IsNullOrWhiteSpace(environment) ? DefaultEnvironment : environment.Trim();
        if (Environments.TryGetValue(key, out NetworkEnvironmentOptions? endpoints) && endpoints is not null)
        {
            return endpoints;
        }

        if (Environments.TryGetValue(DefaultEnvironment, out NetworkEnvironmentOptions? fallback) && fallback is not null)
        {
            return fallback;
        }

        return new NetworkEnvironmentOptions();
    }

    private static NetworkOptions CreateDefault()
    {
        NetworkOptions options = new()
        {
            DefaultEnvironment = "Sandbox",
        };

        // Compile-time defaults mirror config/network/endpoints.json for design-time / unit hosts.
        options.Environments["Production"] = new NetworkEnvironmentOptions
        {
            ApiBase = "https://api.cipherbank.money",
            PublicApiBase = "https://api.cipherbank.money/",
            StreamEndpoint = "wss://api.cipherbank.money/v1/stream", // NOSONAR S1075 — documented default endpoint
        };
        options.Environments["Sandbox"] = new NetworkEnvironmentOptions
        {
            ApiBase = "https://api.sandbox.cipherbank.money",
            PublicApiBase = "https://api.cipherbank.money/",
            StreamEndpoint = "wss://api.sandbox.cipherbank.money/v1/stream", // NOSONAR S1075 — documented default endpoint
        };
        options.Environments["Development"] = new NetworkEnvironmentOptions
        {
            ApiBase = "https://api.dev.cipherbank.money",
            PublicApiBase = "https://api.cipherbank.money/",
            StreamEndpoint = "wss://api.dev.cipherbank.money/v1/stream", // NOSONAR S1075 — documented default endpoint
        };
        options.Environments["Local"] = new NetworkEnvironmentOptions
        {
            ApiBase = "http://localhost:5000",
            PublicApiBase = "http://localhost:5000/",
            StreamEndpoint = "ws://localhost:5000/v1/stream", // NOSONAR S1075 — documented default endpoint
        };
        return options;
    }
}
