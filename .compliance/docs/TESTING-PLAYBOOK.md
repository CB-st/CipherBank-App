# Testing playbook for modernization

## Test in layers of evidence

| Test type | Proves | Does not prove |
|---|---|---|
| Characterization | What the old system observably does | That the behavior is desirable |
| Unit | Pure policy, boundaries, invariants | Provider/protocol behavior |
| Integration | Database, serializer, filesystem, HTTP pipeline | Full user journey |
| Contract | Stable payload/schema compatibility | Internal correctness |
| Host/UI | Routing, middleware, binding, commands, lifecycle | Every numeric edge |
| End-to-end | High-value complete journey | Precise cause of a failure |
| Benchmark | Relative cost under stated conditions | Functional correctness |

## Characterization testing

Start at a seam that already exists: public method, command, endpoint, exported file, database state, or rendered view model. Capture inputs and all important observable outputs.

Use golden fixtures for complex output, but normalize nondeterministic fields such as timestamps, GUIDs, paths, and generated ordering only when they are not part of intent.

Name known undesirable behavior in the test. Example: `LegacyImport_PreservesCurrentBankersRounding_UntilRuleChangeIsApproved`.

## Unit testing the extracted policy

For every decision function test:

- ordinary representative input;
- lower and upper boundaries;
- empty and missing input;
- each expected failure result;
- numeric precision and rounding;
- time-zone or clock edge when relevant;
- property-based invariants for conversions/parsers;
- cancellation only if the operation is actually asynchronous.

Avoid mocks for records, calculations, collections, or value objects.

## Adapter integration testing

### Database

- Use the deployed provider and major version when possible.
- Test mappings, constraints, indexes, transactions, concurrency, migrations, and generated SQL.
- Do not mock `DbSet` or LINQ providers.
- For SQLite, use a real connection; the EF in-memory provider does not reproduce relational semantics.

### HTTP

- Use a fake handler for narrow request formation or WireMock.Net for protocol behavior.
- Test URI, headers, serialization, cancellation, timeout, transient sequence, and non-retryable failures.
- Prove state-changing requests are not duplicated.

### Files and scientific data

- Test encoding, culture, delimiter, missing/extra columns, malformed rows, size bounds, metadata, and unit interpretation.
- Retain reference datasets with provenance and expected tolerances.
- Compare numeric arrays statistically and elementwise as appropriate; visual similarity is not a numerical test.

## Composition testing

Build the complete host with development scope validation and options validation. Resolve each entry point inside its correct scope. This catches missing registrations, captive dependencies, decorator-order mistakes, and invalid configuration.

## API and UI testing

For APIs use `WebApplicationFactory<Program>` and assert status, content type, problem details, schema, authorization, and side effects.

For UI, unit test view models/components against Application ports with fakes/substitutes, independent of any UI framework. Exercise dispatcher marshaling, cancellation, and busy/error-state transitions with a UI automation or component test harness kept separate from those policy tests. See `UI-COMPOSITION.md`.

## Differential testing

While old and new implementations coexist, execute the same sanitized corpus through both. Compare:

- returned values and error categories;
- persisted rows and events;
- serialized files or payloads;
- ordering and duplicates;
- logs/metrics only when they are operational contracts;
- performance distributions.

Investigate every difference. Classify it as defect, intentional change, nondeterminism, or test normalization error.

## Mutation and property testing

Use mutation testing to find assertions that execute code without proving behavior. Use property-based testing for parsers, unit conversions, batching, serialization round trips, and numerical invariants.

## Vectorized numeric testing

Test a vectorized/SIMD routine against its scalar reference across representative inputs, the element-count boundary (0, 1, vector-width − 1, vector-width, vector-width + 1), and floating-point edge cases (`NaN`, `Infinity`, negative zero, denormals). State the comparison tolerance explicitly — exact bits, ULP, or an absolute/relative tolerance — rather than asserting bit-for-bit equality by default. Benchmark the vectorized path against the scalar one with BenchmarkDotNet before relying on it. See `SIMD-AND-VECTORIZATION.md`.

## Memory and GPU-accelerated testing

Apply the same differential method to a pooled/native-memory implementation against a plain managed-array one, and to a GPU-backed kernel against its CPU reference — plus, for the GPU case, a dedicated test that forces "device unavailable" and asserts the CPU fallback actually runs. Test pooled/native code for leaks directly: a counting or debug allocator that fails when `Rent` outpaces `Return`, or when `Free` is never observed after `Alloc`, catches what a correctness-only test will not. See `MEMORY-COMPUTE.md` and `GPU-COMPUTE.md`.

## Branchless optimization testing

Keep the readable branchy implementation as the oracle. Differentially test every value in small integer domains and randomized/production corpora for larger domains. Cover overflow mode, signedness, shifts, conversions, exceptions, side-effect order, overlapping memory policy, floating-point special values, and lengths around SIMD width.

Benchmark branchy and masked candidates on predictable/skewed, 50/50 random, sorted/bursty, and production-representative datasets. Inspect warmed Release disassembly and repeat on every supported architecture. Similar timing is not evidence of constant-time security; use approved cryptographic APIs and security review. See `BRANCHLESS-PROGRAMMING.md`.

## Final gate

```bash
./.compliance/scripts/verify-compliance.sh
```

Then run the configured Scanner for .NET begin/build/test/end workflow and require the completed server quality gate. Coverage generation alone is not a pass: confirm Sonar imported the report and evaluated the intended new-code period. See `SONARQUBE-SETUP.md`.

Also rehearse schema upgrade, rollback/recovery, cancellation, process shutdown, dependency outage, and production-like data volume before cutover.
