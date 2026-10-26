// <copyright file="DashboardPage.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// Page object for the Dashboard page.
/// Provides methods to interact with the main dashboard and navigate to other pages.
/// </summary>
public class DashboardPage : BasePage
{
    // Element locators
    private static readonly By _welcomeLabel = By.Id("WelcomeLabel");
    private static readonly By _totalBalanceLabel = By.Id("TotalBalanceLabel");
    private static readonly By _walletButton = By.Id("WalletButton");
    private static readonly By _purchaseButton = By.Id("PurchaseButton");
    private static readonly By _settingsButton = By.Id("SettingsButton");
    private static readonly By _logoutButton = By.Id("LogoutButton");
    private static readonly By _refreshButton = By.Id("RefreshButton");
    private static readonly By _transactionsList = By.Id("RecentTransactionsList");

    public DashboardPage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Gets the welcome message text.
    /// </summary>
    public string GetWelcomeMessage()
    {
        return GetElementText(_welcomeLabel);
    }

    /// <summary>
    /// Gets the total balance displayed.
    /// </summary>
    public string GetTotalBalance()
    {
        return GetElementText(_totalBalanceLabel);
    }

    /// <summary>
    /// Navigates to the Wallet page.
    /// </summary>
    public WalletPage GoToWallet()
    {
        ClickElement(_walletButton);
        return new WalletPage(Driver);
    }

    /// <summary>
    /// Navigates to the Purchase page.
    /// </summary>
    public PurchasePage GoToPurchase()
    {
        ClickElement(_purchaseButton);
        return new PurchasePage(Driver);
    }

    /// <summary>
    /// Navigates to the Settings page.
    /// </summary>
    public void GoToSettings()
    {
        ClickElement(_settingsButton);
    }

    /// <summary>
    /// Logs out and returns to the Login page.
    /// </summary>
    public LoginPage Logout()
    {
        ClickElement(_logoutButton);
        return new LoginPage(Driver);
    }

    /// <summary>
    /// Refreshes the dashboard data.
    /// </summary>
    public DashboardPage Refresh()
    {
        ClickElement(_refreshButton);
        return this;
    }

    /// <summary>
    /// Checks if the user is logged in by verifying dashboard elements.
    /// </summary>
    public bool IsLoggedIn()
    {
        return IsElementDisplayed(_welcomeLabel) && IsElementDisplayed(_totalBalanceLabel);
    }

    /// <summary>
    /// Checks if recent transactions are displayed.
    /// </summary>
    public bool HasRecentTransactions()
    {
        return IsElementDisplayed(_transactionsList);
    }

    public override void WaitForPageLoad()
    {
        WaitForElement(_welcomeLabel);
        WaitForElement(_totalBalanceLabel);
    }
}
