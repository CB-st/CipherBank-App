using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Recovery phrase screen (CB-ACCOUNT-001 backup step).</summary>
public class KeysPage : BasePage
{
    private static readonly By PageRoot = By.Id("KeysPage");
    private static readonly By MnemonicLabel = By.Id("KeysMnemonicLabel");
    private static readonly By ContinueButton = By.Id("KeysContinueButton");

    public KeysPage(AppiumDriver driver) : base(driver)
    {
    }

    public override void WaitForPageLoad() => WaitForElement(ContinueButton);

    public string GetMnemonic() => GetElementText(MnemonicLabel);

    public BackupQuizPage Continue()
    {
        ClickElement(ContinueButton);
        return new BackupQuizPage(Driver);
    }

    public bool IsLoaded() => IsElementDisplayed(PageRoot) || IsElementDisplayed(ContinueButton);
}
