// <copyright file="DeviceDiagnostics.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Writes on-device evidence to disk when a story cannot find what it expects, so a failure ships with the
/// actual screen hierarchy instead of only a locator name. Owns nothing but the artifact directory.
/// Use: Low (only on unexpected-screen paths). Scope: process artifacts.
/// </summary>
public static class DeviceDiagnostics
{
    /// <summary>
    /// Dumps the current Appium page source under <c>artifacts/e2e-diagnostics</c> and returns the file path
    /// (or a short reason string when the dump itself fails — diagnostics must never mask the real error).
    /// Use: Low (per unexpected screen). Scope: process artifacts.
    /// </summary>
    public static string CapturePageSource(AppiumDriver driver, string label)
    {
        try
        {
            string dir = RepoPaths.ResolveFromRoot(
                Environment.GetEnvironmentVariable("E2E_DIAGNOSTICS_DIR") ?? "artifacts/e2e-diagnostics");
            Directory.CreateDirectory(dir);
            string path = Path.Combine(dir, $"{label}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}.xml");
            File.WriteAllText(path, driver.PageSource);
            return path;
        }
        catch (Exception ex)
        {
            return $"(page source unavailable: {ex.Message})";
        }
    }
}
