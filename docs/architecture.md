# Architecture

## Overview

CipherBank-app uses a layered architecture with clear separation between the MAUI app, Core library, and test projects.

```mermaid
flowchart TB
    subgraph app [CipherBank-app MAUI]
        Views[Views]
        ViewModels[ViewModels]
        Services[Services]
    end
    subgraph core [CipherBank-app.Core]
        CoreInterfaces[Interfaces]
        CoreModels[Models]
    end
    subgraph tests [Test Projects]
        UnitTests[Unit Tests]
        IntegrationTests[Integration Tests]
        E2ETests[E2E Tests]
    end
    Views --> ViewModels
    ViewModels --> Services
    Services --> CoreInterfaces
    Services --> CoreModels
    UnitTests --> CoreInterfaces
    IntegrationTests --> CoreModels
```

## Layer Responsibilities

| Layer | Responsibility |
|-------|----------------|
| **Views** | XAML UI, bindings, user interaction. No business logic. |
| **ViewModels** | Presentation logic, commands, state. Uses CommunityToolkit.Mvvm. |
| **Services** | API calls, auth, persistence. Implements Core interfaces. |
| **Core** | Shared interfaces, models, validation, rate limiting. Referenced by tests. |

**Note**: The app references CipherBank-app.Core as the single source of truth for Models and service interfaces. Core is MAUI-agnostic.

## Data Flow

```mermaid
sequenceDiagram
    participant View
    participant ViewModel
    participant Service
    participant Handler
    participant API

    View->>ViewModel: User action
    ViewModel->>Service: Command (e.g. GetCryptoPricesAsync)
    Service->>Handler: HTTP request
    Handler->>Handler: AuthHeaderHandler (add Bearer)
    Handler->>Handler: RateLimitingHandler (check limit)
    Handler->>API: Request (certificate pinning)
    API-->>Handler: Response
    Handler-->>Service: HttpResponse
    Service-->>ViewModel: Domain model
    ViewModel->>ViewModel: Update ObservableProperty
    ViewModel-->>View: Binding update
```

## HTTP Pipeline

Outgoing HTTP requests pass through the following pipeline (order matters):

1. **PlatformHttpHandlerFactory** – Creates platform-specific handler with certificate pinning (iOS, Android, Windows).
2. **RateLimitingHandler** – Sliding-window rate limiter (60 requests/minute default). Returns 429 if exceeded.
3. **AuthHeaderHandler** – Injects Bearer token from `IAuthService`. Skips auth endpoints (`/auth/login`, `/auth/refresh`, `/auth/register`). Auto-refreshes token if expiring within 5 minutes.
4. **StandardResilienceHandler** – Retry (3 attempts, exponential backoff, jitter), circuit breaker (50% failure, 30s break), timeouts (15s attempt, 60s total).

## Navigation

Shell-based navigation via `INavigationService` and `IDialogService` abstractions (implemented with Shell wrappers). ViewModels use these services instead of `Shell.Current` for testability.

Route constants are defined in `Constants/Routes.cs`:

| Constant | Route | Page |
|----------|-------|------|
| Routes.Login | `//LoginPage` | LoginPage |
| Routes.Dashboard | `//DashboardPage` | DashboardPage |
| Routes.Wallet | `//WalletPage` | WalletPage |
| Routes.Purchase | `//PurchasePage` | PurchasePage |
| Routes.PurchaseWithSymbol(symbol) | `//PurchasePage?symbol=BTC` | PurchasePage with pre-selected crypto |
| Routes.Settings | `//SettingsPage` | SettingsPage |

## Security

### Certificate Pinning

- **iOS/Mac Catalyst**: `IosCertificatePinningHandler` (NSUrlSessionHandler) validates server cert against pinned public key hashes.
- **Android**: `AndroidCertificatePinningHandler` + `NetworkSecurityConfig.xml`.
- **Windows**: `WindowsCertificatePinningHandler` (HttpClientHandler with custom validation).

Pinned hostnames: `api.cipherbank.money`, `api.sandbox.cipherbank.money`. Placeholder pins must be replaced before production.

### Authentication

- Tokens stored via `SecureStorage`.
- `AuthHeaderHandler` adds Bearer token to requests; refreshes when expiring.
- Logout revokes tokens server-side when possible.

### Other

- **Rate limiting**: Client-side sliding window to avoid API abuse.
- **Log redaction**: `LogRedactionHelper` redacts tokens, addresses, wallet IDs, etc.
- **Address validation**: `AddressValidator` for BTC, ETH, SOL formats.

## Mock vs Real Services

Mock strategy is **build-time only** (no runtime switching):

- **DEBUG**: Uses mock services (`MockAuthService`, `MockCryptoAPIService`, etc.).
- **Release**: Always uses real services (AuthService, CryptoAPIService, etc.).

`UseMocks` has been removed from settings; registration is conditional via `#if DEBUG` in MauiProgram.
