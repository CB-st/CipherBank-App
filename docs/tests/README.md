# Tests

CipherBank-app has four test projects following a test pyramid: structure
analyzer tests (compile-time rules), unit tests (fast, many), integration
tests (API behavior), and E2E tests (full user flows).

## Test Pyramid

```mermaid
flowchart TB
    subgraph e2e [E2E Tests]
        E2E[CriticalUserJourneyTests]
    end
    subgraph integration [Integration Tests]
        API[ApiIntegrationTests]
        SEC[SecurityTests]
    end
    subgraph unit [Unit Tests]
        Models[Model Tests]
        Services[Service Tests]
    end
    subgraph structure [Structure analyzers]
        Analyzers[CipherBank-app.Analyzers.Tests]
    end
    e2e --> integration
    integration --> unit
    unit --> structure
```

## Frameworks

| Project | Framework | Assertions | Mocking |
|---------|-----------|------------|---------|
| CipherBank-app.Analyzers.Tests | xUnit | xUnit | Roslyn analyzer testing |
| CipherBank-app.Tests | xUnit | FluentAssertions | Moq |
| CipherBank-app.IntegrationTests | xUnit | FluentAssertions | WireMock |
| CipherBank-app.E2ETests | xUnit | FluentAssertions | Appium |

## Running Tests

```bash
# Structure analyzer tests
dotnet test CipherBank-app.Analyzers.Tests/CipherBank-app.Analyzers.Tests.csproj

# Unit tests only
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj /p:CollectCoverage=false

# Integration tests only
dotnet test CipherBank-app.IntegrationTests

# E2E tests (requires Appium server + device/emulator)
TEST_PLATFORM=android ANDROID_APK_PATH=path/to/app.apk dotnet test CipherBank-app.E2ETests
```

## Coverage

The coverage job in `.github/workflows/sonar.yml` publishes OpenCover for
Sonar (`new_coverage`) and Cobertura for tooling:

- **Analyzer tests**: Coverlet OpenCover on `CipherBank-app.Analyzers`
  (`reports/analyzer.opencover.xml`). No local threshold; Sonar's new-code
  coverage condition is the gate.
- **Unit tests**: Coverlet Cobertura + OpenCover
  (`reports/coverage.cobertura.xml`, `reports/coverage.opencover.xml`).
  Project file still records a 70% local threshold; CI passes
  `Threshold=0` and lets Sonar enforce new-code coverage.
- **Integration tests**: Coverage collected, 0% threshold. Not in the M1
  coverage job.
- **E2E tests**: Coverage collected. Not in the M1 coverage job.

The AI-review `cipherbank_coverage_report.txt` summary is an M4 harness
artifact, not produced on this slice.

## Related Documentation

- [unit-tests.md](unit-tests.md)
- [integration-tests.md](integration-tests.md)
- [e2e-tests.md](e2e-tests.md)
