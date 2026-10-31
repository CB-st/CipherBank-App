// <copyright file="GapNotes.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using OpenQA.Selenium.Appium;

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Writes gap notes under docs/tests/gaps when an E2E story fails under E2E_RUN.
/// Use: Medium (on story failure). Scope: repo docs / next-plan queue.
/// </summary>
public static class GapNotes
{
    private const string GapsRelativePath = "docs/tests/gaps";
    private const string Redacted = "[redacted]";

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
        var path = Path.Combine(dir, $"{SanitizeFileName(storyId)}.md");
        File.WriteAllText(path, FormatNote(storyId, step, expected, actual, proposedFix));
    }

    /// <summary>
    /// Captures diagnostics for an uncaught Fact exception (page source, journal path, stack) then writes a gap note.
    /// Never swallows the original exception — callers must rethrow. Journal PIN/mnemonic/recovery secrets are
    /// stripped from the markdown so gap notes can live in the repo without device credentials.
    /// Use: High (every executable Fact failure under E2E_RUN=1). Scope: docs/tests/gaps + artifacts/e2e-diagnostics.
    /// </summary>
    public static string Write(
        string storyId,
        Exception exception,
        AppiumDriver? driver,
        StoryJournal? journal,
        string step = "uncaught Fact failure")
    {
        string pageSourcePath = driver is null
            ? "(no Appium driver)"
            : DeviceDiagnostics.CapturePageSource(driver, SanitizeFileName(storyId));
        string journalPath = journal?.LastFlushPath ?? "(journal not flushed)";
        string actual =
            $"""
            Exception: {exception.GetType().FullName}: {exception.Message}

            Stack:
            {exception}

            Page source: {pageSourcePath}
            Journal: {journalPath}
            Appium: {Environment.GetEnvironmentVariable("APPIUM_SERVER_URL") ?? "(unset)"}
            """;

        Write(
            storyId,
            step,
            expected: "Fact completes without throwing under E2E_RUN=1",
            actual: RedactJournalSecrets(actual, journal),
            proposedFix: "Inspect page source + journal; fix selector, timing, or app state for this story.");

        return Path.Combine(
            RepoPaths.ResolveFromRoot(GapsRelativePath),
            $"{SanitizeFileName(storyId)}.md");
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

    /// <summary>
    /// Replaces journaled PIN / alternate PIN / recovery password / mnemonic substrings in gap-note text.
    /// Use: High (exception gap notes). Scope: docs/tests/gaps markdown only.
    /// </summary>
    private static string RedactJournalSecrets(string text, StoryJournal? journal)
    {
        if (journal is null)
        {
            return text;
        }

        text = ReplaceSecret(text, journal.Pin);
        text = ReplaceSecret(text, journal.AlternatePin);
        text = ReplaceSecret(text, journal.RecoveryPassword);
        if (!string.IsNullOrEmpty(journal.Mnemonic))
        {
            text = ReplaceSecret(text, journal.Mnemonic);
        }

        return text;
    }

    /// <summary>
    /// Substitutes a non-empty secret with <see cref="Redacted"/> wherever it appears in <paramref name="text"/>.
    /// Use: High (RedactJournalSecrets). Scope: gap-note string scrubbing.
    /// </summary>
    private static string ReplaceSecret(string text, string? secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            return text;
        }

        return text.Replace(secret, Redacted, StringComparison.Ordinal);
    }

    /// <summary>
    /// Strips path-hostile characters from story IDs used as file names.
    /// Use: Low. Scope: gap note file naming.
    /// </summary>
    private static string SanitizeFileName(string storyId)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            storyId = storyId.Replace(c, '_');
        }

        return storyId;
    }
}
