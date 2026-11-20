// <copyright file="StoryJournal.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Holds in-memory credentials and step log for one E2E run; flushes to disk for diagnosis.
/// Credentials come from ctor args, process env, or a gitignored local env file — not from source literals.
/// Use: High (every device story). Scope: per-test / per-fixture session.
/// </summary>
public sealed class StoryJournal
{
    private readonly List<string> _steps = new();
    private readonly string _dir;

    /// <summary>
    /// Initializes PINs, the recovery-file password and the journal output dir from args, env, or local env file.
    /// Use: High. Scope: per-test / per-fixture session.
    /// </summary>
    public StoryJournal(
        string? pin = null,
        string? alternatePin = null,
        string? dir = null,
        string? recoveryPassword = null)
    {
        E2EHarnessCredentials.Snapshot creds = E2EHarnessCredentials.Resolve(pin, alternatePin, recoveryPassword);
        Pin = creds.Pin;
        AlternatePin = creds.AlternatePin;
        RecoveryPassword = creds.RecoveryPassword;
        _dir = RepoPaths.ResolveFromRoot(
            dir ?? Environment.GetEnvironmentVariable("E2E_JOURNAL_DIR") ?? "artifacts/e2e-journal");
    }

    /// <summary>Gets or sets pin for StoryJournal.</summary>
    public string Pin { get; private set; }

    /// <summary>Gets or sets alternate Pin for StoryJournal.</summary>
    public string AlternatePin { get; private set; }

    /// <summary>
    /// Recovery-file password for backup export/restore stories. Known and journaled on purpose so a device
    /// run is reproducible; it is a synthetic lab value for local/CI diagnosis, never a product secret.
    /// </summary>
    public string RecoveryPassword { get; }

    /// <summary>Gets or sets mnemonic for StoryJournal.</summary>
    public string? Mnemonic { get; private set; }

    /// <summary>Absolute path of the last successful <see cref="Flush"/>, if any.</summary>
    public string? LastFlushPath { get; private set; }

    /// <summary>Stores mnemonic for quiz/PIN flows. Use: Medium. Scope: account stories.</summary>
    public void SetMnemonic(string mnemonic) => Mnemonic = mnemonic;

    /// <summary>Swaps active PIN after a successful change-PIN flow. Use: Medium. Scope: account stories.</summary>
    public void PromoteAlternatePin() => (Pin, AlternatePin) = (AlternatePin, Pin);

    /// <summary>Appends a journal line. Use: High. Scope: per-story session.</summary>
    public void RecordStep(string line) => _steps.Add($"{DateTimeOffset.UtcNow:o} {line}");

    /// <summary>
    /// Writes journal file including PIN/mnemonic for emulator diagnosis under gitignored artifacts/.
    /// Use: High. Scope: process artifacts (debug only — never commit).
    /// </summary>
    public void Flush(string storyId)
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, $"{storyId}.journal.txt");
        File.WriteAllLines(path, new[]
        {
            $"story={storyId}",
            $"pin={Pin}",
            $"altPin={AlternatePin}",
            $"recoveryPassword={RecoveryPassword}",
            $"mnemonic={Mnemonic ?? "(none)"}",
        }.Concat(_steps));
        LastFlushPath = path;
    }
}
