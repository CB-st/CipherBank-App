namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Fails a story Fact with a gap note instead of letting an unexpected boot screen surface as a bare
/// Appium timeout, or worse, as a silent soft-return that reports green without checking anything.
/// Use: Medium (device stories with a boot-screen precondition). Scope: any Fact running under E2E_RUN=1.
/// </summary>
public static class StoryGuard
{
    /// <summary>
    /// Writes a gap note and throws when <paramref name="screenLoaded"/> is false, so a wrong boot screen
    /// fails loudly with expected/actual/fix context instead of continuing on stale device state.
    /// Use: Medium. Scope: precondition checks before a story Fact's real assertions run.
    /// </summary>
    public static void RequireScreen(
        bool screenLoaded,
        string storyId,
        string expected,
        string actual,
        string proposedFix)
    {
        if (screenLoaded)
        {
            return;
        }

        GapNotes.Write(storyId, step: "boot-screen precondition", expected, actual, proposedFix);
        throw new InvalidOperationException($"{storyId}: expected {expected} but saw {actual}. Gap note written.");
    }
}
