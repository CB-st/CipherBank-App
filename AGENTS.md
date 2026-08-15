# CipherBank repository contract

This file governs the M1a platform slice. More specific `AGENTS.md` files apply in their subtrees and may tighten these rules.

Start with this file, then read the nearest subtree contract and the relevant documentation index in `docs/README.md`.

## Stack and ownership

| Layer | Owns | Must not own |
| --- | --- | --- |
| `CipherBank-app.Core` | Domain models, application services, persistence ports | MAUI controls, platform APIs |
| `CipherBank-app` | MAUI composition root, views, ViewModels, platform adapters | Domain policy, manual SQL, static service locators |
| `CipherBank-app.Tests` | Unit and options regression tests | Shared mutable fixtures or production substitutes |
| `CipherBank-app.Analyzers` | Repository-structure Roslyn diagnostics (CPM, AssemblyInfo, Core SQL, retired names) | Product behavior |
| Integration tests | HTTP and persistence boundaries | Reimplementation of product behavior |
| `CipherBank-app.E2ETests` | Appium journeys and page objects | Product policy or hard-coded credentials |

Dependencies point inward: MAUI may depend on Core; Core never depends on MAUI. Tests may depend on the layer they verify.

## Structural rules

1. Package versions live only in `Directory.Packages.props`. Project files declare package identity and asset metadata without `Version=`.
2. Assembly metadata lives in the owning `.csproj`. Do not add `Properties/AssemblyInfo.cs`.
3. Constructor injection is the default. Depend on focused interfaces, not dependency bags, static service locators, or broad API objects.
4. Use production names for production and stateful development implementations. `Mock*` is reserved for test doubles; prefer Moq for a small collaborator contract and `InMemory*` for behavior that intentionally keeps state.
5. Routine database work uses EF Core. Compatibility SQL is centralized in `CipherBank-app.Core/Persist/Sql/LocalDbSql.cs` when that file exists; no other production file owns SQL command text.
6. Prefer framework facilities (`ArgumentNullException.ThrowIfNull`, `TimeProvider`, spans, options validation) over local substitutes.
7. One primary type per C# file. The filename matches the primary type.

## Quality and Sonar

- `TreatWarningsAsErrors` remains enabled. Allow lists are narrow, documented, and shrinking.
- `CipherBank-app.Analyzers` is the architecture gate; it runs on every `dotnet build`.
- CI Sonar remains the merge authority. Do not put SonarScanner or quality-gate verify into `dotnet build` / `Directory.Build.*`.
- A local `.compliance/` overlay is optional and untracked. Do not commit it.

## Required verification

```bash
dotnet test CipherBank-app.Analyzers.Tests/CipherBank-app.Analyzers.Tests.csproj
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj /p:CollectCoverage=false
```

## Repository map

| Path | Scope |
|---|---|
| `CipherBank-app/AGENTS.md` | Host (composition/startup) + UI |
| `CipherBank-app.Core/AGENTS.md` | Core/domain |
| `CipherBank-app.Tests/AGENTS.md` | Unit tests |
| `CipherBank-app.Analyzers/AGENTS.md` | Repository-structure Roslyn analyzers |
| `CipherBank-app.IntegrationTests/AGENTS.md` | Integration tests |
| `CipherBank-app.E2ETests/AGENTS.md` | End-to-end tests |
| `config/sonar/AGENTS.md` | Gate ownership and analyzer/suppression governance |

## Licensing

Do not add or change a repository license, file-header ownership policy, or third-party attribution without the owner's explicit choice. Package additions require license and maintenance review.
