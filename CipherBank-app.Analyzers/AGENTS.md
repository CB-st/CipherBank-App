# Analyzer agent contract

Repository-structure diagnostics. Not product behavior.

- CB1001: `PackageReference Version=` outside `Directory.Packages.props` (IDE fence; CPM already owns restore)
- CB1002: `Properties/AssemblyInfo.cs`
- CB1003: raw SQL anywhere in Core (`CommandText`, `FromSqlRaw`, `ExecuteSqlRaw` by identifier). Total ban — no `LocalDbSql` quarantine. Schema is EF `EnsureCreated` on M2.
- CB1004: retired names `IProductApi`, `MockProductApi`, `AppSessionDeps`
- `SourcePath`: string extensions. Slash-normalize, then `Path.GetFileName` / `Path.GetExtension`. Predicates answer Core / `Directory.Packages.props` / `AssemblyInfo`. Do not use `Path.GetFullPath` or `Combine` on additional-file strings.

These run on every `dotnet build` via `Directory.Build.props`. `Directory.Build.targets`
feeds every product `.csproj` and product C# file as additional files, so CB1001,
CB1002, and CB1004 still fire for `CipherBank-app`, IntegrationTests, and E2ETests
when CI cannot compile the MAUI host on Linux. Tests live in
`CipherBank-app.Analyzers.Tests` and feed OpenCover into the coverage job.
Do not add CodeFixProviders — these rules are not mechanically fixable.
