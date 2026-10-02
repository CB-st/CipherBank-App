# Certificate Pinning Setup Guide

This guide explains how to configure certificate pinning for the CipherBank mobile app. Certificate pinning is a security technique that helps prevent man-in-the-middle (MITM) attacks by validating that the server's certificate matches a known, trusted certificate.

Shared hostname and C# pin matching live in
`CipherBank-app.Core/Security/CertificatePinPolicy.cs`. iOS
(`Platforms/iOS/IosCertificatePinningHandler.cs`), Mac Catalyst
(`Platforms/MacCatalyst/CertificatePinningHandler.cs`, class
`MacCatalystCertificatePinningHandler`), and Windows
(`Platforms/Windows/WindowsCertificatePinningHandler.cs`) call that type.
Those pins use the `sha256/` prefix plus the base64 SHA-256 of the
SubjectPublicKeyInfo.

Android does not read `CertificatePinPolicy`. `AndroidCertificatePinningHandler`
is a default `HttpClientHandler`. Pinning is enforced by
`CipherBank-app/Platforms/Android/Resources/xml/network_security_config.xml`,
which `Platforms/Android/AndroidManifest.xml` references as
`@xml/network_security_config`. Android pin text is the base64 digest only.
The `digest="SHA-256"` attribute names the algorithm. Omitting `sha256/` is
the Android network security config format: adding the prefix to the XML
would not match. Each XML pin body must equal the matching
`CertificatePinPolicy` pin with the `sha256/` prefix removed
(`CertificatePinPolicy.ToAndroidNetworkSecurityPin`). Update both sources
together.

`MauiProgram` calls `CertificatePinPolicy.EnsureReleasePinsAreConfigured()`
when the assembly configuration is not Debug. A Release start throws
`InvalidOperationException` if any configured pin still contains
`REPLACE_WITH_`. Debug startup leaves the placeholders in place so local
development can run.

## Overview

The CipherBank app implements certificate pinning on all platforms:
- **iOS**: `Platforms/iOS/IosCertificatePinningHandler.cs` (uses `CertificatePinPolicy`)
- **Mac Catalyst**: `Platforms/MacCatalyst/CertificatePinningHandler.cs` (uses `CertificatePinPolicy`)
- **Android**: `Platforms/Android/Resources/xml/network_security_config.xml` (base64 digests, no `sha256/` prefix)
- **Windows**: `Platforms/Windows/WindowsCertificatePinningHandler.cs` (uses `CertificatePinPolicy`)
- **Shared pins**: `CipherBank-app.Core/Security/CertificatePinPolicy.cs`

## Prerequisites

- OpenSSL installed on your system
- Access to the production and sandbox API endpoints
- Network access to retrieve certificates

## Obtaining Certificate Pins

### Step 1: Get the Certificate from the Server

For each endpoint, run the following command to extract the public key pin:

```bash
# For production API
openssl s_client -servername api.cipherbank.money -connect api.cipherbank.money:443 < /dev/null 2>/dev/null | \
  openssl x509 -pubkey -noout | \
  openssl pkey -pubin -outform der | \
  openssl dgst -sha256 -binary | \
  openssl enc -base64

# For sandbox API
openssl s_client -servername api.sandbox.cipherbank.money -connect api.sandbox.cipherbank.money:443 < /dev/null 2>/dev/null | \
  openssl x509 -pubkey -noout | \
  openssl pkey -pubin -outform der | \
  openssl dgst -sha256 -binary | \
  openssl enc -base64
```

### Step 2: Verify the Pin

The output will be a Base64-encoded string like:
```
YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=
```

Add the `sha256/` prefix to create the full pin:
```
sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=
```

### Step 3: Get Backup Pins

**Important**: Always include at least one backup pin to allow for certificate rotation without breaking the app.

Options for backup pins:
1. **Certificate Authority (CA) pin**: Pin the intermediate or root CA certificate
2. **Planned rotation pin**: If you know the next certificate, pin it in advance
3. **Secondary endpoint pin**: Pin a secondary CDN or failover endpoint

To get the CA certificate pin:
```bash
# Get the full certificate chain
openssl s_client -servername api.cipherbank.money -connect api.cipherbank.money:443 -showcerts < /dev/null 2>/dev/null > chain.pem

# Extract and pin the intermediate CA (usually the second certificate)
# Then compute its pin using the same method
```

## Updating Certificate Pins

### C# policy (iOS, Mac Catalyst, Windows)

Edit the pin properties on `CertificatePinPolicy` in
`CipherBank-app.Core/Security/CertificatePinPolicy.cs`. Keep the `sha256/`
prefix. The platform handlers do not keep a separate `PinnedPublicKeys` array.

```csharp
public static string ProductionPin { get; } = "sha256/YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=";

public static string BackupPin { get; } = "sha256/BACKUP_PIN_HERE=";
```

Set `SandboxPin` and `SandboxBackupPin` the same way. Do not leave
`REPLACE_WITH_` in any of those four properties in a Release build.
`EnsureReleasePinsAreConfigured` throws at startup when the marker remains.

### Android

Edit `CipherBank-app/Platforms/Android/Resources/xml/network_security_config.xml`.
Use the same digest as the C# pin, without the `sha256/` prefix:

```xml
<domain-config>
    <domain includeSubdomains="true">api.cipherbank.money</domain>
    <pin-set expiration="2027-12-31">
        <pin digest="SHA-256">YLh1dUR9y6Kja30RrAn7JKnbQG/uEtLMkBgFF2Fuihg=</pin>
        <pin digest="SHA-256">BACKUP_PIN_HERE=</pin>
    </pin-set>
</domain-config>
```

**Note**: Android pins do NOT include the `sha256/` prefix. The `digest`
attribute is `SHA-256`. That is the network security config schema.
`AndroidCertificatePinningHandler` does not compare `CertificatePinPolicy`
itself; the XML is the Android source of truth.

## Pin Expiration and Rotation

### Monitoring

Set up monitoring to alert before pin expiration:
- Check certificate expiration dates monthly
- Plan pin updates 30 days before certificate renewal
- Test new pins in staging environment first

### Rotation Process

1. **Before certificate rotation**:
   - Add the new certificate's pin as a backup pin
   - Deploy app update with both old and new pins
   - Wait for users to update (typically 2-4 weeks)

2. **After certificate rotation**:
   - Verify the new certificate is active
   - In the next app update, remove the old pin
   - Update the backup pin for the next rotation

### Emergency Procedures

If pins need to be changed urgently:
1. Deploy a server-side pin override (if supported)
2. Push an expedited app update
3. Consider temporarily disabling pinning (not recommended for production)

## Testing Certificate Pinning

### Using a Proxy (Should Fail)

1. Install Charles Proxy or mitmproxy
2. Configure device to use the proxy
3. Install the proxy's CA certificate on the device
4. Launch the app and attempt to make API calls
5. **Expected result**: All API calls should fail with certificate validation errors

### Successful Connection Test

1. Remove any proxy configuration
2. Ensure the app has the correct pins
3. Launch the app and perform login
4. **Expected result**: All API calls should succeed

### Debug Logging

In DEBUG builds, certificate pinning logs are enabled. Look for:
```
[Certificate Pinning] Success for api.cipherbank.money
```

or on failure:
```
[Certificate Pinning] Pin mismatch for api.cipherbank.money
```

## Troubleshooting

### "Certificate validation failed" errors

1. Verify the pin is correct using the openssl command
2. Check if the server certificate has been rotated
3. Ensure both primary and backup pins are present

### App works in DEBUG but not RELEASE

1. Ensure all platform configurations are updated
2. Verify no debug-only bypass code is present
3. Check that release builds include the correct pins

### Pin mismatch after server update

1. Generate new pin from the current server certificate
2. Update all platform configurations
3. Deploy app update before removing old pins

### iOS-specific issues

- Check that pins are correctly formatted (include `sha256/` prefix)
- Verify certificate chain is valid
- Check debug logs for specific error messages

### Android-specific issues

- Verify `network_security_config.xml` is in `Platforms/Android/Resources/xml/`
- Check that pins are base64-encoded (without `sha256/` prefix)
- Verify `AndroidManifest.xml` references the config correctly
- Check logcat for network security errors

### Windows-specific issues

- Ensure `WindowsCertificatePinningHandler` is being used
- Check that pins include the `sha256/` prefix
- Verify TLS 1.2+ is supported

## Files Reference

| Platform | File | Description |
|----------|------|-------------|
| Shared C# pins | `CipherBank-app.Core/Security/CertificatePinPolicy.cs` | Hostnames and `sha256/` SPKI pins used by iOS, Mac Catalyst, and Windows |
| iOS | `Platforms/iOS/IosCertificatePinningHandler.cs` | NSUrlSessionHandler implementation |
| Mac Catalyst | `Platforms/MacCatalyst/CertificatePinningHandler.cs` | NSUrlSessionHandler implementation (`MacCatalystCertificatePinningHandler`) |
| Android | `Platforms/Android/Resources/xml/network_security_config.xml` | Network security configuration (base64 digests, no `sha256/` prefix) |
| Android handler | `Platforms/Android/AndroidCertificatePinningHandler.cs` | Default handler; pinning is the XML above, referenced from `AndroidManifest.xml` |
| Windows | `Platforms/Windows/WindowsCertificatePinningHandler.cs` | HttpClientHandler implementation |
| Handler factory | `Services/IPlatformHttpMessageHandlerFactory.cs` | Per-platform `*HttpMessageHandlerFactory` |

## Security Best Practices

1. **Never disable pinning in production** - Even temporarily
2. **Always use backup pins** - Prevents lockout during rotation
3. **Monitor certificate expiration** - Set up alerts 60 days before expiry
4. **Test with proxies** - Verify pinning works before each release
5. **Use short pin expiration in Android** - Set reasonable `expiration` dates
6. **Log pinning failures** - But don't log actual pin values in production
7. **Keep pins in sync** - All platforms should have identical pins

## References

- [OWASP Certificate Pinning](https://owasp.org/www-community/controls/Certificate_and_Public_Key_Pinning)
- [Android Network Security Config](https://developer.android.com/training/articles/security-config)
- [Apple Transport Security](https://developer.apple.com/documentation/security/preventing_insecure_network_connections)
