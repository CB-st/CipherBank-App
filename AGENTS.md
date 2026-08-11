# CipherBank repository contract

This file is the root contract for humans and coding agents. A deeper `AGENTS.md`
may add stricter rules for its subtree but may not weaken these rules.

Start with this file, then read the nearest subtree contract and the
documentation index in `docs/README.md`.

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

## Quality and Sonar

- `TreatWarningsAsErrors` remains enabled. Allow lists are narrow, documented, and shrinking.
- `scripts/validate-structure.sh` is the architecture gate.
- CI Sonar remains the merge authority. Do not put SonarScanner or quality-gate verify into `dotnet build` / `Directory.Build.*`.
- The live quality gate lives on Sonar. `scripts/sonar/provision_quality_gate.py`
  is the versioned way to change it. Do not reintroduce a checked-in
  `quality-gate.yaml` that CI diffs against the server.
- A local `.compliance/` overlay is optional and untracked. Do not commit it.

## Required verification

Run the narrowest relevant tests while editing, then run:

```bash
./scripts/validate-structure.sh
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release /p:CollectCoverage=false
```

The Sonar quality gate and structure validator are merge gates. Do not suppress a
new analyzer finding globally to make a branch green; fix it or document a narrow,
time-bounded exception.

## Repository map

| Path | Scope |
|---|---|
| `CipherBank-app/AGENTS.md` | Host (composition/startup) + UI (Views/ViewModels) |
| `CipherBank-app.Core/AGENTS.md` | Core/domain, currently also carrying Application/Infrastructure concerns |
| `CipherBank-app.Core/Persist/AGENTS.md` | Persistence ports and LocalDb SQL ownership |
| `CipherBank-app.Tests/AGENTS.md` | Unit + architecture tests |
| `CipherBank-app.IntegrationTests/AGENTS.md` | Integration tests |
| `CipherBank-app.E2ETests/AGENTS.md` | End-to-end tests |
| `config/sonar/AGENTS.md` | Gate ownership and analyzer/suppression governance |

## Licensing

Do not add or change a repository license, file-header ownership policy, or third-party attribution without the owner's explicit choice. Package additions require license and maintenance review.
