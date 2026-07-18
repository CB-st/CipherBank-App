using CipherBank_app.E2ETests.PageObjects;
using FluentAssertions;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Legacy login/dashboard E2E (pre-Cora Shell). Prefer <see cref="CoraShellSmokeTests"/>.
/// </summary>
[Collection("E2E Tests")]
public class CriticalUserJourneyTests : IDisposable
{
    private readonly AppiumDriver _driver;
    private readonly string _testUsername = "testuser";
    private readonly string _testPassword = "password123";

    public CriticalUserJourneyTests()
    {
        var platform = Environment.GetEnvironmentVariable("TEST_PLATFORM") ?? "android";

        if (platform.Equals("ios", StringComparison.OrdinalIgnoreCase))
        {
            var options = new AppiumOptions
            {
                PlatformName = "iOS",
                AutomationName = "XCUITest",
                App = Environment.GetEnvironmentVariable("IOS_APP_PATH") ?? "/path/to/CipherBank.app"
            };
            options.AddAdditionalAppiumOption("deviceName", Environment.GetEnvironmentVariable("IOS_DEVICE") ?? "iPhone 15");
            options.AddAdditionalAppiumOption("platformVersion", Environment.GetEnvironmentVariable("IOS_VERSION") ?? "17.0");
            _driver = new IOSDriver(new Uri("http://localhost:4723"), options);
        }
        else
        {
            var options = new AppiumOptions
            {
                PlatformName = "Android",
                AutomationName = "UiAutomator2",
                App = Environment.GetEnvironmentVariable("ANDROID_APK_PATH") ?? "/path/to/CipherBank.apk"
            };
            options.AddAdditionalAppiumOption("deviceName", Environment.GetEnvironmentVariable("ANDROID_DEVICE") ?? "Android Emulator");
            _driver = new AndroidDriver(new Uri("http://localhost:4723"), options);
        }

        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    [Fact(Skip = "Superseded by Cora Shell (Unlock→Home). See CoraShellSmokeTests.")]
    public void LoginFlow_WithValidCredentials_ShouldShowDashboard()
    {
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();
        var dashboardPage = loginPage.LoginAs(_testUsername, _testPassword);
        dashboardPage.WaitForPageLoad();
        dashboardPage.IsLoggedIn().Should().BeTrue();
        dashboardPage.GetWelcomeMessage().Should().Contain(_testUsername);
    }

    [Fact(Skip = "Superseded by Cora Shell (Unlock→Home). See CoraShellSmokeTests.")]
    public void LoginFlow_WithInvalidCredentials_ShouldShowError()
    {
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();
        loginPage.EnterUsername("invaliduser");
        loginPage.EnterPassword("wrongpassword");
        loginPage.ClickLogin();
        loginPage.IsErrorDisplayed().Should().BeTrue();
        loginPage.GetErrorMessage().Should().Contain("Invalid");
    }

    [Fact(Skip = "Superseded by Cora Shell Convert/Pay flows. See CoraShellSmokeTests.")]
    public void PurchaseFlow_CompletePurchase_ShouldSucceed()
    {
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();
        var dashboardPage = loginPage.LoginAs(_testUsername, _testPassword);
        dashboardPage.WaitForPageLoad();
        var purchasePage = dashboardPage.GoToPurchase();
        purchasePage.WaitForPageLoad();
        purchasePage.CompletePurchase("BTC", 100.00m);
        purchasePage.IsPurchaseSuccessful().Should().BeTrue();
        purchasePage.GetSuccessMessage().Should().Contain("BTC");
    }

    [Fact(Skip = "Superseded by Cora Shell Send tab. See CoraShellSmokeTests.")]
    public void SendFlow_CompleteSend_ShouldSucceed()
    {
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();
        var dashboardPage = loginPage.LoginAs(_testUsername, _testPassword);
        dashboardPage.WaitForPageLoad();
        var walletPage = dashboardPage.GoToWallet();
        walletPage.WaitForPageLoad();
        walletPage.SendCrypto("1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa", 0.001m);
        walletPage.HasTransactionHistory().Should().BeTrue();
    }

    [Fact(Skip = "Superseded by Cora Shell lock/unlock. See CoraShellSmokeTests.")]
    public void LogoutFlow_ShouldReturnToLogin()
    {
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();
        var dashboardPage = loginPage.LoginAs(_testUsername, _testPassword);
        dashboardPage.WaitForPageLoad();
        var resultLoginPage = dashboardPage.Logout();
        resultLoginPage.WaitForPageLoad();
        resultLoginPage.IsElementDisplayed(OpenQA.Selenium.By.Id("LoginButton")).Should().BeTrue();
    }

    [Fact(Skip = "Superseded by CoraShellSmokeTests.")]
    public void CriticalPath_LoginPurchaseLogout_ShouldComplete()
    {
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();
        var dashboardPage = loginPage.LoginAs(_testUsername, _testPassword);
        dashboardPage.WaitForPageLoad();
        dashboardPage.IsLoggedIn().Should().BeTrue();
        var purchasePage = dashboardPage.GoToPurchase();
        purchasePage.WaitForPageLoad();
        purchasePage.CompletePurchase("ETH", 50.00m);
        purchasePage.IsPurchaseSuccessful().Should().BeTrue();
        dashboardPage = purchasePage.GoBack();
        var walletPage = dashboardPage.GoToWallet();
        walletPage.WaitForPageLoad();
        walletPage.HasTransactionHistory().Should().BeTrue();
        dashboardPage = walletPage.GoBack();
        var resultLoginPage = dashboardPage.Logout();
        resultLoginPage.WaitForPageLoad();
        resultLoginPage.IsElementDisplayed(OpenQA.Selenium.By.Id("LoginButton")).Should().BeTrue();
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}
