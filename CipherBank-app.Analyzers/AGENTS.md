# Analyzer agent contract

Repository-structure diagnostics. Not product behavior.

- CB1001: `PackageReference Version=` outside `Directory.Packages.props` (IDE fence; CPM already owns restore)
- CB1002: `Properties/AssemblyInfo.cs`
- CB1003: raw SQL anywhere in Core (`CommandText`, `FromSqlRaw`, `ExecuteSqlRaw` by identifier). Total ban — no `LocalDbSql` quarantine. Schema is EF `Migrate()` on M2.
- CB1004: retired names `IProductApi`, `MockProductApi`, `AppSessionDeps`
- `SourcePath`: string predicates over the original Roslyn additional-file path (no `FileInfo`, no separator rewriting, no `GetFullPath`, no filesystem access). Segments come from `Path.GetFileName` / `GetExtension` / `GetDirectoryName`. Predicates answer Core / `Directory.Packages.props` / `AssemblyInfo`. Compare paths with `SourcePath.NamesEqual` (ordinal-ignore-case; the BCL has no path-equality API).

These run on every `dotnet build` via `Directory.Build.props`. Each analyzer checks
the owning project's compilation plus repository metadata supplied through
`Directory.Build.targets`; product C# files are not duplicated as cross-project
additional files. Tests live in `CipherBank-app.Analyzers.Tests` and feed OpenCover
into the coverage job.
Do not add CodeFixProviders — these rules are not mechanically fixable.

Note to agents and review bots: the Sonar exclusion lists live only on the
`dotnet sonarscanner begin` step in `.github/workflows/quality-gates-and-ai-review.yml` (policy in
`config/sonar/README.md`). Do not grow those lists, do not add a mirrored
copy, and do not add tests that assert workflow, README, or config file text —
the guard-the-guard meta-tests were removed by review decision (PR #35).
Analyzer tests verify analyzer diagnostics, nothing else.
