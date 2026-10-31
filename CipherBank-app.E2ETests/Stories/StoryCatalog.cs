// <copyright file="StoryCatalog.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.E2ETests.Support;

namespace CipherBank_app.E2ETests.Stories;

/// <summary>
/// Appium-side catalog. Procedures follow the Draw.io / scaffold CB-* catalog;
/// this list drives backlog visibility and Trait filtering.
/// Use: High (backlog tests + wave planning). Scope: E2E story inventory.
/// </summary>
public static class StoryCatalog
{
    /// <summary>Contains the complete M4 story catalog.</summary>
    public static readonly IReadOnlyList<StoryEntry> All =
    [
        new(
            StoryIds.CbAccount001,
            StoryIds.UsOnb01,
            "Create an account",
            StoryRunnerStatus.Executable,
            CatalogNotes.Account001,
            DeviceProfile.Fresh),
        new(
            StoryIds.CbAccountPinChange,
            null,
            "Change unlock PIN",
            StoryRunnerStatus.Executable,
            CatalogNotes.AccountPinChange,
            DeviceProfile.Sealed),
        new(
            StoryIds.CbAccount002,
            StoryIds.UsOnb02,
            "Recover / returning device",
            StoryRunnerStatus.Executable,
            CatalogNotes.Account002,
            DeviceProfile.Fresh),
        new(
            StoryIds.CbWallet001,
            "US-WLT-01",
            "Create user-controlled wallet",
            StoryRunnerStatus.Backlog,
            "Home / wallets"),
        new(
            StoryIds.CbWallet002,
            "US-WLT-02",
            "Create CipherBank checking wallet",
            StoryRunnerStatus.Backlog,
            "Hybrid checking"),
        new(
            StoryIds.CbFund001,
            StoryIds.UsRcv01,
            "Fund user-controlled wallet",
            StoryRunnerStatus.Partial,
            "Receive QR + address (deposit confirmations TBD)"),
        new(
            StoryIds.CbFund002,
            StoryIds.UsRcv01,
            "Fund CipherBank checking wallet",
            StoryRunnerStatus.Backlog,
            "Receive checking"),
        new(
            StoryIds.CbCard001,
            "US-VLT-01",
            "Create prepaid card from account",
            StoryRunnerStatus.Backlog,
            "Profile vault cards"),
        new(
            StoryIds.CbCard002,
            null,
            "Create prepaid card as guest",
            StoryRunnerStatus.Backlog,
            "Guest flow N/A on Shell yet"),
        new(
            StoryIds.CbPay001,
            StoryIds.UsPay01,
            "Pay merchant from user wallet",
            StoryRunnerStatus.Backlog,
            "Pay tab"),
        new(
            StoryIds.CbPay002,
            "US-PAY-02",
            "Pay merchant from CB checking",
            StoryRunnerStatus.Backlog,
            "Pay + hybrid"),
        new(
            StoryIds.CbPay003,
            StoryIds.UsPos01,
            "Pay merchant with prepaid / POS",
            StoryRunnerStatus.Partial,
            "PosLab Simulate"),
        new(
            StoryIds.CbMarket001,
            StoryIds.UsHom05,
            "View price / chart data",
            StoryRunnerStatus.Partial,
            "Home range chips + Convert iquote"),
    ];

    /// <summary>
    /// Entries still Partial or Backlog (not yet full executable Facts).
    /// Use: Medium (StoryBacklogTests). Scope: catalog filter.
    /// </summary>
    public static IEnumerable<StoryEntry> Backlog =>
        All.Where(s => s.Status is StoryRunnerStatus.Backlog or StoryRunnerStatus.Partial);

    /// <summary>
    /// Long executable-story notes kept nested so <see cref="All"/> initializer args stay single-line (SA1118).
    /// Use: Low (catalog construction). Scope: StoryCatalog.
    /// </summary>
    private static class CatalogNotes
    {
        /// <summary>Describes the executable create-account and negative-onboarding coverage.</summary>
        public const string Account001 =
            "Welcome → Keys → BackupQuiz → SetPin → Home wired; StoryProcedures.Account001Steps journaled "
            + "step-by-step; passed the Task 7 emulator canary on CipherBank_API34 (./scripts/e2e-android.sh "
            + "--story CB-ACCOUNT-001). Onboarding negatives US-ONB-03 (wrong quiz words block BackupQuiz) and "
            + "US-ONB-04 (mismatched PIN confirm blocks SetPin seal) passed the same Task 8 emulator run "
            + "(--story US-ONB-03, --story US-ONB-04)";

        /// <summary>Describes the executable PIN-change and replaced-PIN rejection coverage.</summary>
        public const string AccountPinChange =
            "Profile → Security → Change PIN (ChangePinPage; PinChangeCoordinator in Core). "
            + "CB_ACCOUNT_PIN_CHANGE_DynamicPin seals from Fresh with the journaled PIN, changes it to the "
            + "journaled AlternatePin, then proves the replaced PIN is rejected and the new one unlocks; passed "
            + "on CipherBank_API34 (./scripts/e2e-android.sh --story CB-ACCOUNT-PIN-CHANGE). Covers the "
            + "wrong-PIN-error half of US-LCK-02; the lockout-after-N-fails half is still uncovered";

        /// <summary>Describes the executable export, wipe, restore, and custody-equivalence coverage.</summary>
        public const string Account002 =
            "CB_ACCOUNT_002_RecoverAccount seals a wallet, exports a real ciphered recovery file through "
            + "Profile → Backup recovery file (Core IMnemonicBackupService, saved to the device's Downloads "
            + "collection), wipes the device, then restores through Welcome → Restore from backup → Android's "
            + "own document picker → recovery password → SetPin → Home, journaling every "
            + "StoryProcedures.Account002Steps id. A wrong recovery password is rejected first, and same-custody "
            + "is proven by revealing the phrase on the recovered wallet (Profile → Vault) and comparing it with "
            + "the pre-wipe phrase; passed on CipherBank_API34 (./scripts/e2e-android.sh --story CB-ACCOUNT-002)";
    }
}
