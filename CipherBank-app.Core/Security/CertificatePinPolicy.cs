// <copyright file="CertificatePinPolicy.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Security.Cryptography;

namespace CipherBank_app.Security;

/// <summary>Platform-neutral API hostname and SPKI pin policy.</summary>
public static class CertificatePinPolicy
{
    public const string ProductionHost = "api.cipherbank.money";
    public const string SandboxHost = "api.sandbox.cipherbank.money";
    public const string ProductionPin = "sha256/REPLACE_WITH_PRODUCTION_PIN=";
    public const string BackupPin = "sha256/REPLACE_WITH_BACKUP_PIN=";
    public const string SandboxPin = "sha256/REPLACE_WITH_SANDBOX_PIN=";
    public const string SandboxBackupPin = "sha256/REPLACE_WITH_SANDBOX_BACKUP_PIN=";

    public static bool RequiresPinning(string hostname) =>
        HostMatches(hostname, ProductionHost) || HostMatches(hostname, SandboxHost);

    public static bool Matches(string hostname, string computedPin)
    {
        ReadOnlySpan<string> pins = HostMatches(hostname, ProductionHost)
            ? [ProductionPin, BackupPin]
            : HostMatches(hostname, SandboxHost)
                ? [SandboxPin, SandboxBackupPin]
                : [];
        return pins.Contains(computedPin, StringComparer.OrdinalIgnoreCase);
    }

    public static string ComputeSpkiSha256Pin(ReadOnlySpan<byte> subjectPublicKeyInfo)
    {
        byte[] hash = SHA256.HashData(subjectPublicKeyInfo);
        return $"sha256/{Convert.ToBase64String(hash)}";
    }

    private static bool HostMatches(string hostname, string pinnedHost) =>
        string.Equals(hostname, pinnedHost, StringComparison.OrdinalIgnoreCase)
        || hostname.EndsWith("." + pinnedHost, StringComparison.OrdinalIgnoreCase);
}
