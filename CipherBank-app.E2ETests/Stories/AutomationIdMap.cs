// <copyright file="AutomationIdMap.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Stories;

/// <summary>
/// Expo RN <c>testID</c> ↔ MAUI <c>AutomationId</c> for shared story automation.
/// Prefer identical strings when adding new controls; this table covers historical drift.
/// </summary>
public static class AutomationIdMap
{
    /// <summary>Maps legacy Expo test identifiers to MAUI automation IDs.</summary>
    public static readonly IReadOnlyDictionary<string, string> ExpoTestIdToMaui = new Dictionary<string, string>
    {
        ["welcome-screen"] = "WelcomePage",
        ["welcome-create"] = "WelcomeCreateWalletButton",
        ["welcome-returning"] = "WelcomeReturningButton",
        ["keys-screen"] = "KeysPage",
        ["keys-continue"] = "KeysContinueButton",
        ["quiz-screen"] = "BackupQuizPage",
        ["quiz-continue"] = "BackupQuizVerifyButton",
        ["set-pin-screen"] = "SetPinPage",
        ["pin-input"] = "SetPinEntry",
        ["pin-confirm"] = "SetPinConfirmEntry",
        ["pin-finish"] = "SetPinSealButton",
        ["home-screen"] = "HomePage",
        ["home-setup-prompt"] = "HomeSetupPrompt",
    };
}
