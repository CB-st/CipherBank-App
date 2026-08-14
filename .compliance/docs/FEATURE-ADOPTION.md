# Feature adoption sequence

## Stage 1: compiler and repository baseline

- Pin the .NET 10 SDK with `global.json` after every required workload supports it.
- Add `net10.0` or the correct platform-qualified target.
- Centralize shared compiler properties.
- Enable nullable annotations in audited scopes, then repository-wide.
- Enable latest recommended analyzers and warnings-as-errors after warning triage.
- Centralize package versions and commit lock files.
- Establish the Sonar way-derived C# profile and new-code quality gate before broad refactoring; see `SONARQUBE-SETUP.md`.

## Stage 2: domain clarity

- Replace primitive identifiers with strongly typed values at important boundaries.
- Use immutable records for value data.
- Express expected outcomes with explicit result types.
- Use pattern matching for closed decision states.
- Make units, culture, rounding, and time explicit.
- Construct functions/objects with the method-level complexity, cohesion, and suppression rules in `SONARQUBE-DEVELOPMENT-STANDARD.md`.

## Stage 3: application and DI

- Establish one composition root per executable.
- Register typed options with startup validation.
- Establish correct singleton/scoped/transient lifetimes.
- Introduce decorators for cross-cutting behavior with tested ordering.
- Remove service-location and static environment access.

## Stage 4: UI composition and accessibility

- Keep views/components thin; move formatting, validation, and business rules into Core/Application.
- Marshal every UI update through the toolkit's dispatcher/synchronization context; treat `.Result`/`.Wait()` on the UI thread as a defect, not a shortcut.
- Adopt source-generated observable properties/commands only after a package-selection review; do not hand-roll `INotifyPropertyChanged` boilerplate indefinitely once a generator is approved.
- Propagate cancellation from window/page lifetime into any long-running command; report progress instead of reaching across threads into controls.
- Meet baseline keyboard and screen-reader accessibility before considering a UI feature complete.

See `UI-COMPOSITION.md` for the full method.

## Stage 5: I/O and resilience

- Make I/O async end-to-end and propagate cancellation.
- Use typed `HttpClient` instances with bounded total/attempt timeouts.
- Add retries only for transient idempotent work.
- Bound queues and parallelism.
- Use migrations and provider-backed tests.

## Stage 6: observability and operations

- Adopt structured logging.
- Add activities/traces around meaningful operations.
- Add bounded-cardinality metrics for rates, latency, errors, and queue pressure.
- Separate liveness from readiness.
- Test shutdown, dependency outage, and telemetry-export failure.

## Stage 7: optimization and deployment

- Profile before using spans, pooling, SIMD, source generation, or GPU acceleration.
- Guard hardware-intrinsics paths with the matching `IsSupported` check and keep a correct scalar/`Vector<T>` fallback; see `SIMD-AND-VECTORIZATION.md` for the full method.
- Give every pooled or native buffer one explicit owner that releases it on every exit path; see `MEMORY-COMPUTE.md` for the full method.
- Treat a GPU compute kernel as an Infrastructure adapter behind a Core-defined port, with a device-availability check and a tested CPU fallback; see `GPU-COMPUTE.md` for the full method.
- Treat branchless code as a measured candidate, not a style rule. Preserve a scalar oracle; benchmark realistic branch distributions and inspect shipping-runtime disassembly. See `BRANCHLESS-PROGRAMMING.md`.
- Benchmark allocation and end-to-end cost.
- Evaluate trimming, single-file publishing, and Native AOT only after dependency compatibility review.
- Preserve a CPU/reference implementation for accelerated scientific code.
