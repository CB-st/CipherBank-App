# CipherBank documentation

Documentation for the .NET 10 CipherBank MAUI/Core/ChallengePass stack.

## Build prerequisites

- .NET 10 SDK and the workload for the target MAUI platform
- Android SDK/JDK for the primary Android build
- Appium only for device-level E2E execution

## Build and test

```bash
dotnet restore
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj /p:CollectCoverage=false
dotnet build CipherBank-app.ChallengePass/CipherBank-app.ChallengePass.csproj
dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-android
```

## Documentation index

| Document | Description |
| --- | --- |
| [../AGENTS.md](../AGENTS.md) | Repository architecture, coding, security, UI, and verification contract |
| [architecture.md](architecture.md) | Architecture, data flow, security, and HTTP pipeline |
| [style/README.md](style/README.md) | Typography, semantic color, components, layout, and accessibility |
| [BUILD_LOG.md](BUILD_LOG.md) | Prototype layer map and implementation history |
| [MAUI_FUNCTION_REF.md](MAUI_FUNCTION_REF.md) | MAUI/Core/ChallengePass function map |
| [SONAR_GATE.md](SONAR_GATE.md) | Sonar quality-gate policy |
| [SONAR_STRUCTURAL_PLAN.md](SONAR_STRUCTURAL_PLAN.md) | One-type-per-file and caller-map work |
| [LOCAL_LINT.md](LOCAL_LINT.md) | Multi-language local lint workflow |
| [LOCAL_SONAR_LINT.md](LOCAL_SONAR_LINT.md) | Local SonarAnalyzer and IDE connected-mode guidance |
| [core/README.md](core/README.md) | Core library overview |
| [core/models.md](core/models.md) | Domain models |
| [core/services.md](core/services.md) | Core ports and services |
| [app/README.md](app/README.md) | MAUI app and composition root |
| [app/services.md](app/services.md) | MAUI service adapters and HTTP clients |
| [app/viewmodels.md](app/viewmodels.md) | ViewModels and commands |
| [app/views.md](app/views.md) | Pages and XAML bindings |
| [app/converters.md](app/converters.md) | Value converters |
| [app/platforms.md](app/platforms.md) | Platform-specific adapters |
| [tests/README.md](tests/README.md) | Test strategy |
| [tests/unit-tests.md](tests/unit-tests.md) | Unit tests |
| [tests/integration-tests.md](tests/integration-tests.md) | Integration tests |
| [tests/e2e-tests.md](tests/e2e-tests.md) | Appium E2E tests |
| [tests/e2e-local.env.example](tests/e2e-local.env.example) | Gitignored local E2E credential template |
| [config/README.md](config/README.md) | Build, analyzer, and tooling configuration |
| [review/m1a-comment-resolution.md](review/m1a-comment-resolution.md) | M1a PR #25 feedback map |
| [review/m2-alignment-resolution.md](review/m2-alignment-resolution.md) | M2 PR #21 and M1a forward-port map |

Reusable scaffolds are indexed in [../templates/README.md](../templates/README.md).
