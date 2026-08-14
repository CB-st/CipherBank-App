# Core/domain agent contract

- Own domain vocabulary, invariants, strongly typed IDs, result types, pure algorithms, and interfaces required from outer layers.
- Do not reference web, database, UI, configuration, telemetry-export, or external-service frameworks.
- Keep time, randomness, file access, networking, and persistence behind injected ports.
- Make units, numeric tolerances, empty-input behavior, and error states explicit.
- Prefer immutable records/values and read-only collections.
- `Span<T>`/`Memory<T>`, pooling, and hardware intrinsics are BCL, not external frameworks: a pure, deterministic vectorized or memory-optimized routine belongs here alongside its plain reference implementation. A GPU-library-backed kernel does not: define the port here, but implement it in Infrastructure.
- Unit-test normal, boundary, invalid, and numerical-reference behavior.

## Repository-specific note

`Persist/Sql/LocalDbSql.cs` is the one blessed exception to "no raw SQL":
`RepositoryStructureTests.cs` and `scripts/validate-structure.sh` both
enforce that no other file under this project uses `CommandText =`,
`FromSqlRaw`, or `ExecuteSqlRaw`. Don't widen that exception without
updating both checks.

This project currently absorbs Application- and Infrastructure-layer
responsibilities too (there's no separate project for either yet) — see
`.compliance/docs/MIGRATION-PLAYBOOK.md` for the staged extraction path if
that separation becomes worth it. Until then, judge new code here against
`AGENTS.application.md` and `AGENTS.infrastructure.md` in
`.compliance/templates/` as well as the Core rules above, depending on
which kind of code it actually is.
