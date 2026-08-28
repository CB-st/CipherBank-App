# Test agent contract

- Characterize existing observable behavior before refactoring it.
- Use unit tests for pure policy, integration tests for adapters, contract tests for serialized/external boundaries, and end-to-end tests for high-value journeys.
- Avoid wall-clock delays, external network calls, order dependence, and shared mutable fixtures.
- Use fixed clocks, deterministic data, and explicit cultures.
- Do not mock EF query providers, serializers, or framework internals.
- Every bug fix requires a regression test that fails without the fix.

## This project

Unit tests for CipherBank-app.Core. Repository-shape rules (CPM versions, AssemblyInfo, Core SQL, retired API names) live in `CipherBank-app.Analyzers` and run on compile. Fastest tier; runs in the `coverage` job of `.github/workflows/sonar.yml` and feeds Coverlet/OpenCover into the Sonar scan.
