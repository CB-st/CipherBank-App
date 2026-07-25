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
    private const string DefaultAppiumUri = "http://localhost:4723";
    private const int ImplicitWaitSeconds = 10;
    private const int ServerHealthCheckTimeoutSeconds = 3;

    /// <summary>Driver → platform factory map so adding a platform never grows an if/else chain.</summary>
    private static readonly Dictionary<string, Func<Uri, AppiumDriver>> DriverFactories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["android"] = BuildAndroidDriver,
            ["ios"] = BuildIosDriver,
        };

    public AppiumDriver Driver { get; }
    public StoryJournal Journal { get; }

    private AppiumFixture(AppiumDriver driver, StoryJournal journal)
    {
        Driver = driver;
        Journal = journal;
    }

    /// <summary>True when the run opted into device execution via E2E_RUN=1.</summary>
    public static bool IsEnabled =>
        string.Equals(Environment.GetEnvironmentVariable("E2E_RUN"), "1", StringComparison.Ordinal);

    private static string AppiumServerUri =>
        Environment.GetEnvironmentVariable("APPIUM_SERVER_URL") ?? DefaultAppiumUri;

    /// <summary>
    /// Builds the Appium session for this run. Returns null when E2E_RUN is unset (callers Skip);
    /// throws when E2E_RUN=1 but Appium/APK/platform prerequisites are missing — no silent soft-pass.
    /// Use: High (test collection fixture setup). Scope: process-wide E2E session.
    /// </summary>
    public static AppiumFixture? CreateOrThrow()
    {
        if (!IsEnabled)
        {
            return null;
        }

        string platform = Environment.GetEnvironmentVariable("TEST_PLATFORM") ?? "android";
        if (!DriverFactories.TryGetValue(platform, out var buildDriver))
        {
            throw new InvalidOperationException(
                $"E2E_RUN=1 but TEST_PLATFORM='{platform}' has no driver factory (expected 'android' or 'ios').");
        }

        EnsureAppiumServerReachable();

        var driver = buildDriver(new Uri(AppiumServerUri));
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(ImplicitWaitSeconds);
        return new AppiumFixture(driver, new StoryJournal());
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
            var response = client.GetAsync($"{AppiumServerUri}/status").GetAwaiter().GetResult();
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
    private static AppiumDriver BuildAndroidDriver(Uri serverUri)
    {
        string apkPath = Environment.GetEnvironmentVariable("ANDROID_APK_PATH")
            ?? throw new InvalidOperationException(
                "E2E_RUN=1 with TEST_PLATFORM=android requires ANDROID_APK_PATH pointing at a built debug APK.");
        if (!File.Exists(apkPath))
        {
            throw new FileNotFoundException($"ANDROID_APK_PATH does not exist: {apkPath}", apkPath);
        }

        var options = new AppiumOptions
        {
            PlatformName = "Android",
            AutomationName = "UiAutomator2",
            App = apkPath,
        };
        options.AddAdditionalAppiumOption("deviceName", Environment.GetEnvironmentVariable("ANDROID_DEVICE") ?? "Android Emulator");
        return new AndroidDriver(serverUri, options);
    }

    /// <summary>
    /// Builds the iOS (XCUITest) driver from IOS_APP_PATH; throws if the env var or bundle is missing.
    /// Use: Low (iOS runs). Scope: fixture bootstrap.
    /// </summary>
    private static AppiumDriver BuildIosDriver(Uri serverUri)
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
        };
        options.AddAdditionalAppiumOption("deviceName", Environment.GetEnvironmentVariable("IOS_DEVICE") ?? "iPhone 15");
        options.AddAdditionalAppiumOption("platformVersion", Environment.GetEnvironmentVariable("IOS_VERSION") ?? "17.0");
        return new IOSDriver(serverUri, options);
    }

    /// <summary>Quits and disposes the owned Appium session. Use: High (end of collection). Scope: this fixture.</summary>
    public void Dispose()
    {
        Driver.Quit();
        Driver.Dispose();
    }
}
