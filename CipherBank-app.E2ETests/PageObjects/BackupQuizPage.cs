using System.Globalization;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.PageObjects;

/// <summary>Confirm three recovery words (CB-ACCOUNT-001 backup quiz; US-ONB-03 negative).</summary>
public class BackupQuizPage : BasePage
{
    private static readonly By PageRoot = By.Id("BackupQuizPage");
    private static readonly By PromptLabels = By.Id("BackupQuizPrompt");
    private static readonly By AnswerEntries = By.Id("BackupQuizAnswerEntry");
    private static readonly By VerifyButton = By.Id("BackupQuizVerifyButton");
    private static readonly By ErrorLabel = By.Id("BackupQuizErrorLabel");

    public BackupQuizPage(AppiumDriver driver)
        : base(driver)
    {
    }

    public override void WaitForPageLoad() => WaitForElement(VerifyButton);

    public bool IsLoaded() => IsElementDisplayed(PageRoot) || IsElementDisplayed(VerifyButton);

    public bool IsErrorDisplayed() => IsElementDisplayed(ErrorLabel);

    /// <summary>
    /// Fills each "Word #N" prompt with the matching word from the shown mnemonic.
    /// Prompts and entries share document (row) order, so index <c>i</c> of one maps to index <c>i</c> of the other.
    /// </summary>
    public BackupQuizPage AnswerFromMnemonic(string mnemonic)
    {
        string[] words = mnemonic.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var prompts = Driver.FindElements(PromptLabels);
        var entries = Driver.FindElements(AnswerEntries);

        for (int i = 0; i < entries.Count && i < prompts.Count; i++)
        {
            int wordNumber = ParseWordNumber(prompts[i].Text);
            if (wordNumber >= 1 && wordNumber <= words.Length)
            {
                entries[i].Clear();
                entries[i].SendKeys(words[wordNumber - 1]);
            }
        }

        return this;
    }

    /// <summary>Fills every prompt with a deliberately wrong word (US-ONB-03 negative path).</summary>
    public BackupQuizPage AnswerWrong()
    {
        var entries = Driver.FindElements(AnswerEntries);
        foreach (var entry in entries)
        {
            entry.Clear();
            entry.SendKeys("zzzz");
        }

        return this;
    }

    /// <summary>Taps Verify and advances to Set PIN (correct answers).</summary>
    public SetPinPage Verify()
    {
        ClickElement(VerifyButton);
        return new SetPinPage(Driver);
    }

    /// <summary>Taps Verify but stays on the quiz (wrong answers → error surfaced).</summary>
    public BackupQuizPage VerifyExpectingError()
    {
        ClickElement(VerifyButton);
        return this;
    }

    private static int ParseWordNumber(string prompt)
    {
        int hashIndex = prompt.LastIndexOf('#');
        if (hashIndex < 0 || hashIndex + 1 >= prompt.Length)
        {
            return -1;
        }

        string digits = new string(prompt[(hashIndex + 1)..].Where(char.IsDigit).ToArray());
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) ? n : -1;
    }
}
