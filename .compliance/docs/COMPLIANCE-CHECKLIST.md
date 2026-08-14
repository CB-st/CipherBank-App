# Compliance checklist

## Repository baseline

- [ ] Supported .NET 10 SDK pinned or explicitly provided by CI.
- [ ] All projects target `net10.0` or justified platform-qualified/multi-target variants.
- [ ] Nullable references enabled and warnings resolved intentionally.
- [ ] Latest recommended analyzer level enabled.
- [ ] SonarQube C# quality profile starts from Sonar way; customizations are documented and owned.
- [ ] New-code quality gate requires zero new issues, 100% reviewed new Security Hotspots, at least 80% coverage, and at most 3% duplication.
- [ ] Warnings treated as errors in the enforced scope.
- [ ] Deterministic Release builds.
- [ ] Shared package versions centrally managed.
- [ ] Lock files and dependency audits included in CI.

## Structure

- [ ] Domain policy does not reference database, web, UI, or external SDK types.
- [ ] Use-case orchestration is separated from transport and persistence mechanics.
- [ ] External mechanisms implement inward-owned ports.
- [ ] Executable projects contain the composition root.
- [ ] Transport DTOs and persistence entities are not domain models.
- [ ] Project references follow an inward acyclic direction.
- [ ] Scoped `AGENTS.md` and README files describe actual ownership.

## Implementation

- [ ] Each function has one named outcome, explicit failure semantics, shallow valid flow, and one readable abstraction level.
- [ ] Each object owns cohesive invariants, has explicit narrow dependencies, and avoids service location/ambient mutable state.
- [ ] Sonar S3776 is enforced per function using the active profile; aggregate Cognitive Complexity is not used as a gate.
- [ ] Public I/O APIs are async and propagate cancellation.
- [ ] No sync-over-async or unobserved tasks.
- [ ] Queues and concurrency are bounded.
- [ ] Time-dependent behavior uses `TimeProvider` or another explicit clock.
- [ ] Configuration is typed and validated at startup.
- [ ] HTTP clients use managed lifetimes and bounded resilience.
- [ ] Database schemas use migrations in deployed environments.
- [ ] Structured logging, tracing, and metrics avoid sensitive/high-cardinality values.
- [ ] Resources have explicit ownership and disposal.
- [ ] UI/view-model code stays thin; validation and business rules live in Core/Application, and long-running UI commands propagate cancellation without blocking the dispatcher/UI thread.
- [ ] Hardware-intrinsics paths check `IsSupported`/`IsHardwareAccelerated` and keep a correct scalar/`Vector<T>` fallback.
- [ ] Every pooled/native/device buffer has one explicit owner that releases it on every exit path, including exceptions.
- [ ] GPU compute kernels have a device-availability check and a correct CPU fallback, implemented as an Infrastructure adapter behind a Core-defined port.
- [ ] Branchless/masked code is confined to a profiled hot path; both alternatives are pure and safe for eager evaluation.
- [ ] Constant-time security uses approved cryptographic APIs and review; source-level branchlessness is not treated as proof.
- [ ] Trust boundaries validate and bound input; SQL/commands are parameterized; secrets and sensitive values are absent from source and telemetry.

## Behavior preservation

- [ ] Intent worksheet completed for each migrated slice.
- [ ] Characterization tests preserve observable old behavior.
- [ ] Intentional changes are separated and approved.
- [ ] Numeric units, tolerances, and rounding are explicit.
- [ ] Transaction, ordering, duplicate, and retry semantics are explicit.
- [ ] Differential comparison performed where old and new implementations coexist.

## Testing

- [ ] Pure policy covered by unit tests.
- [ ] Adapters tested with real providers/protocol-faithful substitutes.
- [ ] Serialized and API contracts tested.
- [ ] DI composition, options, and service lifetimes validated.
- [ ] Cancellation, shutdown, timeout, and dependency outage tested.
- [ ] Production-like migration and data-volume rehearsal completed.
- [ ] Benchmarks used for material performance decisions.
- [ ] Vectorized/SIMD code is verified against a scalar reference across representative and boundary numeric inputs with a stated tolerance.
- [ ] Pooled/native-memory code is tested for leaks (rent/return, alloc/free balance) in addition to correctness.
- [ ] GPU-accelerated numeric code is verified against a CPU reference across representative and boundary inputs with a stated tolerance, and the fallback path is exercised by a test.
- [ ] Branchless candidates are differentially tested across numeric/length boundaries and benchmarked on predictable, skewed, random, sorted, and production distributions.
- [ ] UI view-model/component behavior is unit tested independent of the UI framework, and baseline accessibility (keyboard operability, automation names) is checked.
- [ ] Security-sensitive changes include positive behavior tests and negative attack-shape tests.

## Final evidence

- [ ] Formatting passes.
- [ ] Release build passes with zero warnings.
- [ ] Full test suite passes repeatedly.
- [ ] Vulnerable and deprecated dependency audit passes.
- [ ] Authoritative SonarQube analysis completes and the pull-request quality gate passes.
- [ ] Every new Security Hotspot is reviewed; suppressions/exclusions have rule key, evidence, owner, and revisit condition.
- [ ] Rollback/recovery path rehearsed.
- [ ] Remaining exceptions have owner, rationale, risk, and expiration date.
- [ ] Every retained branchless optimization has a completed performance record, disassembly evidence, target-architecture results, and a removal/rebenchmark trigger.
