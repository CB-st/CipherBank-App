# Core Library

The CipherBank-app.Core project contains shared interfaces, models, and utilities used across the solution. It is a class library targeting .NET 10 with no platform-specific code.

## Purpose

- Define service contracts (`IProductClient`, `IPublicQuoteService`, persist ports) for dependency injection and testing.
- Define domain models (Wallet, Transaction, CryptoCurrency, etc.) used by services and ViewModels.
- Provide wallet address checks via `AddressValidate` (NBitcoin).

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
└── Wallets/          # AddressValidate
```

## Related Documentation

- [models.md](models.md) – All Core models
- [services.md](services.md) – All Core interfaces and utilities
