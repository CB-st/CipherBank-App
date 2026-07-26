using CipherBank_app.E2ETests.PageObjects;
using CipherBank_app.E2ETests.Stories;
using CipherBank_app.E2ETests.Support;
using FluentAssertions;
using OpenQA.Selenium;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Account/onboarding stories that start from a Fresh (wallet-less) device: CB-ACCOUNT-001 create-account,
/// the US-ONB-03/04 negatives, and CB-ACCOUNT-PIN-CHANGE (which seals from Fresh, then changes the PIN).
/// Kept out of <see cref="CoraShellSmokeTests"/> so the sealed-device smoke suite
/// and this Fresh-install suite each fail fast on their own boot precondition instead of soft-returning.
/// Requires <c>E2E_RUN=1</c> and <c>ANDROID_APK_PATH</c> (or <c>IOS_APP_PATH</c>); skips (not a soft-pass)
/// when E2E_RUN is unset.
/// </summary>
[Collection("E2E Tests")]
public class AccountStories : IDisposable
{
    private readonly AppiumFixture? _fixture;

    /// <summary>
    /// Builds (or, when E2E_RUN is unset, leaves null) the shared Appium session for this run.
    /// Use: High (once per test instance). Scope: AccountStories session.
    /// </summary>
    public AccountStories()
    {
        _fixture = AppiumFixture.CreateOrThrow();
    }

    /// <summary>
    /// Drives the full CB-ACCOUNT-001 create-account flow (Welcome → Keys → BackupQuiz → SetPin → Home) on a
    /// Fresh device and journals each <see cref="StoryProcedures.Account001Steps"/> id as the device reaches
    /// its equivalent screen transition: open (tap Create wallet), complete-form (recovery phrase captured),
    /// submit (phrase acknowledged), backup (quiz verified), complete (PIN sealed, Home loaded).
    /// Use: High (canary Fact; the Wave 0 gate for this story). Scope: this Fact's device session.
    /// </summary>
    [SkippableFact]
    [Trait("Story", StoryIds.CbAccount001)]
    [Trait("Story", StoryIds.UsOnb01)]
    public async Task CB_ACCOUNT_001_US_ONB_01_CreateAccount()
    {
        Skip.If(_fixture is null, "E2E_RUN not set");

        var welcome = await FreshWelcomeOrFail(StoryIds.CbAccount001);
        welcome.IsLoaded().Should().BeTrue("US-ONB-01: clean install lands on Welcome");

        var keys = welcome.StartCreateAccount();
        keys.WaitForPageLoad();
        JournalProcedureStep("open");

        string mnemonic = keys.GetMnemonic();
        mnemonic.Should().NotBeNullOrWhiteSpace("Keys screen shows the generated recovery phrase");
        _fixture!.Journal.SetMnemonic(mnemonic);
        JournalProcedureStep("complete-form");

        var quiz = keys.Continue();
        quiz.WaitForPageLoad();
        JournalProcedureStep("submit");

        quiz.AnswerFromMnemonic(mnemonic);
        var setPin = quiz.Verify();
        setPin.WaitForPageLoad();
        JournalProcedureStep("backup");

        var home = setPin.SealMatching(_fixture.Journal.Pin);
        home.WaitForPageLoad();
        home.IsLoaded().Should().BeTrue("US-ONB-01: sealing the vault lands on Home");
        JournalProcedureStep("complete");

        _fixture.Journal.Flush(StoryIds.CbAccount001);
    }

    /// <summary>
    /// Fills the BackupQuiz prompts with deliberately wrong words and asserts the app's own guard
    /// (<c>BackupQuizViewModel.VerifyAsync</c>) surfaces <c>BackupQuizErrorLabel</c> and keeps the user on
    /// BackupQuiz instead of advancing to SetPin. Negative counterpart to CB-ACCOUNT-001/US-ONB-01.
    /// Use: High (Wave 0 onboarding-negative gate). Scope: this Fact's device session.
    /// </summary>
    [SkippableFact]
    [Trait("Story", StoryIds.UsOnb03)]
    public async Task US_ONB_03_WrongQuizWords_BlocksAdvance()
    {
        Skip.If(_fixture is null, "E2E_RUN not set");

        var welcome = await FreshWelcomeOrFail(StoryIds.UsOnb03);

        var keys = welcome.StartCreateAccount();
        keys.WaitForPageLoad();
        string mnemonic = keys.GetMnemonic();
        _fixture!.Journal.SetMnemonic(mnemonic);

        var quiz = keys.Continue();
        quiz.WaitForPageLoad();
        quiz.AnswerWrong().VerifyExpectingError();

        quiz.IsErrorDisplayed().Should().BeTrue("US-ONB-03: wrong quiz words surface an error");
        quiz.IsLoaded().Should().BeTrue("US-ONB-03: wrong quiz words must not advance past BackupQuiz to SetPin");
        _fixture.Journal.RecordStep("device: confirmed wrong quiz words block advance to SetPin");
        _fixture.Journal.Flush(StoryIds.UsOnb03);
    }

    /// <summary>
    /// Enters a PIN that differs from its confirmation and asserts the app's own guard
    /// (<c>SetPinViewModel.SealAsync</c>) surfaces <c>SetPinErrorLabel</c> instead of sealing the vault.
    /// Negative counterpart to CB-ACCOUNT-001/US-ONB-01.
    /// Use: High (Wave 0 onboarding-negative gate). Scope: this Fact's device session.
    /// </summary>
    [SkippableFact]
    [Trait("Story", StoryIds.UsOnb04)]
    public async Task US_ONB_04_PinMismatch_BlocksSeal()
    {
        Skip.If(_fixture is null, "E2E_RUN not set");

        var welcome = await FreshWelcomeOrFail(StoryIds.UsOnb04);

        var keys = welcome.StartCreateAccount();
        keys.WaitForPageLoad();
        string mnemonic = keys.GetMnemonic();

        var quiz = keys.Continue();
        quiz.WaitForPageLoad();
        quiz.AnswerFromMnemonic(mnemonic);

        var setPin = quiz.Verify();
        setPin.WaitForPageLoad();
        setPin.SealMismatch(_fixture!.Journal.Pin, _fixture.Journal.Pin + "9");
        setPin.IsErrorDisplayed().Should().BeTrue("US-ONB-04: mismatched confirm PIN blocks seal with an error");
        _fixture.Journal.RecordStep("device: confirmed mismatched PIN blocks seal");
        _fixture.Journal.Flush(StoryIds.UsOnb04);
    }

    /// <summary>
    /// CB-ACCOUNT-PIN-CHANGE: seals a wallet with the journaled PIN, proves a wrong current PIN is rejected
    /// on-device with a real error, then changes the PIN through the same Shell surface (Profile → Security →
    /// Change PIN) to the journaled AlternatePin, promotes that alternate to the active journal PIN, and
    /// locks/unlocks with it. The unlock is the proof: it only succeeds if the stored PIN really changed, and
    /// the PIN values are never hard-coded here.
    /// Use: High (Wave 1 PIN-change gate). Scope: this Fact's device session.
    /// </summary>
    [SkippableFact]
    [Trait("Story", StoryIds.CbAccountPinChange)]
    public async Task CB_ACCOUNT_PIN_CHANGE_DynamicPin()
    {
        Skip.If(_fixture is null, "E2E_RUN not set");

        var journal = _fixture!.Journal;
        var deviceState = new DeviceState(_fixture.Driver, journal);
        var home = await SealedHomeOrFail(deviceState);

        var profile = home.GoToProfileTab();
        profile.WaitForPageLoad();
        profile.IsLoaded().Should().BeTrue("Profile → Security card is the Change PIN entry point");

        var changePin = profile.OpenChangePin();
        changePin.WaitForPageLoad();
        journal.RecordStep($"device: opened Change PIN with active PIN={journal.Pin}");

        string oldPin = journal.Pin;
        string newPin = journal.AlternatePin;
        string wrongCurrentPin = ShiftDigits(oldPin);
        wrongCurrentPin.Should().NotBe(oldPin, "the rejection leg needs a PIN that is genuinely not the active one");
        wrongCurrentPin.Should().NotBe(newPin, "a wrong current PIN must not collide with the requested new PIN");

        changePin.Submit(wrongCurrentPin, newPin, newPin);
        changePin.IsErrorDisplayed().Should().BeTrue(
            "CB-ACCOUNT-PIN-CHANGE: a wrong current PIN must surface a visible, non-empty ChangePinErrorLabel");
        changePin.IsStatusDisplayed().Should().BeFalse("a rejected change must not report success");
        changePin.IsLoaded().Should().BeTrue("a rejected change keeps the user on Change PIN");
        journal.RecordStep($"device: rejected change with wrong current PIN={wrongCurrentPin}");

        changePin.Submit(oldPin, newPin, newPin);
        changePin.IsStatusDisplayed().Should().BeTrue(
            "CB-ACCOUNT-PIN-CHANGE: a valid change reports success on ChangePinStatusLabel "
            + "(and proves the rejected attempt preserved the old PIN)");
        changePin.IsErrorDisplayed().Should().BeFalse("a successful change must not surface an error");

        journal.PromoteAlternatePin();
        journal.RecordStep($"device: changed PIN {oldPin} -> {journal.Pin} (previous PIN journaled as alternate)");

        var backToProfile = changePin.BackToProfile();
        backToProfile.WaitForPageLoad();

        var unlock = backToProfile.LockApp();
        unlock.WaitForPageLoad();
        unlock.IsLoaded().Should().BeTrue("locking from Profile lands on Unlock");

        unlock.AttemptUnlockExpectingRejection(journal.AlternatePin);
        unlock.IsErrorDisplayed().Should().BeTrue(
            "CB-ACCOUNT-PIN-CHANGE: the replaced PIN must be rejected with an error");
        unlock.IsLoaded().Should().BeTrue("a rejected PIN keeps the user on Unlock");
        journal.RecordStep($"device: confirmed replaced PIN={journal.AlternatePin} no longer unlocks");

        var unlocked = unlock.UnlockWithPin(journal.Pin);
        unlocked.WaitForPageLoad();
        unlocked.IsLoaded().Should().BeTrue("CB-ACCOUNT-PIN-CHANGE: the new PIN unlocks the sealed wallet");
        journal.RecordStep($"device: unlocked with new PIN={journal.Pin}");
        journal.Flush(StoryIds.CbAccountPinChange);
    }

    /// <summary>
    /// Derives a same-length PIN that shares no digit with <paramref name="pin"/>, giving the rejection leg a
    /// deliberately wrong current PIN without hard-coding one alongside the journaled values.
    /// Use: Low (once per PIN-change story). Scope: this test class.
    /// </summary>
    private static string ShiftDigits(string pin)
        => string.Concat(pin.Select(c => (char)('0' + (((c - '0') + 1) % 10))));

    /// <summary>
    /// Seals a wallet through the real onboarding UI and returns Home; converts a boot/seal timeout into a
    /// gap note plus a loud failure so a broken Sealed precondition is never mistaken for a story failure.
    /// Use: Medium (Sealed-profile account stories). Scope: this test class session.
    /// </summary>
    private async Task<HomePage> SealedHomeOrFail(DeviceState deviceState)
    {
        try
        {
            return await deviceState.SealedAsync();
        }
        catch (WebDriverTimeoutException ex)
        {
            GapNotes.Write(
                StoryIds.CbAccountPinChange,
                step: "DeviceState.SealedAsync precondition",
                expected: "Home after Welcome→Keys→Quiz→SetPin with the journaled PIN",
                actual: $"onboarding did not reach Home: {ex.Message}",
                proposedFix: "Re-check the create-account flow (CB-ACCOUNT-001) before diagnosing the PIN-change story.");
            throw new InvalidOperationException(
                $"{StoryIds.CbAccountPinChange}: Sealed precondition failed; gap note written.", ex);
        }
    }

    /// <summary>
    /// Resets the device to Fresh and returns the loaded Welcome page; on a wrong-screen boot (e.g. a
    /// sealed wallet survives the reset and Unlock shows instead) writes a gap note and fails loudly
    /// instead of letting a bare Appium timeout obscure the precondition violation.
    /// Use: High (every Fresh-profile account story). Scope: this test class session.
    /// </summary>
    private async Task<WelcomePage> FreshWelcomeOrFail(string storyId)
    {
        var deviceState = new DeviceState(_fixture!.Driver, _fixture.Journal);
        try
        {
            return await deviceState.FreshAsync();
        }
        catch (WebDriverTimeoutException ex)
        {
            var unlock = new UnlockPage(_fixture.Driver);
            GapNotes.Write(
                storyId,
                step: "DeviceState.FreshAsync boot precondition",
                expected: "Welcome screen (WelcomeCreateWalletButton) after clearing app data",
                actual: unlock.IsLoaded() ? "Unlock screen (sealed wallet survived reset)" : "neither Welcome nor Unlock visible",
                proposedFix: "Confirm `adb shell pm clear` targets the running MAUI package id and that relaunch lands on Welcome for a wallet-less install.");
            throw new InvalidOperationException($"{storyId}: Fresh reset did not land on Welcome; gap note written.", ex);
        }
    }

    /// <summary>
    /// Journals one CB-ACCOUNT-001 procedure step, pairing <paramref name="stepId"/> with its
    /// <see cref="StoryProcedures.Account001Steps"/> description so the flushed journal traces which named
    /// procedure step the device reached instead of only ad-hoc prose.
    /// Use: High (every CB-ACCOUNT-001 screen transition). Scope: this Fact's device session.
    /// </summary>
    private void JournalProcedureStep(string stepId) =>
        _fixture!.Journal.RecordStep($"step:{stepId} - {StoryProcedures.Account001Steps[stepId]}");

    /// <summary>Quits and disposes the owned Appium session (no-op when E2E_RUN is unset). Use: High. Scope: this fixture.</summary>
    public void Dispose()
    {
        _fixture?.Dispose();
    }
}
