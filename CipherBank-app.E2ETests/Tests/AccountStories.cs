using CipherBank_app.E2ETests.PageObjects;
using CipherBank_app.E2ETests.Stories;
using CipherBank_app.E2ETests.Support;
using FluentAssertions;
using OpenQA.Selenium;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Account/onboarding stories that need a Fresh (wallet-less) device: CB-ACCOUNT-001 create-account and
/// US-ONB-04 PIN-mismatch. Kept out of <see cref="CoraShellSmokeTests"/> so the sealed-device smoke suite
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
        string mnemonic = keys.GetMnemonic();
        mnemonic.Should().NotBeNullOrWhiteSpace("Keys screen shows the generated recovery phrase");
        _fixture!.Journal.SetMnemonic(mnemonic);
        _fixture.Journal.RecordStep("device: recorded mnemonic from Keys screen");

        var quiz = keys.Continue();
        quiz.WaitForPageLoad();
        quiz.AnswerFromMnemonic(mnemonic);

        var setPin = quiz.Verify();
        setPin.WaitForPageLoad();

        var home = setPin.SealMatching(_fixture.Journal.Pin);
        home.WaitForPageLoad();
        home.IsLoaded().Should().BeTrue("US-ONB-01: sealing the vault lands on Home");
        _fixture.Journal.RecordStep($"device: sealed vault with PIN={_fixture.Journal.Pin}");
        _fixture.Journal.Flush(StoryIds.CbAccount001);
    }

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

    /// <summary>Quits and disposes the owned Appium session (no-op when E2E_RUN is unset). Use: High. Scope: this fixture.</summary>
    public void Dispose()
    {
        _fixture?.Dispose();
    }
}
