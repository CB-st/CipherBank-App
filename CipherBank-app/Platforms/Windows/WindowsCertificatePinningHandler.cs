using System;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CipherBank_app.Platforms.Windows;

/// <summary>
/// Windows-specific HTTP handler with certificate pinning support.
/// Validates server certificates against pinned public key hashes.
/// </summary>
public class WindowsCertificatePinningHandler : HttpClientHandler
{
    // ===================================================================================
    // TODO: REPLACE CERTIFICATE PINS BEFORE PRODUCTION DEPLOYMENT
    // ===================================================================================
    // These placeholder pins MUST be replaced with actual certificate pins before release.
    // The app will fail to connect to the API with these placeholder values.
    //
    // To obtain production pins, run the following commands in PowerShell or Git Bash:
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
    private static readonly string[] PinnedPublicKeys = new[]
    {
        "sha256/REPLACE_WITH_PRODUCTION_PIN=",  // Primary pin - MUST be replaced before production
        "sha256/REPLACE_WITH_BACKUP_PIN=",      // Backup pin for certificate rotation
    };

    // Hostnames that require certificate pinning
    private static readonly string[] PinnedHostnames = new[]
    {
        "api.cipherbank.money",
        "api.sandbox.cipherbank.money"
    };

    public WindowsCertificatePinningHandler()
    {
        ServerCertificateCustomValidationCallback = ValidateServerCertificate;
    }

    private bool ValidateServerCertificate(
        HttpRequestMessage request,
        X509Certificate2? certificate,
        X509Chain? chain,
        SslPolicyErrors sslPolicyErrors)
    {
        // Get the hostname from the request
        var hostname = request.RequestUri?.Host ?? "";

        // Check if this hostname requires pinning
        if (!PinnedHostnames.Any(h => hostname.Equals(h, StringComparison.OrdinalIgnoreCase)))
        {
            // Not a pinned host, use standard validation
            return sslPolicyErrors == SslPolicyErrors.None;
        }

        // For pinned hosts, require valid certificate chain
        if (sslPolicyErrors != SslPolicyErrors.None)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Certificate Pinning] SSL errors for {hostname}: {sslPolicyErrors}");
#endif
            return false;
        }

        if (certificate == null)
        {
#if DEBUG
            System.Diagnostics.Debug.WriteLine($"[Certificate Pinning] No certificate for {hostname}");
#endif
            return false;
        }

        // Validate certificate pinning
        return ValidateCertificatePin(certificate, hostname);
    }

    private static bool ValidateCertificatePin(X509Certificate2 certificate, string hostname)
    {
        try
        {
            // Get the public key
            var publicKey = certificate.GetPublicKey();
            if (publicKey == null || publicKey.Length == 0)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("[Certificate Pinning] Failed to get public key");
#endif
                return false;
            }

            // For RSA keys, we need to export the SubjectPublicKeyInfo
            using var rsa = certificate.GetRSAPublicKey();
            if (rsa != null)
            {
                var spki = rsa.ExportSubjectPublicKeyInfo();
                var hash = SHA256.HashData(spki);
                var base64Hash = Convert.ToBase64String(hash);
                var pin = $"sha256/{base64Hash}";

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
                System.Diagnostics.Debug.WriteLine($"[Certificate Pinning] Pin mismatch for {hostname}");
#endif
                return false;
            }

            // For ECDSA keys
            using var ecdsa = certificate.GetECDsaPublicKey();
            if (ecdsa != null)
            {
                var spki = ecdsa.ExportSubjectPublicKeyInfo();
                var hash = SHA256.HashData(spki);
                var base64Hash = Convert.ToBase64String(hash);
                var pin = $"sha256/{base64Hash}";

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
                System.Diagnostics.Debug.WriteLine($"[Certificate Pinning] Pin mismatch for {hostname}");
#endif
                return false;
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine("[Certificate Pinning] Unsupported key type");
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
}
