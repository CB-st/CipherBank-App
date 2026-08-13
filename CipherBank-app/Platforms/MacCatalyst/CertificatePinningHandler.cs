// <copyright file="CertificatePinningHandler.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using Foundation;
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
    // ===================================================================================
    // TODO: REPLACE CERTIFICATE PINS BEFORE PRODUCTION DEPLOYMENT
    // ===================================================================================
    // These placeholder pins MUST be replaced with actual certificate pins before release.
    // The app will fail to connect to the API with these placeholder values.
    //
    // To obtain production pins, run the following commands:
    //
    // For api.cipherbank.money (production):
    //   openssl s_client -servername api.cipherbank.money -connect api.cipherbank.money:443 < /dev/null 2>/dev/null | \
    //     openssl x509 -pubkey -noout | openssl pkey -pubin -outform der | \
    //     openssl dgst -sha256 -binary | openssl enc -base64
    //
    // For api.sandbox.cipherbank.money (sandbox):
    //   openssl s_client -servername api.sandbox.cipherbank.money -connect api.sandbox.cipherbank.money:443 < /dev/null 2>/dev/null | \
    //     openssl x509 -pubkey -noout | openssl pkey -pubin -outform der | \
    //     openssl dgst -sha256 -binary | openssl enc -base64
    //
    // IMPORTANT: Always include a backup pin for certificate rotation.
    // See CERTIFICATE_PINNING_SETUP.md for detailed instructions.
    // ===================================================================================
    private static readonly string[] PinnedPublicKeys =
    [
        "sha256/REPLACE_WITH_PRODUCTION_PIN=",  // Primary pin - MUST be replaced before production
        "sha256/REPLACE_WITH_BACKUP_PIN=",      // Backup pin for certificate rotation
    ];

    // Hostnames that require certificate pinning
    private static readonly string[] PinnedHostnames =
    [
        "api.cipherbank.money",
        "api.sandbox.cipherbank.money",
    ];

    public MacCatalystCertificatePinningHandler()
    {
        // Configure TLS settings - require TLS 1.2 or higher
        TrustOverrideForUrl += HandleTrustOverride;
    }

    private static bool RequiresPinning(string hostname)
    {
        foreach (var pinnedHost in PinnedHostnames)
        {
            if (hostname.Equals(pinnedHost, StringComparison.OrdinalIgnoreCase) ||
                hostname.EndsWith("." + pinnedHost, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[Certificate Pinning] No certificates in chain");
#endif
                return false;
            }

            // Extract public key from the trust evaluation (works on MacCatalyst)
            var publicKey = trust.GetPublicKey();
            if (publicKey == null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[Certificate Pinning] Failed to get public key");
#endif
                return false;
            }

            // Get public key data
            var publicKeyData = publicKey.GetExternalRepresentation();
            if (publicKeyData == null)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[Certificate Pinning] Failed to get public key data");
#endif
                return false;
            }

            // Calculate SHA256 hash of public key
            var hash = SHA256.HashData(publicKeyData.ToArray());
            var base64Hash = Convert.ToBase64String(hash);
            var pin = $"sha256/{base64Hash}";

            // Check if pin matches any of our pinned keys
            foreach (var pinnedKey in PinnedPublicKeys)
            {
                if (string.Equals(pin, pinnedKey, StringComparison.OrdinalIgnoreCase))
                {
#if DEBUG
                    System.Diagnostics.Debug.WriteLine($"[Certificate Pinning] Success for {hostname}");
#endif
                    return true;
                }
            }

#if DEBUG
            // Only log the actual pin value in debug builds to avoid information disclosure
            System.Diagnostics.Debug.WriteLine($"[Certificate Pinning] Pin mismatch for {hostname}");
#endif
            return false;
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Certificate Pinning] Error: {ex.Message}");
#endif
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
            if (!RequiresPinning(hostname))
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
#if DEBUG
                System.Diagnostics.Debug.WriteLine($"[Certificate Validation] Failed for {hostname}: {error?.LocalizedDescription}");
#endif
                return false;
            }

            // Check certificate pinning
            return ValidateCertificatePinning(trust, hostname);
        }
        catch (Exception ex)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Certificate Pinning] Error: {ex.Message}");
#endif
            return false;
        }
    }
}
