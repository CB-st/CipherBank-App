# CipherBank Documentation

Documentation for the CipherBank-app repository, a .NET 10 MAUI cross-platform cryptocurrency banking application.

## Overview

CipherBank-app targets Android, iOS, Mac Catalyst, and Windows. It provides cryptocurrency wallet management, market data, purchases, and transactions with a focus on security (certificate pinning, secure storage, rate limiting).

## Prerequisites

- **.NET 10 SDK** (10.0.101 or later)
- **MAUI workload** for your target platform(s)

## Build and Run

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run (specify framework for multi-target projects)
dotnet build -f net10.0-android -t:Run
dotnet build -f net10.0-ios -t:Run
dotnet build -f net10.0-maccatalyst -t:Run
dotnet build -f net10.0-windows10.0.19041.0 -t:Run
```

## Documentation Index

| Document | Description |
|----------|-------------|
| [MAUI_FUNCTION_REF.md](MAUI_FUNCTION_REF.md) | Monolithic INVOKE-style map of MAUI/Core/ChallengePass functions (API.md format) |
| [CB_MauiFunctionRef.html](CB_MauiFunctionRef.html) | Navigable HTML twin (also at repo root `CB_MauiFunctionRef.html`) |
| [architecture.md](architecture.md) | Architecture, data flow, security, HTTP pipeline |
| [core/README.md](core/README.md) | Core library overview |
| [core/models.md](core/models.md) | Core models (Wallet, Transaction, CryptoCurrency, etc.) |
| [core/services.md](core/services.md) | Core service interfaces and utilities |
| [app/README.md](app/README.md) | MAUI app overview, DI, MauiProgram |
| [app/services.md](app/services.md) | Service implementations, HTTP handlers, mocks |
| [app/viewmodels.md](app/viewmodels.md) | ViewModels and commands |
| [app/views.md](app/views.md) | Views/Pages and XAML bindings |
| [app/converters.md](app/converters.md) | Value converters |
| [app/platforms.md](app/platforms.md) | Platform-specific code (certificate pinning) |
| [tests/README.md](tests/README.md) | Test strategy overview |
| [tests/unit-tests.md](tests/unit-tests.md) | Unit tests |
| [tests/integration-tests.md](tests/integration-tests.md) | Integration tests |
| [tests/e2e-tests.md](tests/e2e-tests.md) | End-to-end Appium (Shell) |
| [tests/STORY_ID_MAP.md](tests/STORY_ID_MAP.md) | CB-* / US-* story map — Appium owns design-spec at Expo parity |
| [config/README.md](config/README.md) | Build config, analyzers, tooling |
