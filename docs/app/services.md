# App Services

Service implementations, HTTP handlers, and mocks in `CipherBank-app/Services/`.

---

## AuthService

**File**: `Services/AuthService.cs`

Implements `IAuthService`. Uses `HttpClient` for login/refresh; stores tokens in `SecureStorage`.

| Method | Description |
|--------|-------------|
| LoginAsync | POST `auth/login`, stores tokens in SecureStorage |
| RefreshAsync | POST `auth/refresh`, updates stored tokens |
| GetStoredTokenAsync | Reads from SecureStorage |
| IsTokenExpiredAsync | Compares ExpiresUtc to UtcNow |
| LogoutAsync | Clears SecureStorage |
| RevokeTokenAsync | POST to revoke endpoint (if available) |

**Storage keys**: `auth_access_token`, `auth_refresh_token`, `auth_expires_utc`.

---

## CryptoAPIService

**File**: `Services/CryptoAPIService.cs`

Implements `ICryptoApiService`. HTTP client for market data.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GetCryptoPricesAsync | GET /api/v1/crypto/prices | All crypto prices |
| GetCryptoPriceAsync | GET /api/v1/crypto/prices/{symbol} | Single price |
| GetPriceHistoryAsync | GET /api/v1/crypto/history/{symbol}?period= | Price history |
| SearchCryptoAsync | GET /api/v1/crypto/search?q= | Search by name/symbol |

---

## WalletService

**File**: `Services/WalletService.cs`

Implements `IWalletService`. Manages wallet CRUD and balance.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GetWalletsAsync | GET /api/v1/wallets | All wallets |
| GetWalletAsync | GET /api/v1/wallets/{id} | Single wallet |
| GetWalletBalanceAsync | GET /api/v1/wallets/{id}/balance | Balance |
| CreateWalletAsync | POST /api/v1/wallets | Create wallet |

---

## TransactionService

**File**: `Services/TransactionService.cs`

Implements `ITransactionService`. Manages transactions.

| Method | Endpoint | Description |
|--------|----------|-------------|
| GetTransactionHistoryAsync | GET /api/v1/transactions?walletId={id} | History |
| PurchaseCryptoAsync | POST /api/v1/transactions/purchase | Buy crypto |
| SendCryptoAsync | POST /api/v1/transactions/send | Send to address |
| GetTransactionStatusAsync | GET /api/v1/transactions/{id}/status | Status |

---

## SettingsService

**File**: `Services/SettingsService.cs`

Implements `ISettingsService`. Uses `Preferences` for persistence.

| Property | Default |
|----------|---------|
| CipherBankEndpointBase | https://api.sandbox.cipherbank.money |
| ThemeMode | System | System |
| NotificationsEnabled | true | true |
| BiometricAuthEnabled | false | false |
| AutoLockTimeoutMinutes | 5 | 5 |
| DefaultCurrency | USD | USD |

---

## IHealthCheckClient / HealthCheckClient

**File**: `Services/HealthCheckClient.cs`

Used by Settings Test Connection. Uses app's configured HttpClient (certificate pinning) to hit `/health`. Replaces raw `HttpClient` for security and consistency.

---

## INavigationService / ShellNavigationService

**File**: `Services/ShellNavigationService.cs`

Abstraction for `Shell.Current.GoToAsync` and `GoBackAsync`. Enables ViewModel testability.

---

## IDialogService / ShellDialogService

**File**: `Services/ShellDialogService.cs`

Abstraction for `Shell.Current.DisplayAlertAsync`. Enables ViewModel testability.

---

## IErrorHandler / ErrorHandler

**File**: `Services/ErrorHandler.cs`

Centralizes catch logic for `HttpRequestException`, `UnauthorizedAccessException`, `OperationCanceledException`. Sets ErrorMessage, navigates to Login on 401. ViewModels call `HandleApiErrorsAsync`.

---

## AuthHeaderHandler

**File**: `Services/Handlers/AuthHeaderHandler.cs`

`DelegatingHandler` that injects Bearer token from `IAuthService`. Skips `/auth/login`, `/auth/refresh`, `/auth/register`. Auto-refreshes token if expiring within 5 minutes.

---

## RateLimitingHandler

**File**: `Services/Handlers/RateLimitingHandler.cs`

`DelegatingHandler` that uses `RateLimiter`. Waits up to 30s for a permit; returns 429 if still rate limited.

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
| MockAuthService.cs | IAuthService | In-memory login; accepts any credentials |
| MockCryptoAPIService.cs | ICryptoApiService | Returns sample BTC, ETH, SOL data |
| MockWalletService.cs | IWalletService | Returns sample wallets |
| MockTransactionService.cs | ITransactionService | Returns sample transactions |

Mocks are used in DEBUG builds only (`#if DEBUG`). `ResetToDefaults()` exposed for Settings reset.
