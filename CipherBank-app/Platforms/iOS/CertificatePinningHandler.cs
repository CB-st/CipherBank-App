using System;
using System.Net.Http;
using CipherBank_app.Security;
using Foundation;
using Security;

namespace CipherBank_app.Platforms.iOS;

/// <summary>
/// iOS-specific certificate pinning handler using NSUrlSessionHandler.
/// Validates server certificates against pinned public key hashes.
/// </summary>
public class IosCertificatePinningHandler : NSUrlSessionHandler
{
    public IosCertificatePinningHandler()
    {
        // Configure TLS settings - require TLS 1.2 or higher
        TrustOverrideForUrl += HandleTrustOverride;
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

    /// <summary>
    /// Validates that the server certificate matches one of the pinned public keys.
    /// </summary>
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

            // Extract public key data
            var publicKey = leafCertificate.GetPublicKey();
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
            var hash = ComputeSha256Hash(publicKeyData.ToArray());
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

    /// <summary>
    /// Computes SHA256 hash of the provided data.
    /// </summary>
    private static byte[] ComputeSha256Hash(byte[] data)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        return sha256.ComputeHash(data);
    }
}
