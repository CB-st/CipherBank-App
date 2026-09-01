# Analyzer agent contract

Repository-structure diagnostics. Not product behavior.

- CB1001: `PackageReference Version=` outside `Directory.Packages.props` (IDE fence; CPM already owns restore)
- CB1002: `Properties/AssemblyInfo.cs`
- CB1003: raw SQL anywhere in Core (`CommandText`, `FromSqlRaw`, `ExecuteSqlRaw` by identifier). Total ban — no `LocalDbSql` quarantine. Schema is EF `Migrate()` on M2.
- CB1004: retired names `IProductApi`, `MockProductApi`, `AppSessionDeps`
- `SourcePath`: wraps a `FileInfo` (sealed, so not a subclass) for additional-file paths. Segments from `Path.GetFileName` / `GetExtension` / `GetDirectoryName` on the original Roslyn string (no separator rewriting, no `GetFullPath`). Predicates answer Core / `Directory.Packages.props` / `AssemblyInfo`. Compare paths with `SourcePath.NamesEqual`.

These run on every `dotnet build` via `Directory.Build.props`. `Directory.Build.targets`
feeds every product `.csproj` and product C# file as additional files, so CB1001,
CB1002, and CB1004 still fire for `CipherBank-app`, IntegrationTests, and E2ETests
when a job builds only Analyzers/Core/Tests. Tests live in
`CipherBank-app.Analyzers.Tests` and feed OpenCover into the coverage job.
Do not add CodeFixProviders — these rules are not mechanically fixable.
