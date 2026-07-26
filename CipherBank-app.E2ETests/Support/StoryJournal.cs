namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Holds in-memory credentials and step log for one E2E run; flushes to disk for diagnosis.
/// Use: High (every device story). Scope: per-test / per-fixture session.
/// </summary>
public sealed class StoryJournal
{
    public string Pin { get; private set; }
    public string AlternatePin { get; private set; }
    public string? Mnemonic { get; private set; }
    private readonly List<string> _steps = new();
    private readonly string _dir;

    /// <summary>
    /// Initializes PINs and journal output dir from args or E2E env vars.
    /// Use: High. Scope: per-test / per-fixture session.
    /// </summary>
    public StoryJournal(string? pin = null, string? alternatePin = null, string? dir = null)
    {
        Pin = pin ?? Environment.GetEnvironmentVariable("E2E_TEST_PIN") ?? "246810";
        AlternatePin = alternatePin
            ?? Environment.GetEnvironmentVariable("E2E_TEST_PIN_ALT")
            ?? "135791";
        _dir = RepoPaths.ResolveFromRoot(
            dir ?? Environment.GetEnvironmentVariable("E2E_JOURNAL_DIR") ?? "artifacts/e2e-journal");
    }

    /// <summary>Stores mnemonic for quiz/PIN flows. Use: Medium. Scope: account stories.</summary>
    public void SetMnemonic(string mnemonic) => Mnemonic = mnemonic;

    /// <summary>Swaps active PIN after a successful change-PIN flow. Use: Medium. Scope: account stories.</summary>
    public void PromoteAlternatePin() => (Pin, AlternatePin) = (AlternatePin, Pin);

    /// <summary>Appends a journal line. Use: High. Scope: per-story session.</summary>
    public void RecordStep(string line) => _steps.Add($"{DateTimeOffset.UtcNow:o} {line}");

    /// <summary>Writes journal file including PIN/mnemonic for emulator diagnosis. Use: High. Scope: process artifacts.</summary>
    public void Flush(string storyId)
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, $"{storyId}.journal.txt");
        File.WriteAllLines(path, new[]
        {
            $"story={storyId}",
            $"pin={Pin}",
            $"altPin={AlternatePin}",
            $"mnemonic={Mnemonic ?? "(none)"}",
        }.Concat(_steps));
    }
}
