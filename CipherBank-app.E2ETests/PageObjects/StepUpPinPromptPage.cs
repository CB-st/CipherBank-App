// <copyright file="StepUpPinPromptPage.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.E2ETests.Support;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// The platform step-up PIN prompt raised by Core <c>StepUpAuthService</c> through
/// <c>MauiStepUpChallenges.PromptForPinAsync</c> (Shell <c>DisplayPromptAsync</c>). It is a native
/// AlertDialog rather than a XAML page, so it is located by framework resource ids instead of AutomationIds.
/// Guards Profile's Reveal-mnemonic and Create-backup-file actions.
/// </summary>
public class StepUpPinPromptPage : BasePage
{
    private static readonly By PositiveButton = By.Id("android:id/button1");

    /// <summary>
    /// The prompt's field lives in the dialog's custom-view slot. AppCompat namespaces that slot to the
    /// hosting app (<c>com.companyname.cipherbankapp:id/custom</c>) while the framework theme uses
    /// <c>android:id/custom</c>, so the id is matched by suffix.
    /// </summary>
    private static readonly By PromptInput =
        By.XPath("//*[contains(@resource-id, ':id/custom')]//android.widget.EditText");

    private static readonly By DialogInput = By.XPath("//android.widget.EditText");

    /// <summary>
    /// Initializes StepUpPinPromptPage with its Appium/test collaborators. Use: High. Scope: one StepUpPinPromptPage instance.
    /// </summary>
    public StepUpPinPromptPage(AppiumDriver driver)
        : base(driver)
    {
    }

    /// <summary>Blocks until the prompt's confirm button is up. Use: Medium (each step-up). Scope: this page object.</summary>
    public override void WaitForPageLoad() => WaitForElement(PositiveButton);

    /// <summary>Whether a step-up prompt is currently on screen. Use: Medium. Scope: this page object.</summary>
    public bool IsLoaded() => IsElementDisplayed(PositiveButton);

    /// <summary>
    /// Answers the step-up challenge with <paramref name="pin"/> and confirms, leaving the caller back on the
    /// page that raised it. Use: Medium (Profile reveal / export stories). Scope: this page object.
    /// </summary>
    public void Submit(string pin)
    {
        WaitForPageLoad();
        IWebElement input = FindPromptInput();
        input.Clear();
        input.SendKeys(pin);
        ClickElement(PositiveButton);
    }

    /// <summary>
    /// Resolves the prompt's text field, preferring the dialog's own custom-view slot. The unscoped fallback
    /// is safe because a platform dialog owns the foreground window: while one is up the page source contains
    /// the dialog alone, so this cannot reach an entry on the page behind it.
    /// Use: Medium (once per step-up). Scope: this page object.
    /// </summary>
    private IWebElement FindPromptInput()
    {
        var scoped = Driver.FindElements(PromptInput);
        if (scoped.Count > 0)
        {
            return scoped[^1];
        }

        var inDialog = Driver.FindElements(DialogInput);
        if (inDialog.Count > 0)
        {
            return inDialog[^1];
        }

        throw new NoSuchElementException(
            "A platform dialog is showing but has no PIN input field — the app raised an alert "
            + "(e.g. locked custody) instead of the expected step-up prompt. Screen dump: "
            + DeviceDiagnostics.CapturePageSource(Driver, "step-up-prompt"));
    }
}
