using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Profile tab (Security card first, then Preferences/Vault/Backup).</summary>
public class ProfilePage : BasePage
{
    private static readonly By ChangePinButton = By.Id("ProfileChangePinButton");
    private static readonly By LockButton = By.Id("ProfileLockButton");
    private static readonly By SavePrefsButton = By.Id("ProfileSavePrefsButton");

    public ProfilePage(AppiumDriver driver) : base(driver)
    {
    }

    /// <summary>
    /// Anchors on the Security card's Change PIN button: it sits at the top of the Profile ScrollView, so it
    /// is on screen without scrolling, unlike the Save prefs button further down.
    /// Use: High (every Profile story). Scope: this page object.
    /// </summary>
    public bool IsLoaded() => IsElementDisplayed(ChangePinButton);

    /// <summary>Whether the (scrolled-down) prefs card is reachable. Use: Low. Scope: this page object.</summary>
    public bool HasPrefsCard() => IsElementDisplayed(SavePrefsButton);

    public override void WaitForPageLoad() => WaitForElement(ChangePinButton);

    /// <summary>Opens Profile → Security → Change PIN. Use: High (CB-ACCOUNT-PIN-CHANGE). Scope: this page object.</summary>
    public ChangePinPage OpenChangePin()
    {
        ClickElement(ChangePinButton);
        return new ChangePinPage(Driver);
    }

    /// <summary>
    /// Locks the session from Profile → Security, landing on Unlock so a story can re-enter with a PIN.
    /// Use: High (lock/unlock stories). Scope: this page object.
    /// </summary>
    public UnlockPage LockApp()
    {
        ClickElement(LockButton);
        return new UnlockPage(Driver);
    }
}
