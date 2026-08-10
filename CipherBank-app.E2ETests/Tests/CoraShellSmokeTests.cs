// <copyright file="CoraShellSmokeTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.E2ETests.PageObjects;
using CipherBank_app.E2ETests.Stories;
using CipherBank_app.E2ETests.Support;
using FluentAssertions;
using OpenQA.Selenium;
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
                UnlockToHome(StoryIds.UsPos01);

                var pos = new PosLabPage(_fixture!.Driver);
                RequirePosLabReachable(pos);

                pos.StartSession();
                pos.Simulate();
            },
            _fixture);
    }

    /// <summary>
    /// Fails with a gap note when PosLab isn't reachable from Home, instead of the previous silent
    /// soft-return — POS navigation from Home is not yet modeled in the page objects.
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
                step: "PosLab landing after Home unlock",
                expected: "PosLab Simulate button (PosSimulateButton) reachable from Home",
                actual: $"PosLab AutomationId not found: {ex.Message}",
                proposedFix: "Add a HomePage.GoToPosLab() route (or equivalent Shell navigation) once the POS entry point is confirmed.");
            throw new InvalidOperationException($"{StoryIds.UsPos01}: PosLab not reachable; gap note written.", ex);
        }
    }

    /// <summary>
    /// Unlocks the sealed device with the journaled PIN and returns Home; fails with a gap note (instead of
    /// a bare Appium timeout) when Welcome shows up instead of Unlock, i.e. the device isn't sealed as this
    /// smoke suite requires.
    /// Use: High (every sealed-device smoke Fact). Scope: this test class session.
    /// </summary>
    private HomePage UnlockToHome(string storyId)
    {
        var driver = _fixture!.Driver;
        var unlock = new UnlockPage(driver);
        var welcome = new WelcomePage(driver);
        StoryGuard.RequireScreen(
            unlock.IsLoaded(),
            storyId,
            expected: "Unlock screen (sealed-wallet precondition for this smoke suite)",
            actual: welcome.IsLoaded() ? "Welcome screen (device is not sealed)" : "neither Unlock nor Welcome visible",
            proposedFix: "Seal the E2E device first (e.g. run AccountStories.CB_ACCOUNT_001 or DeviceState.SealedAsync) before running CoraShellSmokeTests.");

        var home = unlock.UnlockWithPin(_fixture.Journal.Pin);
        home.WaitForPageLoad();
        home.IsLoaded().Should().BeTrue();
        return home;
    }
}
