using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Welcome / create-account entry (CB-ACCOUNT-001 / US-ONB-01).</summary>
public class WelcomePage : BasePage
{
    private static readonly By CreateButton = By.Id("WelcomeCreateWalletButton");
    private static readonly By ReturningButton = By.Id("WelcomeReturningButton");
    private static readonly By RestoreBackupButton = By.Id("WelcomeRestoreFromBackupButton");

    public WelcomePage(AppiumDriver driver) : base(driver)
    {
    }

    public override void WaitForPageLoad() => WaitForElement(CreateButton);

    public bool IsLoaded() => IsElementDisplayed(CreateButton);

    public KeysPage StartCreateAccount()
    {
        ClickElement(CreateButton);
        return new KeysPage(Driver);
    }

    public KeysPage StartReturning()
    {
        ClickElement(ReturningButton);
        return new KeysPage(Driver);
    }

    public void OpenRestoreFromBackup() => ClickElement(RestoreBackupButton);
}
