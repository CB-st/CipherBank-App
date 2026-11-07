// <copyright file="SetPinPage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// Set PIN / seal vault page for onboarding (CB-ACCOUNT-001 complete; US-ONB-04 mismatch).
/// Assumes the app has already navigated past Keys + BackupQuiz and the SetPin XAML is the foreground page.
/// Use: High (account wave). Scope: SetPin AutomationIds on Android/iOS.
/// </summary>
public class SetPinPage : BasePage
{
    private static readonly By PinEntry = By.Id("SetPinEntry");
    private static readonly By ConfirmEntry = By.Id("SetPinConfirmEntry");
    private static readonly By SealButton = By.Id("SetPinSealButton");
    private static readonly By ErrorLabel = By.Id("SetPinErrorLabel");

    /// <summary>
    /// Binds this page object to the shared Appium session.
    /// Use: High (every SetPin interaction). Scope: SetPinPage instance.
    /// </summary>
    public SetPinPage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>
    /// Blocks until the seal control is present (page ready for PIN entry).
    /// Use: High (before Enter/Seal). Scope: SetPinPage load gate.
    /// </summary>
    public override void WaitForPageLoad() => WaitForElement(SealButton);

    /// <summary>
    /// Types the primary PIN into SetPinEntry (clears existing text first via BasePage.EnterText).
    /// Use: High (seal + mismatch paths). Scope: SetPinPage.
    /// </summary>
    public SetPinPage EnterPin(string pin)
    {
        EnterText(PinEntry, pin);
        return this;
    }

    /// <summary>
    /// Types the confirmation PIN into SetPinConfirmEntry.
    /// Use: High (seal + mismatch paths). Scope: SetPinPage.
    /// </summary>
    public SetPinPage EnterConfirm(string pin)
    {
        EnterText(ConfirmEntry, pin);
        return this;
    }

    /// <summary>
    /// Enters matching PIN/confirm, taps Seal, and returns a HomePage (caller should WaitForPageLoad).
    /// Fails the Fact if Seal does not navigate — does not soft-pass.
    /// Use: High (CB-ACCOUNT-001 happy path). Scope: SetPin → Home transition.
    /// </summary>
    public HomePage SealMatching(string pin)
    {
        EnterPin(pin);
        EnterConfirm(pin);
        ClickElement(SealButton);
        return new HomePage(Driver);
    }

    /// <summary>
    /// Enters mismatched PIN/confirm and taps Seal, expecting to remain on SetPin with an error.
    /// Use: High (US-ONB-04 negative path). Scope: SetPinPage.
    /// </summary>
    public SetPinPage SealMismatch(string pin, string confirm)
    {
        EnterPin(pin);
        EnterConfirm(confirm);
        ClickElement(SealButton);
        return this;
    }

    /// <summary>
    /// Confirms the set-PIN error actually surfaced: the label is visible (per its XAML IsVisible
    /// binding) AND carries non-empty text. Guards against a regression that leaves the always-in-tree
    /// label falsely ".Displayed" while ViewModel.Error was never set.
    /// Use: Medium (US-ONB-04 negative-path assertion). Scope: SetPinPage.
    /// </summary>
    public bool IsErrorDisplayed() =>
        IsElementDisplayed(ErrorLabel) && !string.IsNullOrWhiteSpace(GetElementText(ErrorLabel));
}
