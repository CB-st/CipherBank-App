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

    public ChangePinPage(AppiumDriver driver) : base(driver)
    {
    }

    /// <summary>Whether the Change PIN submit control is on screen. Use: Medium. Scope: this page object.</summary>
    public bool IsLoaded() => IsElementDisplayed(SubmitButton);

    public override void WaitForPageLoad() => WaitForElement(SubmitButton);

    /// <summary>
    /// Fills all three PIN fields and taps Update PIN, staying on this page so the caller can assert the
    /// status (success) or error (rejection) label. Use: High (CB-ACCOUNT-PIN-CHANGE). Scope: this page object.
    /// </summary>
    public ChangePinPage Submit(string currentPin, string newPin, string confirmPin)
    {
        EnterText(CurrentEntry, currentPin);
        EnterText(NewEntry, newPin);
        EnterText(ConfirmEntry, confirmPin);
        ClickElement(SubmitButton);
        return this;
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
}
