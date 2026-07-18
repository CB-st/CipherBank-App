using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for POS Lab.</summary>
public class PosLabPage : BasePage
{
    private static readonly By StartSessionButton = By.Id("PosStartSessionButton");
    private static readonly By SimulateButton = By.Id("PosSimulateButton");

    public PosLabPage(AppiumDriver driver) : base(driver)
    {
    }

    public PosLabPage StartSession()
    {
        ClickElement(StartSessionButton);
        return this;
    }

    public PosLabPage Simulate()
    {
        ClickElement(SimulateButton);
        return this;
    }

    public override void WaitForPageLoad() => WaitForElement(SimulateButton);
}
