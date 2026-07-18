using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Receive tab.</summary>
public class ReceivePage : BasePage
{
    private static readonly By RefreshButton = By.Id("ReceiveRefreshButton");
    private static readonly By QrImage = By.Id("ReceiveQrImage");
    private static readonly By AddressLabel = By.Id("ReceiveAddressLabel");

    public ReceivePage(AppiumDriver driver) : base(driver)
    {
    }

    public ReceivePage RefreshQr()
    {
        ClickElement(RefreshButton);
        return this;
    }

    public bool IsQrVisible() => IsElementDisplayed(QrImage);

    public string GetAddress() => GetElementText(AddressLabel);

    public override void WaitForPageLoad() => WaitForElement(RefreshButton);
}
