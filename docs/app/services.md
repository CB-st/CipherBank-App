# App Services

Service implementations, HTTP handlers, and mocks in `CipherBank-app/Services/`.

---

## Product HTTP

Shell registers `AddCipherBankHttpClient<HttpProductClient>()` and resolves `IProductClient` from that typed client (DEBUG may swap `InMemoryProductClient`). Core no longer registers a competing `HttpClient`. 401 refresh stays on `HttpProductClient`.

Public quotes use `AddPublicApiHttpClient<PublicApiClient>()` / `IPublicQuoteService` (DEBUG may swap `InMemoryPublicQuoteService`).

---

## SettingsService

**File**: `Services/SettingsService.cs`

Implements `ISettingsService`. Uses `Preferences` for persistence.

| Property | Default |
|----------|---------|
| CipherBankEndpointBase | https://api.sandbox.cipherbank.money |
| ThemeMode | System |
| NotificationsEnabled | true |
| BiometricAuthEnabled | false |
| AutoLockTimeoutMinutes | 5 |
| DefaultCurrency | USD |

---

## BiometricService

**File**: `Services/BiometricService.cs`

Implements `IBiometricService` with `Plugin.Maui.Biometric` (`IBiometric` from `BiometricAuthenticationService.Default`). Logical gate only — the custody AES key stays in SecureStorage.

---

## IHealthCheckClient / HealthCheckClient

**File**: `Services/HealthCheckClient.cs`

Used by Settings Test Connection. Uses the app's configured HttpClient (certificate pinning) to hit `/health`.

---

## INavigationService / ShellNavigationService

**File**: `Services/ShellNavigationService.cs`

Abstraction for `Shell.Current.GoToAsync` and `GoBackAsync`. Enables ViewModel testability.

---

## IDialogService / ShellDialogService

**File**: `Services/ShellDialogService.cs`

Abstraction for Shell alerts/prompts. CommunityToolkit.Maui 12.2.0 does not restore on MAUI 10 (NU1608); this wrapper stays.

---

## IErrorHandler / ErrorHandler

**File**: `Services/ErrorHandler.cs`

Centralizes catch logic for `HttpRequestException`, `UnauthorizedAccessException`, `OperationCanceledException`. Sets ErrorMessage, navigates to Login on 401. ViewModels call `HandleApiErrorsAsync`.

---

## AuthHeaderHandler

**File**: `Services/Handlers/AuthHeaderHandler.cs`

`DelegatingHandler` that injects Bearer token from `IProductSessionStore`. Skips `/v1/session` and `/v1/session/refresh`.

---

## HTTP rate limiting

Shared `SlidingWindowRateLimiter` (60/min, fail-fast) registered by `AddCipherBankHttpClient` / `AddPublicApiHttpClient` and wired into `AddStandardResilienceHandler`.

---

## PlatformHttpHandlerFactory

**File**: `Services/PlatformHttpHandlerFactory.cs`

Static factory. Returns platform-specific handler:

| Platform | Handler |
|----------|---------|
| Android | AndroidCertificatePinningHandler |
| iOS / Mac Catalyst | IosCertificatePinningHandler |
| Windows | WindowsCertificatePinningHandler |
| Other | HttpClientHandler |

---

## Mock Implementations

| File | Implements | Description |
|------|------------|-------------|
| InMemoryProductClient | IProductClient | DEBUG product stand-in |
| InMemoryPublicQuoteService | IPublicQuoteService | DEBUG public quotes |
| MockStreamService | IStreamService | DEBUG stream hub |

Mocks are used in DEBUG builds only (`#if DEBUG`).
