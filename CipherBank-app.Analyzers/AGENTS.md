# Analyzer agent contract

Repository-structure diagnostics. Not product behavior.

- CB1001: `PackageReference Version=` outside `Directory.Packages.props`
- CB1002: `Properties/AssemblyInfo.cs`
- CB1003: raw SQL in Core outside `Persist/Sql/LocalDbSql.cs`
- CB1004: retired names `IProductApi`, `MockProductApi`, `AppSessionDeps`
- `SourcePath`: host-native additional-file path value. Segments from `Path.GetFileName` / `GetExtension` / `GetDirectoryName` (no separator rewriting). Predicates answer Core / `Directory.Packages.props` / `AssemblyInfo` / `LocalDbSql`. Do not use `Path.GetFullPath` or `Combine` on additional-file strings.

These run on every `dotnet build` via `Directory.Build.props`. `Directory.Build.targets`
feeds every product `.csproj` and product C# file as additional files, so CB1001,
CB1002, and CB1004 still fire for `CipherBank-app`, IntegrationTests, and E2ETests
when CI cannot compile the MAUI host on Linux. Tests live in
`CipherBank-app.Analyzers.Tests` and feed OpenCover into the coverage job.
Do not add CodeFixProviders — these rules are not mechanically fixable.
