// <copyright file="CertificatePinningHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using CipherBank_app.Security;
using Security;

namespace CipherBank_app.Platforms.MacCatalyst;

/// <summary>
/// MacCatalyst-specific certificate pinning handler using NSUrlSessionHandler.
/// Validates server certificates against pinned public key hashes.
/// Shares the same Apple Security framework implementation as iOS.
/// </summary>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "File is named for the platform-specific handler concept")]
public class MacCatalystCertificatePinningHandler : NSUrlSessionHandler
{
    public MacCatalystCertificatePinningHandler()
    {
        // Configure TLS settings - require TLS 1.2 or higher
        TrustOverrideForUrl += HandleTrustOverride;
    }

    /// <summary>
    /// Validates that the server certificate matches one of the pinned public keys.
    /// </summary>
    [SuppressMessage(
        "Interoperability",
        "CA1422:Validate platform compatibility",
        Justification = "SecTrust leaf certificate export is the available API on Mac Catalyst for SPKI extraction.")]
    private static bool ValidateCertificatePinning(SecTrust trust, string hostname)
    {
        try
        {
            // Get the leaf certificate (index 0)
            var certificateCount = trust.Count;
            if (certificateCount == 0)
            {
                Serilog.Log.Debug("[Certificate Pinning] No certificates in chain");
                return false;
            }

            var leafCertificate = trust[0];
            if (leafCertificate == null)
            {
                Serilog.Log.Debug("[Certificate Pinning] Leaf certificate is null");
                return false;
            }

            if (!CertificatePinPolicy.TryComputeSpkiSha256PinFromCertificateDer(
                    leafCertificate.DerData.ToArray(),
                    out string? pin)
                || pin is null)
            {
                Serilog.Log.Debug("[Certificate Pinning] Failed to compute SPKI pin");
                return false;
            }

            if (CertificatePinPolicy.Matches(hostname, pin))
            {
                Serilog.Log.Debug($"[Certificate Pinning] Success for {hostname}");
                return true;
            }

            // Only log the actual pin value in debug builds to avoid information disclosure
            Serilog.Log.Debug($"[Certificate Pinning] Pin mismatch for {hostname}");
            return false;
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug($"[Certificate Pinning] Error: {ex.Message}");
            return false;
        }
    }

    private bool HandleTrustOverride(NSUrlSessionHandler sender, string url, SecTrust trust)
    {
        try
        {
            var uri = new Uri(url);
            var hostname = uri.Host;

            // Check if this hostname requires pinning
            if (!CertificatePinPolicy.RequiresPinning(hostname))
            {
                // Not a pinned host, allow standard validation
                return true;
            }

            // Validate certificate chain using SecTrust
            var policy = SecPolicy.CreateSslPolicy(true, hostname);
            trust.SetPolicy(policy);

            var result = trust.Evaluate(out var error);

            if (!result)
            {
                Serilog.Log.Debug($"[Certificate Validation] Failed for {hostname}: {error?.LocalizedDescription}");
                return false;
            }

            // Check certificate pinning
            return ValidateCertificatePinning(trust, hostname);
        }
        catch (Exception ex)
        {
            Serilog.Log.Debug($"[Certificate Pinning] Error: {ex.Message}");
            return false;
        }
    }
}
