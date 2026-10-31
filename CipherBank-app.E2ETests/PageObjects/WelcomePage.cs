// <copyright file="WelcomePage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Welcome / create-account entry (CB-ACCOUNT-001 / US-ONB-01).</summary>
public class WelcomePage : BasePage
{
    private static readonly By CreateButton = By.Id("WelcomeCreateWalletButton");
    private static readonly By ReturningButton = By.Id("WelcomeReturningButton");
    private static readonly By RestoreBackupButton = By.Id("WelcomeRestoreFromBackupButton");

    /// <summary>
    /// Initializes WelcomePage with its Appium/test collaborators. Use: High. Scope: one WelcomePage instance.
    /// </summary>
    public WelcomePage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Waits until the WelcomePage anchor control is visible. Use: High. Scope: WelcomePage.
    /// </summary>
    public override void WaitForPageLoad() => WaitForElement(CreateButton);

    /// <summary>
    /// Reports whether the WelcomePage anchor control is visible. Use: High. Scope: WelcomePage.
    /// </summary>
    public bool IsLoaded() => IsElementDisplayed(CreateButton);

    /// <summary>
    /// Starts create Account from WelcomePage. Use: High. Scope: WelcomePage.
    /// </summary>
    public KeysPage StartCreateAccount()
    {
        ClickElement(CreateButton);
        return new KeysPage(Driver);
    }

    /// <summary>
    /// Starts returning from WelcomePage. Use: High. Scope: WelcomePage.
    /// </summary>
    public KeysPage StartReturning()
    {
        ClickElement(ReturningButton);
        return new KeysPage(Driver);
    }

    /// <summary>
    /// Enters the restore-from-backup flow from a wallet-less install (CB-ACCOUNT-002 step "open").
    /// Use: Medium (recovery stories). Scope: this page object.
    /// </summary>
    public RestoreBackupPage OpenRestoreFromBackup()
    {
        ClickElement(RestoreBackupButton);
        return new RestoreBackupPage(Driver);
    }
}
