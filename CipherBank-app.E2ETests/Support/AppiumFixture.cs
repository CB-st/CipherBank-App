// <copyright file="AppiumFixture.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.E2ETests.PageObjects;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Owns the single Appium session (driver + journal) for an E2E run.
/// Use: High (once per test collection). Scope: process-wide Appium session.
/// </summary>
public sealed class AppiumFixture : IDisposable
{
    private const int ImplicitWaitSeconds = 10;

    /// <summary>
    /// Preferred construction path is <see cref="CreateOrThrow"/>; the ctor is public only so StyleCop
    /// member order stays consistent (properties before factory + nested bootstrap helpers).
    /// Use: Low (CreateOrThrow). Scope: this fixture.
    /// </summary>
    public AppiumFixture(AppiumDriver driver, StoryJournal journal)
    {
        Driver = driver;
        Journal = journal;
    }

    /// <summary>True when the run opted into device execution via E2E_RUN=1.</summary>
    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("E2E_RUN"), "1", StringComparison.Ordinal);

    /// <summary>Gets driver for AppiumFixture.</summary>
    public AppiumDriver Driver { get; }

    /// <summary>Gets journal for AppiumFixture.</summary>
    public StoryJournal Journal { get; }

    /// <summary>
    /// Builds the Appium session for this run. Returns null when E2E_RUN is unset (callers Skip);
    /// throws when E2E_RUN=1 but Appium/APK/platform prerequisites are missing — no silent soft-pass.
    /// When <c>E2E_DEVICE_PROFILE=sealed</c>, Android uses noReset and this method proves Unlock (or seals
    /// via <see cref="DeviceState.SealedAsync"/> then locks) so smoke does not depend on AccountStories order.
    /// Use: High (test collection fixture setup). Scope: process-wide E2E session.
    /// </summary>
    public static AppiumFixture? CreateOrThrow()
    {
        if (!IsEnabled)
        {
            return null;
        }

        AppiumDriver driver = DriverBootstrap.CreateDriver();
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(ImplicitWaitSeconds);
        AppiumFixture fixture = new AppiumFixture(driver, new StoryJournal());
        if (IsSealedDeviceProfile())
        {
            fixture.EnsureSealedUnlockOrThrow();
        }

        return fixture;
    }

    /// <summary>
    /// True when the harness requested a sealed-wallet Android session (smoke / --all second half).
    /// Use: High (fixture bootstrap). Scope: process-wide E2E session.
    /// </summary>
    public static bool IsSealedDeviceProfile() =>
        string.Equals(
            Environment.GetEnvironmentVariable("E2E_DEVICE_PROFILE"),
            "sealed",
            StringComparison.OrdinalIgnoreCase);

    /// <summary>Quits and disposes the owned Appium session. Use: High (end of collection). Scope: this fixture.</summary>
    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }

    /// <summary>
    /// Leaves the sealed session on Unlock: already Unlock, Profile→Lock from Shell, or Fresh+seal+Lock when
    /// Welcome appeared (wallet missing despite the sealed profile request).
    /// Use: High (sealed profile CreateOrThrow). Scope: this fixture session.
    /// </summary>
    private void EnsureSealedUnlockOrThrow()
    {
        UnlockPage unlock = new UnlockPage(Driver);
        if (unlock.IsLoaded())
        {
            return;
        }

        WelcomePage welcome = new WelcomePage(Driver);
        if (welcome.IsLoaded())
        {
            DeviceState device = new DeviceState(Driver, Journal);
            HomePage home = device.SealedAsync().GetAwaiter().GetResult();
            UnlockPage locked = home.GoToProfileTab().LockApp();
            locked.WaitForPageLoad();
            if (!locked.IsLoaded())
            {
                throw new InvalidOperationException(
                    "E2E_DEVICE_PROFILE=sealed: sealed after Welcome but Profile→Lock did not land on Unlock.");
            }

            return;
        }

        if (TryLockFromShell(out unlock) && unlock.IsLoaded())
        {
            return;
        }

        throw new InvalidOperationException(
            "E2E_DEVICE_PROFILE=sealed requires Unlock (or Welcome→seal); neither Unlock nor a lockable Shell was visible.");
    }

    /// <summary>
    /// Attempts Home → Profile → Lock from an unlocked Shell tab. Use: Medium. Scope: sealed fixture bootstrap.
    /// </summary>
    private bool TryLockFromShell(out UnlockPage unlock)
    {
        unlock = new UnlockPage(Driver);
        try
        {
            HomePage home = new HomePage(Driver);
            home.GoToHomeTab();
            ProfilePage profile = home.GoToProfileTab();
            profile.WaitForPageLoad();
            unlock = profile.LockApp();
            unlock.WaitForPageLoad();
            return unlock.IsLoaded();
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Platform driver factories and Appium /status probe kept nested so the outer type stays public-first.
    /// Use: High (CreateOrThrow). Scope: fixture bootstrap.
    /// </summary>
    private static class DriverBootstrap
    {
        private const int ServerHealthCheckTimeoutSeconds = 3;

        private static readonly Dictionary<string, Func<Uri, AppiumDriver>> DriverFactories =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["android"] = BuildAndroidDriver,
                ["ios"] = BuildIosDriver,
            };

        private static string AppiumServerUri => Support.AppiumServerUri.Resolve();

        /// <summary>
        /// Resolves TEST_PLATFORM, proves Appium is up, and returns a live driver.
        /// Use: High (CreateOrThrow). Scope: fixture bootstrap.
        /// </summary>
        public static AppiumDriver CreateDriver()
        {
            string platform = Environment.GetEnvironmentVariable("TEST_PLATFORM") ?? "android";
            if (!DriverFactories.TryGetValue(platform, out Func<Uri, AppiumDriver>? buildDriver))
            {
                throw new InvalidOperationException(
                    $"E2E_RUN=1 but TEST_PLATFORM='{platform}' has no driver factory (expected 'android' or 'ios').");
            }

            EnsureAppiumServerReachable();
            return buildDriver(new Uri(AppiumServerUri));
        }

        /// <summary>
        /// Pings the Appium server's /status endpoint so a missing/unstarted server fails fast with a clear message
        /// instead of a raw connection-refused exception from the Selenium client.
        /// Use: High (every CreateOrThrow call under E2E_RUN=1). Scope: fixture bootstrap.
        /// </summary>
        private static void EnsureAppiumServerReachable()
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(ServerHealthCheckTimeoutSeconds) };
            try
            {
                HttpResponseMessage response = client.GetAsync($"{AppiumServerUri}/status").GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException(
                        $"E2E_RUN=1 but Appium server at {AppiumServerUri} returned {(int)response.StatusCode}.");
                }
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                throw new InvalidOperationException(
                    $"E2E_RUN=1 but Appium server is unreachable at {AppiumServerUri}. Start Appium before running E2E.",
                    ex);
            }
        }

        /// <summary>
        /// Builds the Android (UiAutomator2) driver from ANDROID_APK_PATH; throws if the env var or file is missing.
        /// Use: Medium (Android runs). Scope: fixture bootstrap.
        /// </summary>
        private static AndroidDriver BuildAndroidDriver(Uri serverUri)
        {
            string apkPath = Environment.GetEnvironmentVariable("ANDROID_APK_PATH")
                ?? throw new InvalidOperationException(
                    "E2E_RUN=1 with TEST_PLATFORM=android requires ANDROID_APK_PATH pointing at a built debug APK.");
            if (!File.Exists(apkPath))
            {
                throw new FileNotFoundException($"ANDROID_APK_PATH does not exist: {apkPath}", apkPath);
            }

            bool sealedProfile = string.Equals(
                Environment.GetEnvironmentVariable("E2E_DEVICE_PROFILE"),
                "sealed",
                StringComparison.OrdinalIgnoreCase);

            var options = new AppiumOptions
            {
                PlatformName = "Android",
                AutomationName = "UiAutomator2",
                DeviceName = Environment.GetEnvironmentVariable("ANDROID_DEVICE") ?? "Android Emulator",
            };

            // Fresh installs need App=apk. Sealed smoke must not wipe custody: omit fullReset, force
            // noReset, still pass App so UiAutomator2 can attach, and rely on ensure-sealed bootstrap.
            options.App = apkPath;
            options.AddAdditionalAppiumOption("noReset", sealedProfile);
            options.AddAdditionalAppiumOption("fullReset", false);
            if (sealedProfile)
            {
                options.AddAdditionalAppiumOption("appPackage", EmulatorReset.ResolvePackageId());
                options.AddAdditionalAppiumOption("dontStopAppOnReset", true);
            }

            return new AndroidDriver(serverUri, options);
        }

        /// <summary>
        /// Builds the iOS (XCUITest) driver from IOS_APP_PATH; throws if the env var or bundle is missing.
        /// Use: Low (iOS runs). Scope: fixture bootstrap.
        /// </summary>
        private static IOSDriver BuildIosDriver(Uri serverUri)
        {
            string appPath = Environment.GetEnvironmentVariable("IOS_APP_PATH")
                ?? throw new InvalidOperationException(
                    "E2E_RUN=1 with TEST_PLATFORM=ios requires IOS_APP_PATH pointing at a built .app bundle.");
            if (!File.Exists(appPath) && !Directory.Exists(appPath))
            {
                throw new FileNotFoundException($"IOS_APP_PATH does not exist: {appPath}", appPath);
            }

            var options = new AppiumOptions
            {
                PlatformName = "iOS",
                AutomationName = "XCUITest",
                App = appPath,
                DeviceName = Environment.GetEnvironmentVariable("IOS_DEVICE") ?? "iPhone 15",
                PlatformVersion = Environment.GetEnvironmentVariable("IOS_VERSION") ?? "17.0",
            };
            return new IOSDriver(serverUri, options);
        }
    }
}
