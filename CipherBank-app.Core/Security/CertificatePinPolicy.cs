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
    /// <summary>
    /// HPKP-style prefix used by the C# pin comparison. Android network security
    /// config stores the base64 digest only and puts the algorithm on the pin element.
    /// </summary>
    public static string SpkiSha256Prefix { get; } = "sha256/";

    /// <summary>Marker that means a pin has not been replaced with a real SPKI hash.</summary>
    public static string PlaceholderMarker { get; } = "REPLACE_WITH_";

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
        return string.Concat(SpkiSha256Prefix, Convert.ToBase64String(hash));
    }

    /// <summary>Returns true when <paramref name="pin"/> still contains <see cref="PlaceholderMarker"/>.</summary>
    public static bool IsPlaceholderPin(string pin)
    {
        ArgumentNullException.ThrowIfNull(pin);
        return pin.Contains(PlaceholderMarker, StringComparison.Ordinal);
    }

    /// <summary>
    /// Fails when any configured pin still contains <see cref="PlaceholderMarker"/>.
    /// Release startup calls this so a build cannot ship with placeholder pins.
    /// </summary>
    public static void EnsureReleasePinsAreConfigured() =>
        EnsurePinsAreNotPlaceholders(ProductionPin, BackupPin, SandboxPin, SandboxBackupPin);

    /// <summary>Fails when any supplied pin still contains <see cref="PlaceholderMarker"/>.</summary>
    public static void EnsurePinsAreNotPlaceholders(params string[] pins)
    {
        ArgumentNullException.ThrowIfNull(pins);
        if (Array.Exists(pins, IsPlaceholderPin))
        {
            throw new InvalidOperationException(
                "Certificate pin configuration contains a REPLACE_WITH_ placeholder. Replace the production and sandbox pins before shipping a Release build.");
        }
    }

    /// <summary>
    /// Converts a C# <c>sha256/</c> pin to the base64 digest Android's
    /// <c>network_security_config.xml</c> pin element expects.
    /// </summary>
    public static string ToAndroidNetworkSecurityPin(string policyPin)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyPin);
        if (!policyPin.StartsWith(SpkiSha256Prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "C# SPKI pins must start with sha256/. Android network_security_config.xml stores only the base64 digest because digest=\"SHA-256\" already names the algorithm.",
                nameof(policyPin));
        }

        return policyPin[SpkiSha256Prefix.Length..];
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
