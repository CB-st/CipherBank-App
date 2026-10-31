// <copyright file="ConvertPage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Convert tab.</summary>
public class ConvertPage : BasePage
{
    private static readonly By FromPicker = By.Id("ConvertFromPicker");
    private static readonly By ToPicker = By.Id("ConvertToPicker");
    private static readonly By AmountEntry = By.Id("ConvertAmountEntry");
    private static readonly By LockQuoteButton = By.Id("ConvertLockQuoteButton");
    private static readonly By ConvertSubmitButton = By.Id("ConvertSubmitButton");

    /// <summary>
    /// Initializes ConvertPage with its Appium/test collaborators. Use: High. Scope: one ConvertPage instance.
    /// </summary>
    public ConvertPage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Reports whether asset Pickers are visible on ConvertPage. Use: High. Scope: ConvertPage.
    /// </summary>
    public bool HasAssetPickers()
        => IsElementDisplayed(FromPicker)
           && IsElementDisplayed(ToPicker)
           && IsElementDisplayed(AmountEntry);

    /// <summary>
    /// Locks quote through ConvertPage. Use: High. Scope: ConvertPage.
    /// </summary>
    public ConvertPage LockQuote()
    {
        ClickElement(LockQuoteButton);
        return this;
    }

    /// <summary>
    /// Reports whether convert Enabled holds for ConvertPage. Use: High. Scope: ConvertPage.
    /// </summary>
    public bool IsConvertEnabled()
    {
        try
        {
            return WaitForElement(ConvertSubmitButton).Enabled;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Waits until the ConvertPage anchor control is visible. Use: High. Scope: ConvertPage.
    /// </summary>
    public override void WaitForPageLoad() => WaitForElement(LockQuoteButton);
}
