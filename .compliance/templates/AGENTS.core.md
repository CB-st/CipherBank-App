# Core/domain agent contract

- Own domain vocabulary, invariants, strongly typed IDs, result types, pure algorithms, and interfaces required from outer layers.
- Do not reference web, database, UI, configuration, telemetry-export, or external-service frameworks.
- Keep time, randomness, file access, networking, and persistence behind injected ports.
- Make units, numeric tolerances, empty-input behavior, and error states explicit.
- Prefer immutable records/values and read-only collections.
- `Span<T>`/`Memory<T>`, pooling, and hardware intrinsics are BCL, not external frameworks: a pure, deterministic vectorized or memory-optimized routine belongs here alongside its plain reference implementation. A GPU-library-backed kernel does not: define the port here, but implement it in Infrastructure.
- Unit-test normal, boundary, invalid, and numerical-reference behavior.
