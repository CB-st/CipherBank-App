using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Home tab.</summary>
public class HomePage : BasePage
{
    private static readonly By TotalUsd = By.Id("HomeTotalUsdLabel");
    private static readonly By HideBalances = By.Id("HomeHideBalancesButton");
    private static readonly By Range1d = By.Id("HomeRange1dButton");
    private static readonly By Range1w = By.Id("HomeRange1wButton");
    private static readonly By Range1m = By.Id("HomeRange1mButton");
    private static readonly By Range1y = By.Id("HomeRange1yButton");
    private static readonly By ConvertButton = By.Id("HomeConvertButton");
    private static readonly By SendButton = By.Id("HomeSendButton");
    private static readonly By ReceiveButton = By.Id("HomeReceiveButton");

    public HomePage(AppiumDriver driver) : base(driver)
    {
    }

    public bool IsLoaded() => IsElementDisplayed(TotalUsd);

    public bool HasChartRangeChips()
        => IsElementDisplayed(Range1d)
           && IsElementDisplayed(Range1w)
           && IsElementDisplayed(Range1m)
           && IsElementDisplayed(Range1y);

    public bool HasHideBalancesToggle() => IsElementDisplayed(HideBalances);

    public HomePage ToggleHideBalances()
    {
        ClickElement(HideBalances);
        return this;
    }

    public HomePage SelectRange1w()
    {
        ClickElement(Range1w);
        return this;
    }

    public ConvertPage GoToConvert()
    {
        ClickElement(ConvertButton);
        return new ConvertPage(Driver);
    }

    public SendPage GoToSend()
    {
        ClickElement(SendButton);
        return new SendPage(Driver);
    }

    /// <summary>Navigate via Shell tab bar when Home shortcuts are off-screen.</summary>
    public ConvertPage GoToConvertTab()
    {
        TapByText("Convert");
        return new ConvertPage(Driver);
    }

    public SendPage GoToSendTab()
    {
        TapByText("Send");
        return new SendPage(Driver);
    }

    public ReceivePage GoToReceive()
    {
        ClickElement(ReceiveButton);
        return new ReceivePage(Driver);
    }

    public override void WaitForPageLoad() => WaitForElement(TotalUsd);
}
