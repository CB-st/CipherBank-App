using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Set PIN / seal vault (CB-ACCOUNT-001 complete; US-ONB-04 mismatch).</summary>
public class SetPinPage : BasePage
{
    private static readonly By PinEntry = By.Id("SetPinEntry");
    private static readonly By ConfirmEntry = By.Id("SetPinConfirmEntry");
    private static readonly By SealButton = By.Id("SetPinSealButton");
    private static readonly By ErrorLabel = By.Id("SetPinErrorLabel");

    public SetPinPage(AppiumDriver driver) : base(driver)
    {
    }

    public override void WaitForPageLoad() => WaitForElement(SealButton);

    public SetPinPage EnterPin(string pin)
    {
        EnterText(PinEntry, pin);
        return this;
    }

    public SetPinPage EnterConfirm(string pin)
    {
        EnterText(ConfirmEntry, pin);
        return this;
    }

    public HomePage SealMatching(string pin)
    {
        EnterPin(pin);
        EnterConfirm(pin);
        ClickElement(SealButton);
        return new HomePage(Driver);
    }

    public SetPinPage SealMismatch(string pin, string confirm)
    {
        EnterPin(pin);
        EnterConfirm(confirm);
        ClickElement(SealButton);
        return this;
    }

    public bool IsErrorDisplayed() => IsElementDisplayed(ErrorLabel);
}
