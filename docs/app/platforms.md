# Platform-Specific Code

Certificate pinning and platform entry points.

---

## Android

**File**: `Platforms/Android/AndroidCertificatePinningHandler.cs`

Extends `HttpClientHandler`. Certificate pinning is configured in `NetworkSecurityConfig.xml` (Resources/xml). The handler uses the default HttpClientHandler; Android's network security framework validates certs against pinned keys in the config.

**NetworkSecurityConfig.xml**: Defines pin set for `api.cipherbank.money`, `api.sandbox.cipherbank.money`. Placeholder pins must be replaced before production.

---

## iOS / Mac Catalyst

**File**: `Platforms/iOS/CertificatePinningHandler.cs`

Class: `IosCertificatePinningHandler` (NSUrlSessionHandler). Uses `TrustOverrideForUrl` to validate server certificates.

**Pinned hostnames**: `api.cipherbank.money`, `api.sandbox.cipherbank.money`

**Validation**: SecTrust policy, leaf cert public key SHA256 hash compared to `PinnedPublicKeys` array. Placeholder pins must be replaced.

**Note**: `CertificatePinningHandler.cs` defines `IosCertificatePinningHandler`; `PlatformHttpHandlerFactory` references `Platforms.iOS.IosCertificatePinningHandler`.

---

## Windows

**File**: `Platforms/Windows/WindowsCertificatePinningHandler.cs`

Extends `HttpClientHandler`. Uses `ServerCertificateCustomValidationCallback` for custom validation.

**Pinned hostnames**: Same as iOS and Android.

**Validation**: For pinned hosts, validates cert chain; computes SHA256 of SubjectPublicKeyInfo (RSA or ECDSA) and compares to `PinnedPublicKeys`. Placeholder pins must be replaced.

---

## Mac Catalyst

**File**: `Platforms/MacCatalyst/AppDelegate.cs`, `Program.cs`

Uses iOS certificate pinning via `#if IOS || MACCATALYST` in `PlatformHttpHandlerFactory`; `IosCertificatePinningHandler` is shared.

---

## Tizen

**File**: `Platforms/Tizen/Main.cs`, `tizen-manifest.xml`

Stub entry point. Certificate pinning not implemented; default handler would be used.
