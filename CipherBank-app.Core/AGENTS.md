# Core Contract

Applies to `CipherBank-app.Core` in addition to the root contract.

- Keep Core UI- and platform-neutral. No MAUI, Shell, device API, or platform
  conditional compilation belongs here.
- Own domain vocabulary, invariants, strongly typed IDs, result types, pure
  algorithms, and interfaces required from outer layers.
- Put an interface beside the capability it abstracts. Name concrete production
  implementations after what they do, not `Mock*` or `Helper`.
- Domain records remain storage-agnostic. EF mapping belongs in `Persist` and
  transport mapping belongs in `V1`.
- Keep time, randomness, file access, networking, and persistence behind injected
  ports except for the LocalDb SQL exception below.
- Security defaults must be conservative, typed, validated, and overridable by
  configuration. Never log PINs, key material, mnemonics, tokens, PANs, or full
  routing/account numbers.
- Public methods must state units for numeric arguments and describe failure and
  cancellation behavior when it is not obvious from the type.

## Raw SQL exception

`Persist/Sql/LocalDbSql.cs` is the one blessed exception to "no raw SQL":
`RepositoryStructureTests.cs` and `scripts/validate-structure.sh` both
enforce that no other file under this project uses `CommandText =`,
`FromSqlRaw`, or `ExecuteSqlRaw`. Don't widen that exception without
updating both checks.
