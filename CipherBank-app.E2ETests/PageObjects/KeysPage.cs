// <copyright file="KeysPage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Recovery phrase screen (CB-ACCOUNT-001 backup step).</summary>
public class KeysPage : BasePage
{
    private static readonly By PageRoot = By.Id("KeysPage");
    private static readonly By MnemonicLabel = By.Id("KeysMnemonicLabel");
    private static readonly By ContinueButton = By.Id("KeysContinueButton");

    /// <summary>
    /// Initializes KeysPage with its Appium/test collaborators. Use: High. Scope: one KeysPage instance.
    /// </summary>
    public KeysPage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Waits until the KeysPage anchor control is visible. Use: High. Scope: KeysPage.
    /// </summary>
    public override void WaitForPageLoad() => WaitForElement(ContinueButton);

    /// <summary>
    /// Reads mnemonic from KeysPage. Use: High. Scope: KeysPage.
    /// </summary>
    public string GetMnemonic() => GetElementText(MnemonicLabel);

    /// <summary>
    /// Advances from KeysPage to the next story screen. Use: High. Scope: KeysPage.
    /// </summary>
    public BackupQuizPage Continue()
    {
        ClickElement(ContinueButton);
        return new BackupQuizPage(Driver);
    }

    /// <summary>
    /// Reports whether the KeysPage anchor control is visible. Use: High. Scope: KeysPage.
    /// </summary>
    public bool IsLoaded() => IsElementDisplayed(PageRoot) || IsElementDisplayed(ContinueButton);
}
