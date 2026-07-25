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

    public bool IsErrorDisplayed() => IsElementDisplayed(ErrorLabel);

    public override void WaitForPageLoad() => WaitForElement(UnlockButton);
}
