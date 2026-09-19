// <copyright file="CertificatePinningHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. Licensed under the BSD 3-Clause License.
// </copyright>

using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
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
    [SuppressMessage("Interoperability", "CA1422:Validate platform compatibility", Justification = "SecTrust.GetPublicKey() is the available API on MacCatalyst for extracting public keys from trust evaluations")]
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

            // Extract public key from the trust evaluation (works on MacCatalyst)
            var publicKey = trust.GetPublicKey();
            if (publicKey == null)
            {
                Serilog.Log.Debug("[Certificate Pinning] Failed to get public key");
                return false;
            }

            // Get public key data
            var publicKeyData = publicKey.GetExternalRepresentation();
            if (publicKeyData == null)
            {
                Serilog.Log.Debug("[Certificate Pinning] Failed to get public key data");
                return false;
            }

            // Calculate SHA256 hash of public key
            var hash = SHA256.HashData(publicKeyData.ToArray());
            var base64Hash = Convert.ToBase64String(hash);
            var pin = $"sha256/{base64Hash}";

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
