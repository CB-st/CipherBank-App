# Analyzer agent contract

Repository-structure diagnostics. Not product behavior.

- CB1001: `PackageReference Version=` outside `Directory.Packages.props`
- CB1002: `Properties/AssemblyInfo.cs`
- CB1003: raw SQL in Core outside `Persist/Sql/LocalDbSql.cs`
- CB1004: retired names `IProductApi`, `MockProductApi`, `AppSessionDeps`
- `SourcePath`: string extensions on host-native additional-file paths. Uses `Path.GetFileName` / `GetExtension` / `GetDirectoryName` (no separator rewriting). Predicates answer Core / `Directory.Packages.props` / `AssemblyInfo` / `LocalDbSql`. Do not use `Path.GetFullPath` or `Combine` on additional-file strings.

These run in every project on `dotnet build` via `Directory.Build.props`. Each
project's C# is analyzed through its own compilation; `Directory.Build.targets`
provides shared MSBuild files as additional inputs without duplicating source
documents across projects. Tests live in `CipherBank-app.Analyzers.Tests` and
feed OpenCover into the coverage job.
Do not add CodeFixProviders — these rules are not mechanically fixable.
