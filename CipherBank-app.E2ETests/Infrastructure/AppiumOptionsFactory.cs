// <copyright file="AppiumOptionsFactory.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.Infrastructure;

/// <summary>
/// Builds Appium session options for the E2E journeys.
/// Standard capabilities such as deviceName and platformVersion are set through the typed
/// <see cref="AppiumOptions"/> properties rather than <see cref="AppiumOptions.AddAdditionalAppiumOption(string, object)"/>,
/// so each capability has a single source and is emitted once by <see cref="AppiumOptions.ToCapabilities"/>.
/// </summary>
public static class AppiumOptionsFactory
{
    /// <summary>
    /// Creates iOS (XCUITest) options.
    /// </summary>
    /// <param name="getEnvironmentVariable">Environment lookup used for overrides.</param>
    /// <returns>The configured options.</returns>
    public static AppiumOptions CreateIos(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        return new AppiumOptions
        {
            PlatformName = "iOS",
            AutomationName = "XCUITest",
            App = getEnvironmentVariable("IOS_APP_PATH") ?? "/path/to/CipherBank.app",
            DeviceName = getEnvironmentVariable("IOS_DEVICE") ?? "iPhone 15",
            PlatformVersion = getEnvironmentVariable("IOS_VERSION") ?? "17.0",
        };
    }

    /// <summary>
    /// Creates Android (UiAutomator2) options.
    /// </summary>
    /// <param name="getEnvironmentVariable">Environment lookup used for overrides.</param>
    /// <returns>The configured options.</returns>
    public static AppiumOptions CreateAndroid(Func<string, string?> getEnvironmentVariable)
    {
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        return new AppiumOptions
        {
            PlatformName = "Android",
            AutomationName = "UiAutomator2",
            App = getEnvironmentVariable("ANDROID_APK_PATH") ?? "/path/to/CipherBank.apk",
            DeviceName = getEnvironmentVariable("ANDROID_DEVICE") ?? "Android Emulator",
        };
    }
}
