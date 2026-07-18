using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Profile tab.</summary>
public class ProfilePage : BasePage
{
    private static readonly By SavePrefsButton = By.Id("ProfileSavePrefsButton");

    public ProfilePage(AppiumDriver driver) : base(driver)
    {
    }

    public bool IsLoaded() => IsElementDisplayed(SavePrefsButton);

    public override void WaitForPageLoad() => WaitForElement(SavePrefsButton);
}
