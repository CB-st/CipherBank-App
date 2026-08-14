# Repository modernization contract

## Goal

Modernize this repository toward .NET 10 and C# 14 while preserving verified user-visible behavior. Refactor one vertical slice at a time; do not perform broad architectural movement before characterization tests exist.

## Dependency direction

- Core/domain code owns policy, immutable types, invariants, and ports.
- Application code coordinates use cases without knowing transport or persistence details.
- Infrastructure implements database, HTTP, file, queue, clock, and telemetry adapters.
- Executable/UI projects own composition, transport mapping, and process lifetime.
- Tests depend on production code; production code never depends on tests.

## Required workflow

1. Read `.compliance/reports/compliance-report.md`.
2. Complete `.compliance/docs/INTENT-TRANSLATION.md` for the selected behavior.
3. Add characterization tests before structural change.
4. Separate policy from mechanism without changing output.
5. Add focused unit, integration, contract, and cancellation tests.
6. Run `./.compliance/scripts/verify-compliance.sh`.
7. Run the configured SonarQube analysis and require the server quality gate before merge.

## C# rules

- Nullable reference types remain enabled under enforcement; resolve warnings deliberately.
- Public async I/O accepts and propagates `CancellationToken`.
- Do not use `.Result`, `.Wait()`, unobserved tasks, or `async void` outside UI events.
- Use typed options with startup validation, `IHttpClientFactory`, structured logging, `TimeProvider`, bounded concurrency, and explicit resource ownership.
- Centralize package versions when the repository contains multiple projects.
- Do not expose database entities or transport DTOs as domain types.
- Avoid service-location through `IServiceProvider` outside composition infrastructure.
- Follow `.compliance/docs/SONARQUBE-DEVELOPMENT-STANDARD.md`: one named function outcome, shallow valid flow, cohesive invariant-owning objects, method-level S3776 enforcement, and explicit trust-boundary security.
- Do not use aggregate Cognitive Complexity as a gate or add broad Sonar suppressions/exclusions.
- Do not apply branchless style broadly. Follow `.compliance/docs/BRANCHLESS-PROGRAMMING.md` only for profiled hot paths or reviewed constant-time requirements, and retain the scalar reference and evidence record.

## Completion

Do not claim compliance unless Release build, tests, formatting/analyzers, dependency audits, and the authoritative SonarQube quality gate pass. Record any environment-bound validation that remains.
