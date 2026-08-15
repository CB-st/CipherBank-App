# Analyzer agent contract

Repository-structure diagnostics. Not product behavior.

- CB1001: `PackageReference Version=` outside `Directory.Packages.props`
- CB1002: `Properties/AssemblyInfo.cs`
- CB1003: raw SQL in Core outside `Persist/Sql/LocalDbSql.cs`
- CB1004: retired names `IProductApi`, `MockProductApi`, `AppSessionDeps`

These run on every `dotnet build` via `Directory.Build.props`. Tests live in
`CipherBank-app.Analyzers.Tests` and feed OpenCover into the coverage job.
Do not add CodeFixProviders — these rules are not mechanically fixable.
