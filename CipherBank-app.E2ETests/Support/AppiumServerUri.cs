// <copyright file="AppiumServerUri.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Resolves the Appium base URL from environment variables for harness and fixture use.
/// Use: High (every E2E_RUN=1 session). Scope: process env → URI string.
/// </summary>
public static class AppiumServerUri
{
    /// <summary>
    /// Prefer <c>APPIUM_SERVER_URL</c>; otherwise build <c>http://127.0.0.1:{APPIUM_PORT}</c> (default 4723).
    /// Use: High. Scope: AppiumFixture + harness contract tests.
    /// </summary>
    public static string Resolve(
        string? serverUrl = null,
        string? port = null)
    {
        string? explicitUrl = serverUrl ?? Environment.GetEnvironmentVariable("APPIUM_SERVER_URL");
        if (!string.IsNullOrWhiteSpace(explicitUrl))
        {
            return explicitUrl;
        }

        string resolvedPort = port
            ?? Environment.GetEnvironmentVariable("APPIUM_PORT")
            ?? "4723";
        return $"http://127.0.0.1:{resolvedPort}";
    }
}
