// <copyright file="CoraShellSmokeTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.E2ETests.PageObjects;
using CipherBank_app.E2ETests.Stories;
using CipherBank_app.E2ETests.Support;
using FluentAssertions;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Interfaces;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Cora Shell smoke mapped to shared story IDs (US-* / CB-*), run against an already-sealed device.
/// Requires <c>E2E_RUN=1</c>, an Appium server (<c>APPIUM_SERVER_URL</c> or <c>APPIUM_PORT</c>, default
/// <c>http://127.0.0.1:4723</c>), and a DEBUG APK/app with a sealed wallet already on the device.
/// Skips (not a soft-pass) when E2E_RUN is unset; fails with a gap note when the device boots to Welcome
/// instead of Unlock (wrong precondition for this suite). Fresh-install account stories (CB-ACCOUNT-001,
/// US-ONB-04) live in <see cref="AccountStories"/>.
/// </summary>
[Collection("E2E Tests")]
public class CoraShellSmokeTests
{
    private readonly AppiumFixture? _fixture;

    /// <summary>
    /// Receives the shared Appium session from <see cref="AppiumFixtureHolder"/> (null when E2E_RUN is unset).
    /// Use: High (once per test instance). Scope: CoraShellSmokeTests session.
    /// </summary>
    public CoraShellSmokeTests(AppiumFixtureHolder holder)
    {
        _fixture = holder.Fixture;
    }

    /// <summary>
    /// Verifies the US LCK 01 CNV 01 RCV 01 Unlock Convert Quote Receive Qr E2E contract. Use: High. Scope: CoraShellSmokeTests.
    /// </summary>
    [SkippableFact]
    [Trait("Story", StoryIds.UsLck01)]
    [Trait("Story", StoryIds.UsCnv01)]
    [Trait("Story", StoryIds.UsRcv01)]
    [Trait("Story", StoryIds.CbFund001)]
    public void US_LCK_01_CNV_01_RCV_01_Unlock_ConvertQuote_ReceiveQr()
    {
        Skip.If(_fixture is null, "E2E_RUN not set");

        StoryRunner.Run(
            StoryIds.UsLck01,
            () =>
            {
                var home = UnlockToHome(StoryIds.UsLck01);

                var convert = home.GoToConvert();
                convert.WaitForPageLoad();
                convert.LockQuote();
                convert.IsConvertEnabled().Should().BeTrue("US-CNV-01: quote lock should enable Convert");

                // Receive shortcut lives on Home — leave Convert before tapping it.
                home.GoToHomeTab();
                var receive = home.GoToReceive();
                receive.WaitForPageLoad();
                receive.RefreshQr();
                receive.IsQrVisible().Should().BeTrue("US-RCV-01 / CB-FUND-001: QR visible");
                receive.GetAddress().Should().NotBeNullOrWhiteSpace();
            },
            _fixture);
    }

    /// <summary>
    /// Verifies the US HOM 05 SND 01 Home Chart Convert Pickers Send Ach E2E contract. Use: High. Scope: CoraShellSmokeTests.
    /// </summary>
    [SkippableFact]
    [Trait("Story", StoryIds.UsHom05)]
    [Trait("Story", StoryIds.CbMarket001)]
    [Trait("Story", StoryIds.UsSnd01)]
    public void US_HOM_05_SND_01_HomeChart_ConvertPickers_SendAch()
    {
        Skip.If(_fixture is null, "E2E_RUN not set");

        StoryRunner.Run(
            StoryIds.UsHom05,
            () =>
            {
                var home = UnlockToHome(StoryIds.UsHom05);
                home.HasHideBalancesToggle().Should().BeTrue("hide-balances control is a Home parity surface");
                home.HasChartRangeChips().Should().BeTrue("US-HOM-05 / CB-MARKET-001: range chips");
                home.SelectRange1w();
                home.ToggleHideBalances();

                var convert = home.GoToConvert();
                convert.WaitForPageLoad();
                convert.HasAssetPickers().Should().BeTrue("From/To pickers + amount are Convert parity surfaces");

                var send = home.GoToSendTab();
                send.WaitForPageLoad();
                send.HasParitySurfaces().Should().BeTrue("US-SND-01: ACH payee fields + amount/speed/send");
                send.PageSourceContainsConfigSeedPayees().Should().BeTrue(
                    "US-SND-01: saved payees are config ids seed:rent-4th-st / seed:utilities-co (names, not GUIDs)");
            },
            _fixture);
    }

    /// <summary>
    /// Verifies the US POS 01 CB PAY 003 Pos Lab Simulate E2E contract. Use: High. Scope: CoraShellSmokeTests.
    /// </summary>
    [SkippableFact]
    [Trait("Story", StoryIds.UsPos01)]
    [Trait("Story", StoryIds.CbPay003)]
    public void US_POS_01_CB_PAY_003_PosLabSimulate()
    {
        Skip.If(_fixture is null, "E2E_RUN not set");

        StoryRunner.Run(
            StoryIds.UsPos01,
            () =>
            {
                var home = UnlockToHome(StoryIds.UsPos01);
                ProfilePage profile = home.GoToProfileTab();
                profile.WaitForPageLoad();
                PosLabPage pos = profile.GoToPosLab();
                RequirePosLabReachable(pos);

                pos.StartSession();
                pos.Simulate();
            },
            _fixture);
    }

    /// <summary>
    /// Fails with a gap note when PosLab isn't reachable after Profile → Tap to pay lab.
    /// Use: Medium (POS smoke Fact only). Scope: US-POS-01 / CB-PAY-003.
    /// </summary>
    private static void RequirePosLabReachable(PosLabPage pos)
    {
        try
        {
            pos.WaitForPageLoad();
        }
        catch (WebDriverTimeoutException ex)
        {
            GapNotes.Write(
                StoryIds.UsPos01,
                step: "PosLab landing after Profile → Tap to pay lab",
                expected: "PosLab Simulate button (PosSimulateButton) reachable from Profile",
                actual: $"PosLab AutomationId not found: {ex.Message}",
                proposedFix: "Confirm Profile OpenPosLab navigates to Routes.PosLab and PosSimulateButton is present.");
            throw new InvalidOperationException($"{StoryIds.UsPos01}: PosLab not reachable; gap note written.", ex);
        }
    }

    /// <summary>
    /// From any Shell tab, taps Home → Profile → Lock and returns the Unlock page object when that path works.
    /// Use: High (smoke Fact re-entry). Scope: this test class session.
    /// </summary>
    private static bool TryLockFromProfile(AppiumDriver driver, out UnlockPage unlock)
    {
        unlock = new UnlockPage(driver);
        try
        {
            HomePage home = new HomePage(driver);
            home.GoToHomeTab();
            ProfilePage profile = home.GoToProfileTab();
            profile.WaitForPageLoad();
            unlock = profile.LockApp();
            unlock.WaitForPageLoad();
            return unlock.IsLoaded();
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Terminates and reactivates the MAUI package without pm clear so a sealed wallet should boot to Unlock.
    /// Use: Medium (fallback when Profile→Lock is unavailable). Scope: this test class session.
    /// </summary>
    private static void RelaunchSealedApp(AppiumDriver driver)
    {
        if (driver is not IInteractsWithApps appLifecycle)
        {
            throw new InvalidOperationException(
                $"Driver {driver.GetType().Name} does not support app lifecycle; cannot relaunch sealed smoke without wipe.");
        }

        string package = EmulatorReset.ResolvePackageId();
        appLifecycle.TerminateApp(package);
        appLifecycle.ActivateApp(package);
    }

    /// <summary>
    /// Re-establishes Unlock (prior Facts may leave Convert/Receive/Send), unlocks with the journaled PIN,
    /// and returns Home. Fails with a gap note when Welcome shows (device not sealed) or Unlock cannot be
    /// recovered without wiping the wallet.
    /// Use: High (every sealed-device smoke Fact). Scope: this test class session.
    /// </summary>
    private HomePage UnlockToHome(string storyId)
    {
        UnlockPage unlock = EnsureUnlockScreen(storyId);
        HomePage home = unlock.UnlockWithPin(_fixture!.Journal.Pin);
        home.WaitForPageLoad();
        home.IsLoaded().Should().BeTrue();
        return home;
    }

    /// <summary>
    /// Ensures Unlock is showing before a smoke Fact: already on Unlock, or Profile→Lock from Shell, or a
    /// sealed relaunch (terminate + activate, no pm clear). Welcome is a hard gap for this suite.
    /// Use: High (every sealed-device smoke Fact). Scope: this test class session.
    /// </summary>
    private UnlockPage EnsureUnlockScreen(string storyId)
    {
        AppiumDriver driver = _fixture!.Driver;
        UnlockPage unlock = new UnlockPage(driver);
        if (unlock.IsLoaded())
        {
            return unlock;
        }

        WelcomePage welcome = new WelcomePage(driver);
        StoryGuard.RequireScreen(
            !welcome.IsLoaded(),
            storyId,
            expected: "Unlock screen (sealed-wallet precondition for this smoke suite)",
            actual: "Welcome screen (device is not sealed)",
            proposedFix: "Seal the E2E device first (e.g. run AccountStories.CB_ACCOUNT_001 or --all Fresh→sealed) before CoraShellSmokeTests.");

        if (!TryLockFromProfile(driver, out unlock) || !unlock.IsLoaded())
        {
            RelaunchSealedApp(driver);
            unlock = new UnlockPage(driver);
        }

        StoryGuard.RequireScreen(
            unlock.IsLoaded(),
            storyId,
            expected: "Unlock screen after Profile→Lock or sealed relaunch",
            actual: welcome.IsLoaded() ? "Welcome screen (device is not sealed)" : "neither Unlock nor Welcome visible",
            proposedFix: "Confirm E2E_DEVICE_PROFILE=sealed + noReset, journal PIN exists, and Profile Lock reaches Unlock without wiping custody.");
        return unlock;
    }
}
