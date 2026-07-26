using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Page object for Profile tab (Security card first, then Preferences/Vault/Backup).</summary>
public class ProfilePage : BasePage
{
    private const string BackupPasswordId = "ProfileBackupPasswordEntry";
    private const string BackupPasswordConfirmId = "ProfileBackupPasswordConfirmEntry";
    private const string BackupHintId = "ProfileBackupHintEntry";
    private const string ExportBackupId = "ProfileExportBackupButton";
    private const string RevealMnemonicId = "ProfileRevealMnemonicButton";
    private const string MnemonicRevealLabelId = "ProfileMnemonicRevealLabel";

    private static readonly By ChangePinButton = By.Id("ProfileChangePinButton");
    private static readonly By LockButton = By.Id("ProfileLockButton");
    private static readonly By MnemonicRevealLabel = By.Id(MnemonicRevealLabelId);

    public ProfilePage(AppiumDriver driver) : base(driver)
    {
    }

    /// <summary>
    /// Anchors on the Security card's Change PIN button: it sits at the top of the Profile ScrollView, so it
    /// is on screen without scrolling, unlike the Save prefs button further down.
    /// Use: High (every Profile story). Scope: this page object.
    /// </summary>
    public bool IsLoaded() => IsElementDisplayed(ChangePinButton);

    /// <summary>
    /// Blocks until the Security card's anchor control is on screen.
    /// Use: High (every Profile-rooted story). Scope: this page object.
    /// </summary>
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

    /// <summary>
    /// Drives the app's own backup-export card end to end: fills the recovery password twice plus a hint,
    /// taps "Create and save backup file", answers the step-up PIN challenge, and declines the share offer so
    /// the run finishes on the saved-to-device copy. The recovery file's bytes are produced entirely by the
    /// app (Core <c>IMnemonicBackupService</c>); nothing here touches the mnemonic or the ciphertext.
    /// Use: Medium (backup/restore stories). Scope: this page object.
    /// </summary>
    public ProfilePage ExportRecoveryFile(string recoveryPassword, string hint, string stepUpPin)
    {
        ScrollTo(BackupPasswordId);
        EnterText(By.Id(BackupPasswordId), recoveryPassword);
        EnterText(By.Id(BackupPasswordConfirmId), recoveryPassword);
        EnterText(By.Id(BackupHintId), hint);
        HideKeyboard();

        ScrollTo(ExportBackupId);
        ClickElement(By.Id(ExportBackupId));

        new StepUpPinPromptPage(Driver).Submit(stepUpPin);
        new NativeAlertPage(Driver).Decline();
        return this;
    }

    /// <summary>
    /// Reveals the wallet's recovery phrase through the app's own Vault card (step-up PIN → custody
    /// <c>ExportMnemonic</c>) and returns it. This is the surface a recovery story uses to prove the device
    /// holds the original custody, not merely that some wallet exists.
    /// Use: Medium (custody-equivalence assertions). Scope: this page object.
    /// </summary>
    public string RevealMnemonic(string stepUpPin)
    {
        ScrollTo(RevealMnemonicId);
        ClickElement(By.Id(RevealMnemonicId));
        new StepUpPinPromptPage(Driver).Submit(stepUpPin);

        // The phrase label sits below its button, so revealing it usually pushes it past the fold.
        ScrollTo(MnemonicRevealLabelId);
        try
        {
            return WaitForNonEmptyText(MnemonicRevealLabel);
        }
        catch (WebDriverTimeoutException ex)
        {
            throw new InvalidOperationException(
                "Profile → Reveal mnemonic produced no phrase: the step-up PIN was rejected, or custody "
                + "was locked when the reveal ran.",
                ex);
        }
    }
}
