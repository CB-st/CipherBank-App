// <copyright file="E2EHarnessCredentialsTests.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.E2ETests.Support;
using FluentAssertions;
using Xunit;

namespace CipherBank_app.E2ETests.Tests;

/// <summary>
/// Host-side proofs for harness credential resolution (no emulator).
/// Shares the E2E Tests collection so process-env mutation cannot race Appium Facts.
/// Use: High (CI). Scope: E2EHarnessCredentials.
/// </summary>
[Collection("E2E Tests")]
public class E2EHarnessCredentialsTests
{
    /// <summary>
    /// Proves ctor overrides win without reading process env defaults from source.
    /// Use: High. Scope: Resolve.
    /// </summary>
    [Fact]
    public void Resolve_UsesExplicitOverrides_WithoutEnvDefaults()
    {
        E2EHarnessCredentials.Snapshot snap = E2EHarnessCredentials.Resolve(
            pin: "111111",
            alternatePin: "222222",
            recoveryPassword: "Lab-Recovery-Password");

        snap.Pin.Should().Be("111111");
        snap.AlternatePin.Should().Be("222222");
        snap.RecoveryPassword.Should().Be("Lab-Recovery-Password");
    }

    /// <summary>
    /// Proves missing credentials fail closed with an operator-facing message (no embedded lab secrets).
    /// Use: High. Scope: Resolve failure path.
    /// </summary>
    [Fact]
    public void Resolve_WhenUnset_ThrowsWithSetupGuidance()
    {
        using EnvCredentialScope scope = EnvCredentialScope.Clear();
        using LocalEnvIsolation isolation = LocalEnvIsolation.SuspendLocalFiles();

        Action act = () => E2EHarnessCredentials.Resolve();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*e2e-local.env*")
            .And.Message.Should().Contain(E2EHarnessCredentials.PinEnvName);
        _ = scope;
        _ = isolation;
    }

    /// <summary>
    /// Proves a gitignored-style KEY=VALUE file fills unset env keys.
    /// Use: High. Scope: ApplyLocalEnvFileIfPresent / ParseEnvFile.
    /// </summary>
    [Fact]
    public void ApplyLocalEnvFile_FillsUnsetKeysFromFile()
    {
        using EnvCredentialScope scope = EnvCredentialScope.Clear();
        using LocalEnvIsolation isolation = LocalEnvIsolation.WithArtifactsFile(
            """
            # harness test
            E2E_TEST_PIN=654321
            E2E_TEST_PIN_ALT=987654
            E2E_RECOVERY_PASSWORD=Harness-File-Recovery-Ok
            """);

        E2EHarnessCredentials.Snapshot snap = E2EHarnessCredentials.Resolve();
        snap.Pin.Should().Be("654321");
        snap.AlternatePin.Should().Be("987654");
        snap.RecoveryPassword.Should().Be("Harness-File-Recovery-Ok");
        _ = scope;
        _ = isolation;
    }

    /// <summary>
    /// Clears and restores the three harness credential env vars for the duration of a test.
    /// Use: High (credential Facts). Scope: process env isolation.
    /// </summary>
    private sealed class EnvCredentialScope : IDisposable
    {
        private readonly string? _pin;
        private readonly string? _alt;
        private readonly string? _recovery;

        private EnvCredentialScope(string? pin, string? alt, string? recovery)
        {
            _pin = pin;
            _alt = alt;
            _recovery = recovery;
        }

        public static EnvCredentialScope Clear()
        {
            var scope = new EnvCredentialScope(
                Environment.GetEnvironmentVariable(E2EHarnessCredentials.PinEnvName),
                Environment.GetEnvironmentVariable(E2EHarnessCredentials.AlternatePinEnvName),
                Environment.GetEnvironmentVariable(E2EHarnessCredentials.RecoveryPasswordEnvName));
            Environment.SetEnvironmentVariable(E2EHarnessCredentials.PinEnvName, null);
            Environment.SetEnvironmentVariable(E2EHarnessCredentials.AlternatePinEnvName, null);
            Environment.SetEnvironmentVariable(E2EHarnessCredentials.RecoveryPasswordEnvName, null);
            return scope;
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(E2EHarnessCredentials.PinEnvName, _pin);
            Environment.SetEnvironmentVariable(E2EHarnessCredentials.AlternatePinEnvName, _alt);
            Environment.SetEnvironmentVariable(E2EHarnessCredentials.RecoveryPasswordEnvName, _recovery);
        }
    }

    /// <summary>
    /// Isolates local dotenv files so Resolve tests do not pick up a developer's lab file.
    /// Use: High (credential Facts). Scope: artifacts/e2e-local.env and .env.e2e.local.
    /// </summary>
    private sealed class LocalEnvIsolation : IDisposable
    {
        private readonly List<(string Original, string Backup)> _moved = [];
        private string? _writtenPath;
        private bool _deleteWrittenOnDispose;

        public static LocalEnvIsolation SuspendLocalFiles()
        {
            var isolation = new LocalEnvIsolation();
            isolation.Suspend(RepoPaths.ResolveFromRoot(E2EHarnessCredentials.LocalEnvRelativePath));
            isolation.Suspend(RepoPaths.ResolveFromRoot(E2EHarnessCredentials.DotEnvRelativePath));
            return isolation;
        }

        public static LocalEnvIsolation WithArtifactsFile(string contents)
        {
            LocalEnvIsolation isolation = SuspendLocalFiles();
            string path = RepoPaths.ResolveFromRoot(E2EHarnessCredentials.LocalEnvRelativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            isolation._writtenPath = path;
            isolation._deleteWrittenOnDispose = true;
            File.WriteAllText(path, contents);
            return isolation;
        }

        public void Dispose()
        {
            if (_writtenPath is not null && _deleteWrittenOnDispose && File.Exists(_writtenPath))
            {
                File.Delete(_writtenPath);
            }

            for (int i = _moved.Count - 1; i >= 0; i--)
            {
                (string original, string backup) = _moved[i];
                if (File.Exists(original))
                {
                    File.Delete(original);
                }

                if (File.Exists(backup))
                {
                    File.Move(backup, original);
                }
            }
        }

        private void Suspend(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }

            string backup = path + ".harness-bak";
            File.Move(path, backup, overwrite: true);
            _moved.Add((path, backup));
        }
    }
}
