// <copyright file="StoryRunnerStatus.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Stories;

/// <summary>
/// How far an Appium catalog entry has progressed toward an executable Fact.
/// Use: Medium (catalog / backlog filters). Scope: StoryCatalog entries.
/// </summary>
public enum StoryRunnerStatus
{
    /// <summary>Covered by an executable Appium fact (needs E2E_RUN=1 + device).</summary>
    Executable,

    /// <summary>AutomationIds / page objects exist or are partial; full flow not yet asserted.</summary>
    Partial,

    /// <summary>Tracked for MAUI once Expo parity lands; skipped in CI.</summary>
    Backlog,

    /// <summary>Source diagram blank or not applicable.</summary>
    Skipped,
}
