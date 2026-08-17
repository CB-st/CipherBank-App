# Core/domain agent contract

- Own domain vocabulary, invariants, strongly typed IDs, result types, pure algorithms, and interfaces required from outer layers.
- Do not reference web, database, UI, configuration, telemetry-export, or external-service frameworks from domain types.
- Keep time, randomness, file access, networking, and persistence behind injected ports.
- Make units, numeric tolerances, empty-input behavior, and error states explicit.
- Prefer immutable records/values and read-only collections.
- Unit-test normal, boundary, invalid, and numerical-reference behavior.

## Repository-specific note

`NoScatteredSqlAnalyzer` in `CipherBank-app.Analyzers` reports `CommandText =`, `FromSqlRaw`, or `ExecuteSqlRaw` in any Core file. Persistence uses EF Core entities and `SaveChanges`.
