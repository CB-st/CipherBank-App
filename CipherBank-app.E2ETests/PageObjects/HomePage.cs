using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Home tab.</summary>
public class HomePage : BasePage
{
    private static readonly By TotalUsd = By.Id("HomeTotalUsdLabel");
    private static readonly By ConvertButton = By.Id("HomeConvertButton");
    private static readonly By ReceiveButton = By.Id("HomeReceiveButton");

    public HomePage(AppiumDriver driver) : base(driver)
    {
    }

    public bool IsLoaded() => IsElementDisplayed(TotalUsd);

    public ConvertPage GoToConvert()
    {
        ClickElement(ConvertButton);
        return new ConvertPage(Driver);
    }

    public ReceivePage GoToReceive()
    {
        ClickElement(ReceiveButton);
        return new ReceivePage(Driver);
    }

    public override void WaitForPageLoad() => WaitForElement(TotalUsd);
}
