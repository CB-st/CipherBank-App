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
    private static readonly By WelcomeLabel = By.Id("WelcomeLabel");
    private static readonly By TotalBalanceLabel = By.Id("TotalBalanceLabel");
    private static readonly By WalletButton = By.Id("WalletButton");
    private static readonly By PurchaseButton = By.Id("PurchaseButton");
    private static readonly By SettingsButton = By.Id("SettingsButton");
    private static readonly By LogoutButton = By.Id("LogoutButton");
    private static readonly By RefreshButton = By.Id("RefreshButton");
    private static readonly By TransactionsList = By.Id("RecentTransactionsList");

    public DashboardPage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Gets the welcome message text.
    /// </summary>
    public string GetWelcomeMessage()
    {
        return GetElementText(WelcomeLabel);
    }

    /// <summary>
    /// Gets the total balance displayed.
    /// </summary>
    public string GetTotalBalance()
    {
        return GetElementText(TotalBalanceLabel);
    }

    /// <summary>
    /// Navigates to the Wallet page.
    /// </summary>
    public WalletPage GoToWallet()
    {
        ClickElement(WalletButton);
        return new WalletPage(Driver);
    }

    /// <summary>
    /// Navigates to the Purchase page.
    /// </summary>
    public PurchasePage GoToPurchase()
    {
        ClickElement(PurchaseButton);
        return new PurchasePage(Driver);
    }

    /// <summary>
    /// Navigates to the Settings page.
    /// </summary>
    public void GoToSettings()
    {
        ClickElement(SettingsButton);
    }

    /// <summary>
    /// Logs out and returns to the Login page.
    /// </summary>
    public LoginPage Logout()
    {
        ClickElement(LogoutButton);
        return new LoginPage(Driver);
    }

    /// <summary>
    /// Refreshes the dashboard data.
    /// </summary>
    public DashboardPage Refresh()
    {
        ClickElement(RefreshButton);
        return this;
    }

    /// <summary>
    /// Checks if the user is logged in by verifying dashboard elements.
    /// </summary>
    public bool IsLoggedIn()
    {
        return IsElementDisplayed(WelcomeLabel) && IsElementDisplayed(TotalBalanceLabel);
    }

    /// <summary>
    /// Checks if recent transactions are displayed.
    /// </summary>
    public bool HasRecentTransactions()
    {
        return IsElementDisplayed(TransactionsList);
    }

    public override void WaitForPageLoad()
    {
        WaitForElement(WelcomeLabel);
        WaitForElement(TotalBalanceLabel);
    }
}
