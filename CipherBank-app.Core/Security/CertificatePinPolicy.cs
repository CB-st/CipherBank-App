// <copyright file="CertificatePinPolicy.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CipherBank_app.Security;

/// <summary>Platform-neutral API hostname and SPKI pin policy.</summary>
public static class CertificatePinPolicy
{
    public static string ProductionHost { get; } = "api.cipherbank.money";

    public static string SandboxHost { get; } = "api.sandbox.cipherbank.money";

    public static string ProductionPin { get; } = "sha256/REPLACE_WITH_PRODUCTION_PIN=";

    public static string BackupPin { get; } = "sha256/REPLACE_WITH_BACKUP_PIN=";

    public static string SandboxPin { get; } = "sha256/REPLACE_WITH_SANDBOX_PIN=";

    public static string SandboxBackupPin { get; } = "sha256/REPLACE_WITH_SANDBOX_BACKUP_PIN=";

    public static bool RequiresPinning(string hostname) =>
        HostMatches(hostname, ProductionHost) || HostMatches(hostname, SandboxHost);

    public static bool Matches(string hostname, string computedPin)
    {
        if (HostMatches(hostname, ProductionHost))
        {
            return PinMatches(computedPin, ProductionPin, BackupPin);
        }

        if (HostMatches(hostname, SandboxHost))
        {
            return PinMatches(computedPin, SandboxPin, SandboxBackupPin);
        }

        return false;
    }

    public static string ComputeSpkiSha256Pin(ReadOnlySpan<byte> subjectPublicKeyInfo)
    {
        byte[] hash = SHA256.HashData(subjectPublicKeyInfo);
        return $"sha256/{Convert.ToBase64String(hash)}";
    }

    public static bool TryComputeSpkiSha256PinFromCertificateDer(
        ReadOnlySpan<byte> certificateDer,
        [NotNullWhen(true)] out string? pin)
    {
        pin = null;
        if (certificateDer.IsEmpty)
        {
            return false;
        }

        try
        {
            using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(certificateDer);
            return TryComputeSpkiSha256Pin(certificate, out pin);
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public static bool TryComputeSpkiSha256Pin(
        X509Certificate2 certificate,
        [NotNullWhen(true)] out string? pin)
    {
        byte[]? subjectPublicKeyInfo = certificate.GetRSAPublicKey()?.ExportSubjectPublicKeyInfo()
            ?? certificate.GetECDsaPublicKey()?.ExportSubjectPublicKeyInfo();
        if (subjectPublicKeyInfo is null)
        {
            pin = null;
            return false;
        }

        pin = ComputeSpkiSha256Pin(subjectPublicKeyInfo);
        return true;
    }

    private static bool HostMatches(string hostname, string pinnedHost) =>
        string.Equals(hostname, pinnedHost, StringComparison.OrdinalIgnoreCase)
        || hostname.EndsWith("." + pinnedHost, StringComparison.OrdinalIgnoreCase);

    private static bool PinMatches(string computedPin, string primary, string backup) =>
        string.Equals(computedPin, primary, StringComparison.OrdinalIgnoreCase)
        || string.Equals(computedPin, backup, StringComparison.OrdinalIgnoreCase);
}
