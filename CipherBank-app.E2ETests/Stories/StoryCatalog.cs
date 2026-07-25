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

public sealed record StoryEntry(
    string CbId,
    string? UsId,
    string Title,
    StoryRunnerStatus Status,
    string MauiSurface);

/// <summary>
/// Appium-side catalog. Procedures live in design_handoff USER_STORIES / scaffold catalog;
/// this list drives backlog visibility and Trait filtering.
/// </summary>
public static class StoryCatalog
{
    public static readonly IReadOnlyList<StoryEntry> All =
    [
        new(StoryIds.CbAccount001, StoryIds.UsOnb01, "Create an account", StoryRunnerStatus.Partial,
            "Welcome → Keys → BackupQuiz → SetPin → Home wired (run with E2E_FRESH=1)"),
        new(StoryIds.CbAccount002, StoryIds.UsOnb02, "Recover / returning device", StoryRunnerStatus.Backlog,
            "WelcomeReturningButton + RestoreBackup"),
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
