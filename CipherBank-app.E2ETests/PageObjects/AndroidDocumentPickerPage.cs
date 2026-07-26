using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Android;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>
/// Android's system document picker (DocumentsUI), opened by the app's real
/// <c>FilePicker.PickAsync</c> / <c>ACTION_OPEN_DOCUMENT</c> call on the Restore-from-backup page.
/// It belongs to the platform, not the Shell, so it is located by DocumentsUI resource ids and visible
/// text instead of AutomationIds.
/// </summary>
public class AndroidDocumentPickerPage : BasePage
{
    private const string DocumentsUiPackageFragment = "documentsui";
    private const string DownloadsRootLabel = "Downloads";

    private static readonly By ShowRootsButton =
        By.XPath("//*[@content-desc='Show roots' or @content-desc='Show roots and folders']");
    private static readonly TimeSpan PickerTimeout = TimeSpan.FromSeconds(20);

    public AndroidDocumentPickerPage(AppiumDriver driver) : base(driver)
    {
    }

    /// <summary>
    /// Blocks until DocumentsUI is the foreground package, i.e. the app really handed off to the system
    /// picker rather than silently failing. Use: Medium (each restore story). Scope: this page object.
    /// </summary>
    public override void WaitForPageLoad()
    {
        var android = Driver as AndroidDriver
            ?? throw new InvalidOperationException(
                "AndroidDocumentPickerPage requires the Android driver; the system picker is Android-only.");

        var wait = new OpenQA.Selenium.Support.UI.WebDriverWait(Driver, PickerTimeout);
        wait.Until(_ => android.CurrentPackage.Contains(DocumentsUiPackageFragment, StringComparison.Ordinal));
    }

    /// <summary>
    /// Picks <paramref name="fileName"/> out of the device's Downloads collection through the real picker:
    /// takes it straight from the current listing when already shown, otherwise opens the roots drawer and
    /// navigates to Downloads first.
    /// Use: Medium (each restore story). Scope: this page object.
    /// </summary>
    public void SelectFileFromDownloads(string fileName)
    {
        WaitForPageLoad();
        By file = FileEntry(fileName);
        if (IsElementDisplayed(file))
        {
            ClickElement(file);
            return;
        }

        OpenDownloadsRoot();
        ClickElement(file);
    }

    /// <summary>
    /// Opens the picker's roots drawer and selects Downloads. The drawer button is absent on layouts that
    /// already show the roots list, so a missing button is not an error.
    /// Use: Medium (once per pick). Scope: this page object.
    /// </summary>
    private void OpenDownloadsRoot()
    {
        if (IsElementDisplayed(ShowRootsButton))
        {
            ClickElement(ShowRootsButton);
        }

        ClickElement(TextEntry(DownloadsRootLabel));
    }

    /// <summary>
    /// Locator for a file row: DocumentsUI puts the display name in the row's text and repeats it (with
    /// size/date) in the row's content description.
    /// Use: Medium (per pick). Scope: this page object.
    /// </summary>
    private static By FileEntry(string fileName) =>
        By.XPath($"//*[@text={XPathLiteral(fileName)} or starts-with(@content-desc, {XPathLiteral(fileName)})]");

    /// <summary>Locator for a picker chrome entry (root names, menu items). Use: Medium. Scope: this page object.</summary>
    private static By TextEntry(string label) =>
        By.XPath($"//*[@text={XPathLiteral(label)} or @content-desc={XPathLiteral(label)}]");

    /// <summary>
    /// Quotes a value for XPath, switching to double quotes when the value contains an apostrophe so
    /// generated recovery file names can never break the expression.
    /// Use: Medium (per locator build). Scope: this page object.
    /// </summary>
    private static string XPathLiteral(string value) =>
        value.Contains('\'') ? $"\"{value}\"" : $"'{value}'";
}
