// <copyright file="ChangePinPage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// Change PIN surface (Profile → Security → Change PIN) used by CB-ACCOUNT-PIN-CHANGE: current PIN plus
/// new/confirm entries, an error label for rejected attempts and a status label for the applied change.
/// </summary>
public class ChangePinPage : BasePage
{
    private static readonly By CurrentEntry = By.Id("ChangePinCurrentEntry");
    private static readonly By NewEntry = By.Id("ChangePinEntry");
    private static readonly By ConfirmEntry = By.Id("ChangePinConfirmEntry");
    private static readonly By SubmitButton = By.Id("ChangePinSubmitButton");
    private static readonly By CancelButton = By.Id("ChangePinCancelButton");
    private static readonly By ErrorLabel = By.Id("ChangePinErrorLabel");
    private static readonly By StatusLabel = By.Id("ChangePinStatusLabel");

    /// <summary>
    /// Initializes ChangePinPage with its Appium/test collaborators. Use: High. Scope: one ChangePinPage instance.
    /// </summary>
    public ChangePinPage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>Whether the Change PIN submit control is on screen. Use: Medium. Scope: this page object.</summary>
    public bool IsLoaded() => IsElementDisplayed(SubmitButton);

    /// <summary>
    /// Blocks until the Update PIN control is on screen, i.e. the page finished navigating in.
    /// Use: High (every Change-PIN story step). Scope: this page object.
    /// </summary>
    public override void WaitForPageLoad() => WaitForElement(SubmitButton);

    /// <summary>
    /// Fills all three PIN fields, taps Update PIN and waits for the page's own verdict, staying here so the
    /// caller can assert the status (success) or error (rejection) label.
    /// Use: High (CB-ACCOUNT-PIN-CHANGE). Scope: this page object.
    /// </summary>
    public ChangePinPage Submit(string currentPin, string newPin, string confirmPin)
    {
        EnterText(CurrentEntry, currentPin);
        EnterText(NewEntry, newPin);
        EnterText(ConfirmEntry, confirmPin);
        ClickElement(SubmitButton);
        return WaitForFeedback();
    }

    /// <summary>
    /// Confirms the applied-change status actually surfaced: visible per its XAML IsVisible binding AND
    /// carrying text, so an always-in-tree label cannot fake a pass.
    /// Use: High (change-PIN success assertion). Scope: this page object.
    /// </summary>
    public bool IsStatusDisplayed()
        => IsElementDisplayed(StatusLabel) && !string.IsNullOrWhiteSpace(GetElementText(StatusLabel));

    /// <summary>
    /// Same visible-and-non-empty check for the rejection path.
    /// Use: Medium (change-PIN negative assertion). Scope: this page object.
    /// </summary>
    public bool IsErrorDisplayed()
        => IsElementDisplayed(ErrorLabel) && !string.IsNullOrWhiteSpace(GetElementText(ErrorLabel));

    /// <summary>Returns to Profile via the page's own Back control. Use: High. Scope: this page object.</summary>
    public ProfilePage BackToProfile()
    {
        ClickElement(CancelButton);
        return new ProfilePage(Driver);
    }

    /// <summary>
    /// Waits until either feedback label becomes visible so an assertion is not racing the async submit
    /// (PIN verification is a deliberately slow KDF). A timeout is swallowed on purpose: the caller's
    /// IsErrorDisplayed / IsStatusDisplayed assertion then reports the real, unmasked state.
    /// Use: High (once per submit). Scope: this page object.
    /// </summary>
    private ChangePinPage WaitForFeedback()
    {
        try
        {
            Wait.Until(_ => IsElementDisplayed(ErrorLabel) || IsElementDisplayed(StatusLabel));
        }
        catch (WebDriverTimeoutException)
        {
            // Intentional: let the caller's assertion describe the missing feedback.
        }

        return this;
    }
}
