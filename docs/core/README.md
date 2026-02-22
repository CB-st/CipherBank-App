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

- **CipherBank-app.Tests** – Mocks interfaces, uses models in assertions.
- **CipherBank-app.IntegrationTests** – Uses models for API response deserialization.

The main MAUI app does **not** reference Core; it defines its own Models and service interfaces. This duplication may be consolidated in a future refactor.

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
