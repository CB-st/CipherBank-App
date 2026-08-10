// <copyright file="EmulatorReset.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Wipes MAUI app storage on the connected device/emulator via adb, giving a deterministic Fresh state.
/// Use: Medium (once per Fresh-profile story). Scope: single adb invocation against the attached device.
/// </summary>
public static class EmulatorReset
{
    private const string DefaultPackage = "com.companyname.cipherbankapp";

    /// <summary>
    /// Resolves the MAUI application id from CB_MAUI_PACKAGE, falling back to the shipping default.
    /// Use: High. Scope: any caller needing the package id (reset, relaunch).
    /// </summary>
    public static string ResolvePackageId() =>
        Environment.GetEnvironmentVariable("CB_MAUI_PACKAGE") ?? DefaultPackage;

    /// <summary>
    /// Runs `adb shell pm clear &lt;package&gt;`, wiping PIN/mnemonic/wallet state for the MAUI app.
    /// Throws if adb cannot be run or does not report success, so a broken harness fails fast rather than
    /// silently continuing against stale device state. Use: Medium. Scope: Fresh profile setup.
    /// </summary>
    public static void ClearAppData(string? package = null)
    {
        string pkg = package ?? ResolvePackageId();
        string output = Adb.Shell($"pm clear {pkg}");
        if (!output.Contains("Success", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"adb shell pm clear {pkg} did not report Success (is a device/emulator attached?). Output: {output}");
        }
    }
}
