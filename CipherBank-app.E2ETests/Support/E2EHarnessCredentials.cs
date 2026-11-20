// <copyright file="E2EHarnessCredentials.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Resolves synthetic emulator PIN / recovery-password values from the environment or a gitignored
/// local env file — never from committed source literals.
/// Use: High (StoryJournal / AppiumFixture construction). Scope: E2E harness process.
/// </summary>
public static class E2EHarnessCredentials
{
    public const string PinEnvName = "E2E_TEST_PIN";
    public const string AlternatePinEnvName = "E2E_TEST_PIN_ALT";
    public const string RecoveryPasswordEnvName = "E2E_RECOVERY_PASSWORD";

    /// <summary>Gitignored lab file under artifacts/ (preferred local source).</summary>
    public const string LocalEnvRelativePath = "artifacts/e2e-local.env";

    /// <summary>Committed template operators copy into the gitignored local file.</summary>
    public const string ExampleEnvRelativePath = "docs/tests/e2e-local.env.example";

    /// <summary>
    /// Loads KEY=VALUE pairs from a local env file into the process environment when the key is unset.
    /// Does not override existing environment variables.
    /// Use: High (once before resolving credentials). Scope: process env.
    /// </summary>
    public static void ApplyLocalEnvFileIfPresent()
    {
        string? path = FindFirstExistingLocalEnvPath();
        if (path is null)
        {
            return;
        }

        foreach (KeyValuePair<string, string> pair in ParseEnvFile(path))
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(pair.Key)))
            {
                Environment.SetEnvironmentVariable(pair.Key, pair.Value);
            }
        }
    }

    /// <summary>
    /// Resolves PIN / alt PIN / recovery password from ctor overrides, process env, then local env file.
    /// Throws when any required value is still missing so device runs fail closed without embedded secrets.
    /// Use: High (StoryJournal). Scope: harness credentials.
    /// </summary>
    public static Snapshot Resolve(string? pin = null, string? alternatePin = null, string? recoveryPassword = null)
    {
        ApplyLocalEnvFileIfPresent();

        string resolvedPin = FirstNonEmpty(pin, Environment.GetEnvironmentVariable(PinEnvName));
        string resolvedAlt = FirstNonEmpty(alternatePin, Environment.GetEnvironmentVariable(AlternatePinEnvName));
        string resolvedRecovery = FirstNonEmpty(
            recoveryPassword,
            Environment.GetEnvironmentVariable(RecoveryPasswordEnvName));

        if (string.IsNullOrEmpty(resolvedPin)
            || string.IsNullOrEmpty(resolvedAlt)
            || string.IsNullOrEmpty(resolvedRecovery))
        {
            throw new InvalidOperationException(MissingCredentialsMessage());
        }

        return new Snapshot(resolvedPin, resolvedAlt, resolvedRecovery);
    }

    /// <summary>
    /// Builds the operator-facing error when harness credentials are not configured.
    /// Use: Low (Resolve failure). Scope: exception text.
    /// </summary>
    public static string MissingCredentialsMessage()
    {
        string example = RepoPaths.ResolveFromRoot(ExampleEnvRelativePath);
        string local = RepoPaths.ResolveFromRoot(LocalEnvRelativePath);
        return
            "E2E harness credentials are not configured. "
            + $"Export {PinEnvName}, {AlternatePinEnvName}, and {RecoveryPasswordEnvName}, "
            + $"or copy {example} to {local} (gitignored) and fill the lab values. "
            + "See docs/tests/e2e-tests.md § Harness credentials.";
    }

    /// <summary>
    /// Finds artifacts/e2e-local.env when present.
    /// Use: Medium (ApplyLocalEnvFileIfPresent). Scope: path lookup.
    /// </summary>
    private static string? FindFirstExistingLocalEnvPath()
    {
        string[] candidates =
        [
            RepoPaths.ResolveFromRoot(LocalEnvRelativePath),
        ];
        return Array.Find(candidates, File.Exists);
    }

    /// <summary>
    /// Parses a simple KEY=VALUE dotenv file (ignores blanks and # comments).
    /// Use: Medium (ApplyLocalEnvFileIfPresent). Scope: file parse.
    /// </summary>
    private static IEnumerable<KeyValuePair<string, string>> ParseEnvFile(string path)
    {
        foreach (string raw in File.ReadLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            int eq = line.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            string key = line[..eq].Trim();
            string value = line[(eq + 1)..].Trim();
            if (value.Length >= 2
                && ((value.StartsWith('"') && value.EndsWith('"'))
                    || (value.StartsWith('\'') && value.EndsWith('\''))))
            {
                value = value[1..^1];
            }

            if (key.Length > 0)
            {
                yield return new KeyValuePair<string, string>(key, value);
            }
        }
    }

    /// <summary>
    /// Returns the first non-null/non-whitespace string among the candidates.
    /// Use: High (Resolve). Scope: credential coalesce.
    /// </summary>
    private static string FirstNonEmpty(params string?[] candidates)
    {
        foreach (string? candidate in candidates)
        {
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate.Trim();
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Resolved harness credentials for one StoryJournal session.
    /// Use: High (StoryJournal ctor). Scope: immutable snapshot.
    /// </summary>
    public sealed record Snapshot(string Pin, string AlternatePin, string RecoveryPassword);
}
