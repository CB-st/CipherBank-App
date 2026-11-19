# CipherBank Documentation

Documentation for the CipherBank-app repository, a .NET 10 MAUI cross-platform cryptocurrency banking application.

## Overview

CipherBank-app targets Android, iOS, Mac Catalyst, and Windows. It provides cryptocurrency wallet management, market data, purchases, and transactions with a focus on security (certificate pinning, secure storage, rate limiting).

## Prerequisites

- **.NET 10 SDK** (10.0.401)
- **MAUI workload** for your target platform(s); NuGet MAUI packages are pinned to **10.0.20** to match the installed workload manifest

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

### L1 — Architecture

- [Architecture](architecture.md) — system boundaries, data flow, security, HTTP pipeline, navigation, and persistence.

### L2 — Area indexes

Start with an area index for focused topic pages and future type references:

| Area index | Scope |
|------------|-------|
| [app/](app/README.md) | MAUI startup, composition, services, UI, and platform adapters |
| [core/](core/README.md) | Domain models, service contracts, and Core utilities |
| [tests/](tests/README.md) | Analyzer, unit, integration, and end-to-end verification |
| [config/](config/README.md) | Build configuration, analyzers, and tooling |

### Guides and policy

- [Documentation conventions](conventions.md) — page shape, source links, and mirrored paths.
- [Module composition](agentic/MODULE_COMPOSITION.md) — explicit feature registration and ViewModel/platform boundaries.
- [Certificate pinning setup](config/CERTIFICATE_PINNING_SETUP.md) — platform certificate configuration.
- [Review decisions](review/REVIEW_DECISIONS.md) — declined or changed reviewer requests and rationale.
