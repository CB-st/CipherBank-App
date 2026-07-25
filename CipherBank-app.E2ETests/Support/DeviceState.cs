using CipherBank_app.E2ETests.PageObjects;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Interfaces;

namespace CipherBank_app.E2ETests.Support;

/// <summary>Named device custody profiles a story can require before its Fact runs.</summary>
public enum DeviceProfile
{
    Fresh,
    Sealed,
}

/// <summary>
/// Establishes deterministic device custody state before a story Fact.
/// Use: High (each account/device story). Scope: Appium session.
/// </summary>
public sealed class DeviceState
{
    private static readonly TimeSpan RelaunchSettleDelay = TimeSpan.FromSeconds(1);

    private readonly AppiumDriver _driver;
    private readonly StoryJournal _journal;

    public DeviceState(AppiumDriver driver, StoryJournal journal)
    {
        _driver = driver;
        _journal = journal;
    }

    /// <summary>
    /// Clears app data and relaunches so the app boots to Welcome (no sealed wallet on device).
    /// Use: High. Scope: Fresh-profile stories (onboarding, restore).
    /// </summary>
    public async Task<WelcomePage> FreshAsync()
    {
        string package = EmulatorReset.ResolvePackageId();
        await Task.Run(() => EmulatorReset.ClearAppData(package)).ConfigureAwait(false);
        _journal.RecordStep("device: cleared app data (Fresh)");

        RelaunchApp(package);
        await Task.Delay(RelaunchSettleDelay).ConfigureAwait(false);

        var welcome = new WelcomePage(_driver);
        welcome.WaitForPageLoad();
        return welcome;
    }

    /// <summary>
    /// Fresh + drives Welcome→Keys→Quiz→SetPin through real UI using the journal PIN, journaling the mnemonic
    /// shown on Keys. Use: High (every story that needs a sealed wallet). Scope: Appium session.
    /// </summary>
    public async Task<HomePage> SealedAsync()
    {
        var welcome = await FreshAsync().ConfigureAwait(false);

        var keys = welcome.StartCreateAccount();
        keys.WaitForPageLoad();

        string mnemonic = keys.GetMnemonic();
        _journal.SetMnemonic(mnemonic);
        _journal.RecordStep("device: recorded mnemonic from Keys screen");

        var quiz = keys.Continue();
        quiz.WaitForPageLoad();
        quiz.AnswerFromMnemonic(mnemonic);

        var setPin = quiz.Verify();
        setPin.WaitForPageLoad();

        var home = setPin.SealMatching(_journal.Pin);
        home.WaitForPageLoad();
        _journal.RecordStep($"device: sealed vault with PIN={_journal.Pin}");
        return home;
    }

    /// <summary>
    /// Brings the app to the foreground (starting it if terminated by pm clear) via the platform-neutral
    /// app-lifecycle interface, avoiding an Android/iOS if/else here.
    /// Use: High. Scope: post-reset relaunch step.
    /// </summary>
    private void RelaunchApp(string package)
    {
        if (_driver is not IInteractsWithApps appLifecycle)
        {
            throw new InvalidOperationException(
                $"Driver {_driver.GetType().Name} does not support app activation; cannot relaunch after Fresh reset.");
        }

        appLifecycle.ActivateApp(package);
    }
}
