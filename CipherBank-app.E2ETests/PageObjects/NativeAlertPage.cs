using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// Platform alert/confirm dialogs raised by <c>IDialogService</c> (Shell <c>DisplayAlertAsync</c>).
/// MAUI maps the <c>accept</c> label to the dialog's positive button and <c>cancel</c> to its negative
/// button, so stories choose a side rather than matching button text (which the platform may re-case).
/// </summary>
public class NativeAlertPage : BasePage
{
    private static readonly By AcceptButton = By.Id("android:id/button1");
    private static readonly By DeclineButton = By.Id("android:id/button2");

    public NativeAlertPage(AppiumDriver driver) : base(driver)
    {
    }

    /// <summary>Blocks until a dialog is up. Use: Medium (each dialog-bearing action). Scope: this page object.</summary>
    public override void WaitForPageLoad() => WaitForElement(AcceptButton);

    /// <summary>Whether a platform dialog is currently on screen. Use: Medium. Scope: this page object.</summary>
    public bool IsLoaded() => IsElementDisplayed(AcceptButton);

    /// <summary>Takes the dialog's accept action. Use: Medium. Scope: this page object.</summary>
    public void Accept()
    {
        WaitForPageLoad();
        ClickElement(AcceptButton);
    }

    /// <summary>
    /// Takes the dialog's cancel/decline action, falling back to accept for single-button alerts (which have
    /// only a positive button). Use: Medium. Scope: this page object.
    /// </summary>
    public void Decline()
    {
        WaitForPageLoad();
        ClickElement(IsElementDisplayed(DeclineButton) ? DeclineButton : AcceptButton);
    }
}
