using CipherBank_app.E2ETests.Support;

namespace CipherBank_app.E2ETests.Stories;

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

/// <param name="RequiredProfile">
/// Device custody profile (<see cref="DeviceProfile"/>) the story's Fact must establish before it runs,
/// or null when no story-specific device precondition applies. Optional/trailing so existing positional
/// <c>new(...)</c> catalog entries stay valid without naming every argument.
/// </param>
public sealed record StoryEntry(
    string CbId,
    string? UsId,
    string Title,
    StoryRunnerStatus Status,
    string MauiSurface,
    DeviceProfile? RequiredProfile = null);

/// <summary>
/// Appium-side catalog. Procedures live in design_handoff USER_STORIES / scaffold catalog;
/// this list drives backlog visibility and Trait filtering.
/// </summary>
public static class StoryCatalog
{
    public static readonly IReadOnlyList<StoryEntry> All =
    [
        new(StoryIds.CbAccount001, StoryIds.UsOnb01, "Create an account", StoryRunnerStatus.Executable,
            "Welcome → Keys → BackupQuiz → SetPin → Home wired; StoryProcedures.Account001Steps journaled " +
            "step-by-step; passed the Task 7 emulator canary on CipherBank_API34 (./scripts/e2e-android.sh " +
            "--story CB-ACCOUNT-001). Onboarding negatives US-ONB-03 (wrong quiz words block BackupQuiz) and " +
            "US-ONB-04 (mismatched PIN confirm blocks SetPin seal) passed the same Task 8 emulator run " +
            "(--story US-ONB-03, --story US-ONB-04)",
            DeviceProfile.Fresh),
        new(StoryIds.CbAccountPinChange, null, "Change unlock PIN", StoryRunnerStatus.Executable,
            "Profile → Security → Change PIN (ChangePinPage; PinChangeCoordinator in Core). " +
            "CB_ACCOUNT_PIN_CHANGE_DynamicPin seals from Fresh with the journaled PIN, changes it to the " +
            "journaled AlternatePin, then proves the replaced PIN is rejected and the new one unlocks; passed " +
            "on CipherBank_API34 (./scripts/e2e-android.sh --story CB-ACCOUNT-PIN-CHANGE). Covers the " +
            "wrong-PIN-error half of US-LCK-02; the lockout-after-N-fails half is still uncovered",
            DeviceProfile.Sealed),
        new(StoryIds.CbAccount002, StoryIds.UsOnb02, "Recover / returning device", StoryRunnerStatus.Backlog,
            "WelcomeReturningButton + RestoreBackup; StoryProcedures.Account002Steps imported, page objects pending",
            DeviceProfile.Fresh),
        new(StoryIds.CbWallet001, "US-WLT-01", "Create user-controlled wallet", StoryRunnerStatus.Backlog, "Home / wallets"),
        new(StoryIds.CbWallet002, "US-WLT-02", "Create CipherBank checking wallet", StoryRunnerStatus.Backlog, "Hybrid checking"),
        new(StoryIds.CbFund001, StoryIds.UsRcv01, "Fund user-controlled wallet", StoryRunnerStatus.Partial,
            "Receive QR + address (deposit confirmations TBD)"),
        new(StoryIds.CbFund002, StoryIds.UsRcv01, "Fund CipherBank checking wallet", StoryRunnerStatus.Backlog, "Receive checking"),
        new(StoryIds.CbCard001, "US-VLT-01", "Create prepaid card from account", StoryRunnerStatus.Backlog, "Profile vault cards"),
        new(StoryIds.CbCard002, null, "Create prepaid card as guest", StoryRunnerStatus.Backlog, "Guest flow N/A on Shell yet"),
        new(StoryIds.CbPay001, StoryIds.UsPay01, "Pay merchant from user wallet", StoryRunnerStatus.Backlog, "Pay tab"),
        new(StoryIds.CbPay002, "US-PAY-02", "Pay merchant from CB checking", StoryRunnerStatus.Backlog, "Pay + hybrid"),
        new(StoryIds.CbPay003, StoryIds.UsPos01, "Pay merchant with prepaid / POS", StoryRunnerStatus.Partial,
            "PosLab Simulate"),
        new(StoryIds.CbMarket001, StoryIds.UsHom05, "View price / chart data", StoryRunnerStatus.Partial,
            "Home range chips + Convert iquote"),
    ];

    public static IEnumerable<StoryEntry> Backlog =>
        All.Where(s => s.Status is StoryRunnerStatus.Backlog or StoryRunnerStatus.Partial);
}
