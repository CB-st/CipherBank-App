# CipherBank documentation

Documentation for the .NET 10 CipherBank MAUI/Core/ChallengePass M4 stack.

## Overview

CipherBank-app targets Android, iOS, Mac Catalyst, and Windows. M3 provides the
shipping onboarding/custody, portfolio, money-movement, vault, POS/NFC, product
HTTP, and public quote surfaces. M4 adds the Appium story catalog, device-state
profiles, account/recovery journeys, harness lifecycle, diagnostics, and gap evidence.

## Prerequisites

- **.NET 10 SDK** (10.0.101 or later)
- **MAUI workload** for your target platform(s)

## Build and test

```bash
dotnet restore
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj /p:CollectCoverage=false
dotnet build CipherBank-app.ChallengePass/CipherBank-app.ChallengePass.csproj
dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-android
```

## Coding standards & Sonar

Day-to-day implementer rules (function docs, ownership, complexity, E2E, **Sonar typology / stages / missteps**) live in the repo-root [AGENTS.md](../AGENTS.md). Gate soften-vs-fix policy: [SONAR_GATE.md](SONAR_GATE.md). Stage 2 file splits: [SONAR_STRUCTURAL_PLAN.md](SONAR_STRUCTURAL_PLAN.md).

## Documentation index

| Document | Description |
|----------|-------------|
| [../AGENTS.md](../AGENTS.md) | **Day-to-day agent rules**, including Sonar typology, Stage 1–3 order, Shell compile gate, common missteps |
| [BUILD_LOG.md](BUILD_LOG.md) | Condensed prototype map: what shipped, where it lives, connections |
| [SONAR_GATE.md](SONAR_GATE.md) | Sonar quality-gate policy: what we fix vs soften (with reasons) |
| [LOCAL_LINT.md](LOCAL_LINT.md) | Pre-push multi-language lint (`./scripts/lint.sh`: C#/shell/Python/C++/Make) |
| [LOCAL_SONAR_LINT.md](LOCAL_SONAR_LINT.md) | C# SonarAnalyzer deep dive + SonarQube for IDE Connected Mode |
| [SONAR_STRUCTURAL_PLAN.md](SONAR_STRUCTURAL_PLAN.md) | Stage 2 SA1402/SA1649 inventory, callers, split targets, execution status |
| [MAUI_FUNCTION_REF.md](MAUI_FUNCTION_REF.md) | Monolithic INVOKE-style map of MAUI/Core/ChallengePass functions (API.md format) |
| [architecture.md](architecture.md) | Architecture, data flow, security, HTTP pipeline |
| [style/README.md](style/README.md) | Typography, semantic color, components, layout, and accessibility |
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
| [tests/e2e-tests.md](tests/e2e-tests.md) | End-to-end Appium overview (full story map lands on M4) |
| [tests/STORY_ID_MAP.md](tests/STORY_ID_MAP.md) | M4 story ownership, executable coverage, and backlog status |
| [tests/e2e-local.env.example](tests/e2e-local.env.example) | Template for gitignored harness credentials |
| [config/README.md](config/README.md) | Build config, analyzers, tooling |
| [config/sonar/README.md](config/sonar/README.md) | Sonar quality-gate policy and local SonarQube-for-IDE setup |
| [review/m1a-comment-resolution.md](review/m1a-comment-resolution.md) | M1a PR #25 feedback map |
| [review/m2-alignment-resolution.md](review/m2-alignment-resolution.md) | M2 PR #21 and M1a forward-port map |
| [review/m3-alignment-resolution.md](review/m3-alignment-resolution.md) | M3 feature preservation and architecture alignment map |
| [review/m4-alignment-resolution.md](review/m4-alignment-resolution.md) | M4 E2E forward-port and architecture alignment map |

Reusable scaffolds are indexed in [../templates/README.md](../templates/README.md).
