using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Convert tab.</summary>
public class ConvertPage : BasePage
{
    private static readonly By LockQuoteButton = By.Id("ConvertLockQuoteButton");
    private static readonly By ConvertSubmitButton = By.Id("ConvertSubmitButton");

    public ConvertPage(AppiumDriver driver) : base(driver)
    {
    }

    public ConvertPage LockQuote()
    {
        ClickElement(LockQuoteButton);
        return this;
    }

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

    public override void WaitForPageLoad() => WaitForElement(LockQuoteButton);
}
