# Analyzer agent contract

Repository-structure diagnostics. Not product behavior.

- CB1001: `PackageReference Version=` outside `Directory.Packages.props` (IDE fence; CPM already owns restore)
- CB1002: `Properties/AssemblyInfo.cs`
- CB1003: raw SQL anywhere in Core. `CommandText` assignments, including null-conditional targets, and EF Core relational raw-SQL methods (`RelationalDatabaseFacadeExtensions`, `RelationalQueryableExtensions`, `MigrationBuilder`) resolved through the semantic model, including method-group conversions and delegate aliases. Alias tracking covers every part of a partial class, so a delegate field assigned in one file is still flagged when another file invokes it. Same-named methods that are not those APIs are not flagged. Total ban — no `LocalDbSql` quarantine. Schema is EF `Migrate()` on M2.
- CB1004: retired names `IProductApi`, `MockProductApi`, `AppSessionDeps`
- CB1005: platform globals (`Application.Current`, `Preferences`, `Task.Run`, …) in ViewModels. Compiled ViewModels use the semantic model. Limits of the additional-file path: it is a syntax-only fallback for unbuilt sibling ViewModels, with no semantic model. It reports fully qualified names and partial roots opened by a `using` directive. Shadowing is approximated from enclosing type members, parameters, enclosing-block locals and loop/using/fixed locals. Lambda/local-function captures, pattern/`out` variables, `using static`/alias directives and partial types across files are not modeled, so misses and false positives are possible.
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
