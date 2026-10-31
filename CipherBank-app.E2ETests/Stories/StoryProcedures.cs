// <copyright file="StoryProcedures.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Stories;

/// <summary>
/// Ordered CB-ACCOUNT-* step id -> human-readable description maps, imported verbatim from the
/// Playwright scaffold (`docs/USER_STORIES.md` procedure lists / `artifacts/story-manifest.json`
/// `steps[].id` + `steps[].action`). Data only: no Playwright runner is ported.
/// <see cref="CipherBank_app.E2ETests.Tests.AccountStories"/> wires the actual Appium steps against
/// these ids as page-object coverage lands (Task 7+).
/// Use: Low (read by reports/backlog tooling, not the hot Appium path). Scope: process-wide story catalog.
/// </summary>
public static class StoryProcedures
{
    /// <summary>
    /// CB-ACCOUNT-001 "Create an account" procedure: open the create-account page, complete the form,
    /// submit, complete the backup/recovery step, and land on an authenticated destination.
    /// Use: Low (catalog/report lookups). Scope: CB-ACCOUNT-001 story definition.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Account001Steps = new Dictionary<string, string>
    {
        ["open"] = "Open the create-account page.",
        ["complete-form"] = "Enter required account information and accept required agreements.",
        ["submit"] = "Submit the account form.",
        ["backup"] = "Complete the recovery-secret or backup step.",
        ["complete"] = "Finish account creation.",
    };

    /// <summary>
    /// CB-ACCOUNT-002 "Recover an account" procedure: open recovery, enter the account identifier and
    /// recovery material, submit, restore the backup, and enter the recovered account.
    /// Use: Low (catalog/report lookups). Scope: CB-ACCOUNT-002 story definition.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Account002Steps = new Dictionary<string, string>
    {
        ["open"] = "Open account recovery.",
        ["enter"] = "Enter account identifier and recovery material.",
        ["submit"] = "Submit recovery.",
        ["restore"] = "Unlock the backup and enroll the device if required.",
        ["complete"] = "Enter the recovered account.",
    };
}
