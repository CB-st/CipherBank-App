namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Writes gap notes under docs/tests/gaps when an E2E story fails under E2E_RUN.
/// Use: Medium (on story failure). Scope: repo docs / next-plan queue.
/// </summary>
public static class GapNotes
{
    private const string GapsRelativePath = "docs/tests/gaps";

    /// <summary>
    /// Writes or overwrites docs/tests/gaps/{storyId}.md with failure context.
    /// Use: Medium. Scope: repo docs.
    /// </summary>
    public static void Write(
        string storyId,
        string step,
        string expected,
        string actual,
        string proposedFix)
    {
        var dir = RepoPaths.ResolveFromRoot(GapsRelativePath);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{storyId}.md");
        File.WriteAllText(path, FormatNote(storyId, step, expected, actual, proposedFix));
    }

    /// <summary>
    /// Builds markdown with story ID, broken step, expected, actual, and proposed fix.
    /// Use: Low. Scope: GapNotes file content.
    /// </summary>
    private static string FormatNote(
        string storyId,
        string step,
        string expected,
        string actual,
        string proposedFix) =>
        $"""
        # {storyId}

        ## Broken step
        {step}

        ## Expected
        {expected}

        ## Actual
        {actual}

        ## Proposed fix
        {proposedFix}
        """;
}
