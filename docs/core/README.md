# Core Library

The CipherBank-app.Core project contains shared interfaces, models, and utilities used across the solution. It is a class library targeting .NET 10 with no platform-specific code.

## Purpose

- Define service contracts (IAuthService, IWalletService, etc.) for dependency injection and testing.
- Define domain models (Wallet, Transaction, CryptoCurrency, etc.) used by services and ViewModels.
- Provide validation (AddressValidator) and utilities (RateLimiter, LogRedactionHelper).

## Dependencies

- `Microsoft.Extensions.Http`
- `Microsoft.Extensions.Logging.Abstractions`

## Usage

The Core project is referenced by:

- **CipherBank-app** – Single source of truth for Models and service interfaces.
- **CipherBank-app.Tests** – Mocks interfaces, uses models in assertions.
- **CipherBank-app.IntegrationTests** – Uses models for API response deserialization.

`ISettingsService` remains in the app (may use MAUI Preferences); all other service interfaces and models live in Core.

## Structure

```
CipherBank-app.Core/
├── Models/           # Domain models
├── Services/         # Interfaces
├── Services/Logging/ # LogRedactionHelper
├── Services/Validation/ # AddressValidator
└── Services/         # RateLimiter
```

## Related Documentation

- [models.md](models.md) – All Core models
- [services.md](services.md) – All Core interfaces and utilities
- [Documentation Conventions](../conventions.md) – Component page structure and mirrored paths

## Component References

This area index will link to focused type pages as they are added. Planned
examples include `docs/core/Wallet.md`, `docs/core/IAuthService.md`, and
`docs/core/AddressValidator.md`. These references are planned; create each page
before adding it as a Markdown link.
