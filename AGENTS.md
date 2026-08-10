# CipherBank Repository Contract

This file is the root contract for humans and coding agents. A deeper `AGENTS.md`
may add stricter rules for its subtree but may not weaken these rules.

## Architecture

- `CipherBank-app.Core` owns domain behavior, persistence abstractions, DTOs,
  validation, and platform-neutral services. It must not reference MAUI types.
- `CipherBank-app` is the composition root and UI host. Views call ViewModels;
  ViewModels depend on interfaces; platform code stays under `Platforms/`.
- Test projects may reference production projects. Production projects must never
  reference a test project, Moq, xUnit, FluentAssertions, or test fixtures.
- Dependencies point toward Core. Cross-feature calls use an interface in the
  owning feature, registered in the host composition root.

## Implementation rules

- Add package versions only to `Directory.Packages.props`. A project file may
  declare a `PackageReference`, but never its `Version`.
- Put generated assembly attributes in the relevant `.csproj`; do not add a
  `Properties/AssemblyInfo.cs` file.
- Use constructor injection. Do not create service-locator calls or dependency
  bag records. Use Moq only in test projects for behavior-focused test doubles.
- Use EF Core for normal database operations. Raw SQL is limited to
  `Persist/Sql/LocalDbSql.cs` and EF migration files, must be parameterized or
  compile-time schema SQL, and must never persist account/routing cleartext.
- Async APIs accept and propagate `CancellationToken` unless they are deliberate
  zero-token convenience overloads. Never block on async work.
- Prefer framework APIs (`Math.Sign`, `Math.Clamp`, `TimeProvider`,
  `PriorityQueue`, `TaskScheduler`) over local equivalents.
- Place configuration under `config/<theme>/`. Add a README entry, a typed
  options class, validation, and DI binding for every runtime setting.
- Public APIs require XML summaries; non-obvious parameters and transformations
  require parameter documentation or a short rationale.

## Required verification

Run the narrowest relevant tests while editing, then run:

```bash
./scripts/validate-structure.sh
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release
```

The Sonar quality gate and structure validator are merge gates. Do not suppress a
new analyzer finding globally to make a branch green; fix it or document a narrow,
time-bounded exception.

## Licensing

The repository does not currently declare a license. Do not change copyright
headers or add a license on an agent's assumption; the repository owner must make
and record that legal choice.
