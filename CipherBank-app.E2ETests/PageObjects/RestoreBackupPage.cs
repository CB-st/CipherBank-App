using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// Restore-from-backup surface (Welcome → "Restore from backup file", or Unlock → "Recover with backup
/// file") used by CB-ACCOUNT-002: pick a ciphered recovery file through the system document picker, enter
/// the recovery password, and let <c>RestoreBackupViewModel</c> decrypt it and hand the mnemonic to SetPin.
/// </summary>
public class RestoreBackupPage : BasePage
{
    private static readonly By PickFileButton = By.Id("RestoreBackupPickFileButton");
    private static readonly By PasswordEntry = By.Id("RestoreBackupPasswordEntry");
    private static readonly By OpenButton = By.Id("RestoreBackupOpenButton");
    private static readonly By ErrorLabel = By.Id("RestoreBackupErrorLabel");
    private static readonly By FileStatusLabel = By.Id("RestoreBackupFileStatusLabel");

    public RestoreBackupPage(AppiumDriver driver) : base(driver)
    {
    }

    /// <summary>Blocks until the file-chooser control is on screen. Use: High (CB-ACCOUNT-002). Scope: this page object.</summary>
    public override void WaitForPageLoad() => WaitForElement(PickFileButton);

    /// <summary>Whether the restore page is the current screen. Use: High. Scope: this page object.</summary>
    public bool IsLoaded() => IsElementDisplayed(PickFileButton);

    /// <summary>
    /// Taps "Choose recovery file", which hands off to Android's own document picker via the app's
    /// <c>IBackupFileService.PickBackupFileAsync</c>. Use: High (CB-ACCOUNT-002). Scope: this page object.
    /// </summary>
    public AndroidDocumentPickerPage OpenFilePicker()
    {
        ClickElement(PickFileButton);
        var picker = new AndroidDocumentPickerPage(Driver);
        picker.WaitForPageLoad();
        return picker;
    }

    /// <summary>
    /// Confirms the app actually ingested a picked file: the status label is visible (per its XAML IsVisible
    /// binding) and carries text, so a cancelled or unreadable pick cannot look like a success.
    /// Use: High (CB-ACCOUNT-002). Scope: this page object.
    /// </summary>
    public bool IsFileSelected()
    {
        try
        {
            WaitForNonEmptyText(FileStatusLabel);
        }
        catch (WebDriverTimeoutException)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Enters the recovery password and submits, expecting the file to open and the Shell to navigate on to
    /// SetPin with the recovered mnemonic. Use: High (CB-ACCOUNT-002 happy path). Scope: this page object.
    /// </summary>
    public SetPinPage Restore(string recoveryPassword)
    {
        SubmitPassword(recoveryPassword);
        return new SetPinPage(Driver);
    }

    /// <summary>
    /// Submits a password expected to be rejected, waiting for the page's own error before returning so the
    /// caller's assertion is not racing the KDF-bound decrypt attempt.
    /// Use: Medium (CB-ACCOUNT-002 wrong-password leg). Scope: this page object.
    /// </summary>
    public RestoreBackupPage RestoreExpectingError(string recoveryPassword)
    {
        SubmitPassword(recoveryPassword);
        try
        {
            Wait.Until(_ => IsElementDisplayed(ErrorLabel));
        }
        catch (WebDriverTimeoutException)
        {
            // Intentional: let the caller's assertion describe the missing error.
        }

        return this;
    }

    /// <summary>
    /// Confirms a restore error actually surfaced: visible per its XAML IsVisible binding AND non-empty, so
    /// an always-in-tree label cannot fake a pass. Use: Medium. Scope: this page object.
    /// </summary>
    public bool IsErrorDisplayed()
        => IsElementDisplayed(ErrorLabel) && !string.IsNullOrWhiteSpace(GetElementText(ErrorLabel));

    /// <summary>
    /// Types the password and taps "Restore wallet", dismissing the keyboard first so the button is not left
    /// covered. Use: High (once per restore attempt). Scope: this page object.
    /// </summary>
    private void SubmitPassword(string recoveryPassword)
    {
        EnterText(PasswordEntry, recoveryPassword);
        HideKeyboard();
        ClickElement(OpenButton);
    }
}
