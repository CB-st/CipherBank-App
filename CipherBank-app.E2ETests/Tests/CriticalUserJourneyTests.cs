// <copyright file="CriticalUserJourneyTests.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using CipherBank_app.E2ETests.PageObjects;
using FluentAssertions;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;
using OpenQA.Selenium.Appium.iOS;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// End-to-end tests for critical user journeys in the CipherBank app.
/// These tests cover the most important user flows that must work correctly.
/// </summary>
[Collection("E2E Tests")]
public class CriticalUserJourneyTests : IDisposable
{
    private readonly AppiumDriver _driver;
    private readonly string _testUsername = "testuser";
    private readonly string _testPassword = "password123";

    public CriticalUserJourneyTests()
    {
        // Configure Appium driver based on target platform
        // This setup assumes Appium server is running locally
        var platform = Environment.GetEnvironmentVariable("TEST_PLATFORM") ?? "android";

        if (platform.Equals("ios", StringComparison.OrdinalIgnoreCase))
        {
            var options = new AppiumOptions
            {
                PlatformName = "iOS",
                AutomationName = "XCUITest",
                App = Environment.GetEnvironmentVariable("IOS_APP_PATH") ?? "/path/to/CipherBank.app",
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
                App = Environment.GetEnvironmentVariable("ANDROID_APK_PATH") ?? "/path/to/CipherBank.apk",
            };
            options.AddAdditionalAppiumOption("deviceName", Environment.GetEnvironmentVariable("ANDROID_DEVICE") ?? "Android Emulator");

            _driver = new AndroidDriver(new Uri("http://localhost:4723"), options);
        }

        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// Tests the complete login flow with valid credentials.
    /// </summary>
    [Fact]
    public void LoginFlow_WithValidCredentials_ShouldShowDashboard()
    {
        // Arrange
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();

        // Act
        var dashboardPage = loginPage.LoginAs(_testUsername, _testPassword);
        dashboardPage.WaitForPageLoad();

        // Assert
        dashboardPage.IsLoggedIn().Should().BeTrue();
        dashboardPage.GetWelcomeMessage().Should().Contain(_testUsername);
    }

    /// <summary>
    /// Tests that login fails with invalid credentials.
    /// </summary>
    [Fact]
    public void LoginFlow_WithInvalidCredentials_ShouldShowError()
    {
        // Arrange
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();

        // Act
        loginPage.EnterUsername("invaliduser");
        loginPage.EnterPassword("wrongpassword");
        loginPage.ClickLogin();

        // Assert
        loginPage.IsErrorDisplayed().Should().BeTrue();
        loginPage.GetErrorMessage().Should().Contain("Invalid");
    }

    /// <summary>
    /// Tests the complete purchase flow: login -> navigate to purchase -> complete purchase.
    /// </summary>
    [Fact]
    public void PurchaseFlow_CompletePurchase_ShouldSucceed()
    {
        // Arrange - Login first
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();
        var dashboardPage = loginPage.LoginAs(_testUsername, _testPassword);
        dashboardPage.WaitForPageLoad();

        // Act - Navigate to purchase and complete
        var purchasePage = dashboardPage.GoToPurchase();
        purchasePage.WaitForPageLoad();
        purchasePage.CompletePurchase("BTC", 100.00m);

        // Assert
        purchasePage.IsPurchaseSuccessful().Should().BeTrue();
        purchasePage.GetSuccessMessage().Should().Contain("BTC");
    }

    /// <summary>
    /// Tests the complete send transaction flow.
    /// </summary>
    [Fact]
    public void SendFlow_CompleteSend_ShouldSucceed()
    {
        // Arrange - Login first
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();
        var dashboardPage = loginPage.LoginAs(_testUsername, _testPassword);
        dashboardPage.WaitForPageLoad();

        // Act - Navigate to wallet and send
        var walletPage = dashboardPage.GoToWallet();
        walletPage.WaitForPageLoad();

        var recipientAddress = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa"; // Example address
        walletPage.SendCrypto(recipientAddress, 0.001m);

        // Assert - Verify transaction is in history
        walletPage.HasTransactionHistory().Should().BeTrue();
    }

    /// <summary>
    /// Tests the complete logout flow.
    /// </summary>
    [Fact]
    public void LogoutFlow_ShouldReturnToLogin()
    {
        // Arrange - Login first
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();
        var dashboardPage = loginPage.LoginAs(_testUsername, _testPassword);
        dashboardPage.WaitForPageLoad();

        // Act
        var resultLoginPage = dashboardPage.Logout();
        resultLoginPage.WaitForPageLoad();

        // Assert - Should be back at login
        resultLoginPage.IsElementDisplayed(OpenQA.Selenium.By.Id("LoginButton")).Should().BeTrue();
    }

    /// <summary>
    /// Tests the complete critical path: Login -> Purchase -> View in Wallet -> Logout.
    /// </summary>
    [Fact]
    public void CriticalPath_LoginPurchaseLogout_ShouldComplete()
    {
        // Step 1: Login
        var loginPage = new LoginPage(_driver);
        loginPage.WaitForPageLoad();
        var dashboardPage = loginPage.LoginAs(_testUsername, _testPassword);
        dashboardPage.WaitForPageLoad();
        dashboardPage.IsLoggedIn().Should().BeTrue("User should be logged in");

        // Step 2: Make a purchase
        var purchasePage = dashboardPage.GoToPurchase();
        purchasePage.WaitForPageLoad();
        purchasePage.CompletePurchase("ETH", 50.00m);
        purchasePage.IsPurchaseSuccessful().Should().BeTrue("Purchase should succeed");

        // Step 3: Verify in wallet
        dashboardPage = purchasePage.GoBack();
        var walletPage = dashboardPage.GoToWallet();
        walletPage.WaitForPageLoad();
        walletPage.HasTransactionHistory().Should().BeTrue("Transaction should appear in history");

        // Step 4: Logout
        dashboardPage = walletPage.GoBack();
        var resultLoginPage = dashboardPage.Logout();
        resultLoginPage.WaitForPageLoad();

        // Final assertion
        resultLoginPage.IsElementDisplayed(OpenQA.Selenium.By.Id("LoginButton")).Should().BeTrue("Should be back at login");
    }

    public void Dispose()
    {
        _driver?.Quit();
        _driver?.Dispose();
    }
}
