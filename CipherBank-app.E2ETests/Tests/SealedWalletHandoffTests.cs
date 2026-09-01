// <copyright file="SealedWalletHandoffTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.E2ETests.PageObjects;
using CipherBank_app.E2ETests.Support;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Explicit Fresh→seal→lock (or lock-from-Shell) handoff so <c>--all</c> smoke does not depend on
/// xUnit Fact order inside <see cref="AccountStories"/>. US-ONB-03/04 and CB-ACCOUNT-002 can leave Welcome;
/// this collection runs under <c>E2E_DEVICE_PROFILE=sealed</c> so <see cref="AppiumFixture"/> lands on Unlock.
/// Use: High (<c>scripts/e2e-android.sh --all</c> between AccountStories and CoraShellSmokeTests).
/// </summary>
[Collection("E2E Tests")]
public class SealedWalletHandoffTests
{
    private readonly AppiumFixture? _fixture;

    /// <summary>
    /// Receives the shared Appium session from <see cref="AppiumFixtureHolder"/> (null when E2E_RUN is unset).
    /// Use: High (once per test instance). Scope: SealedWalletHandoffTests session.
    /// </summary>
    public SealedWalletHandoffTests(AppiumFixtureHolder holder)
    {
        _fixture = holder.Fixture;
    }

    /// <summary>
    /// Asserts Unlock after fixture bootstrap: already Unlock, Profile→Lock from Home, or Welcome→Fresh→seal→Lock.
    /// Use: High (--all handoff). Scope: this Fact's device session.
    /// </summary>
    [SkippableFact]
    public void EnsureSealedLockedWallet_ColdStartUnlock()
    {
        Skip.If(_fixture is null, "E2E_RUN not set");
        UnlockPage unlock = new UnlockPage(_fixture!.Driver);
        unlock.IsLoaded().Should().BeTrue(
            "sealed handoff must leave UnlockPinEntry before CoraShellSmokeTests (AccountStories Fact order is not a contract)");
        _fixture.Journal.RecordStep("handoff: sealed locked wallet (UnlockPinEntry)");
        _fixture.Journal.Flush("E2E-HANDOFF-SEAL");
    }
}
