// <copyright file="SendPage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Send / ACH payee surfaces.</summary>
public class SendPage : BasePage
{
    private static readonly By SavedPayeesPicker = By.Id("SendSavedPayeesPicker");
    private static readonly By RecipientEntry = By.Id("SendRecipientEntry");
    private static readonly By AchPayeeName = By.Id("SendAchPayeeNameEntry");
    private static readonly By AchHolder = By.Id("SendAchHolderEntry");
    private static readonly By AchBank = By.Id("SendAchBankEntry");
    private static readonly By AchRouting = By.Id("SendAchRoutingEntry");
    private static readonly By AchAccount = By.Id("SendAchAccountEntry");
    private static readonly By AchAccountType = By.Id("SendAchAccountTypePicker");
    private static readonly By AchMemo = By.Id("SendAchMemoEntry");
    private static readonly By AchSavePayee = By.Id("SendAchSavePayeeButton");
    private static readonly By AmountEntry = By.Id("SendAmountEntry");
    private static readonly By SpeedPicker = By.Id("SendSpeedPicker");
    private static readonly By SubmitButton = By.Id("SendSubmitButton");

    /// <summary>
    /// Initializes SendPage with its Appium/test collaborators. Use: High. Scope: one SendPage instance.
    /// </summary>
    public SendPage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Reports whether parity Surfaces are visible on SendPage. Use: High. Scope: SendPage.
    /// </summary>
    public bool HasParitySurfaces()
        => IsElementDisplayed(AchPayeeName)
           && IsElementDisplayed(AchHolder)
           && IsElementDisplayed(AchBank)
           && IsElementDisplayed(AchRouting)
           && IsElementDisplayed(AchAccount)
           && IsElementDisplayed(AchAccountType)
           && IsElementDisplayed(AchMemo)
           && IsElementDisplayed(AchSavePayee)
           && IsElementDisplayed(AmountEntry)
           && IsElementDisplayed(SpeedPicker)
           && IsElementDisplayed(SubmitButton)
           && IsElementDisplayed(RecipientEntry)
           && IsElementDisplayed(SavedPayeesPicker);

    /// <summary>
    /// Waits until the SendPage anchor control is visible. Use: High. Scope: SendPage.
    /// </summary>
    public override void WaitForPageLoad() => WaitForElement(SubmitButton);
}
