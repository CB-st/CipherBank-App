using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Unlock (PIN) screen.</summary>
public class UnlockPage : BasePage
{
    private static readonly By PinField = By.Id("UnlockPinEntry");
    private static readonly By UnlockButton = By.Id("UnlockButton");
    private static readonly By ErrorLabel = By.Id("UnlockErrorLabel");

    public UnlockPage(AppiumDriver driver) : base(driver)
    {
    }

    /// <summary>
    /// Whether the Unlock PIN button is visible (device already has a sealed wallet).
    /// Use: Medium (wrong-screen boot checks). Scope: single page object.
    /// </summary>
    public bool IsLoaded() => IsElementDisplayed(UnlockButton);

    public UnlockPage EnterPin(string pin)
    {
        EnterText(PinField, pin);
        return this;
    }

    public HomePage UnlockWithPin(string pin)
    {
        EnterPin(pin);
        ClickElement(UnlockButton);
        return new HomePage(Driver);
    }

    /// <summary>
    /// Enters a PIN the caller expects to be rejected and stays on this page object so the caller can assert
    /// the error surfaced and Unlock is still on screen.
    /// Use: Medium (revoked/wrong-PIN assertions). Scope: this page object.
    /// </summary>
    public UnlockPage AttemptUnlockExpectingRejection(string pin)
    {
        EnterPin(pin);
        ClickElement(UnlockButton);
        return this;
    }

    /// <summary>
    /// Confirms the unlock error actually surfaced: visible per its XAML IsVisible binding AND carrying text,
    /// so the always-in-tree label cannot fake a pass. Use: Medium. Scope: this page object.
    /// </summary>
    public bool IsErrorDisplayed()
        => IsElementDisplayed(ErrorLabel) && !string.IsNullOrWhiteSpace(GetElementText(ErrorLabel));

    public override void WaitForPageLoad() => WaitForElement(UnlockButton);
}
