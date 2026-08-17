# CipherBank repository contract

This file governs the M1a → M5 stack. More specific `AGENTS.md` files apply in their subtrees and may tighten these rules.

Start with this file, then read the nearest subtree contract and the
documentation index in `docs/README.md`.

## Stack and ownership

| Layer | Owns | Must not own |
| --- | --- | --- |
| `CipherBank-app.Core` | Domain models, application services, persistence ports, EF Core, scheduling, product client contract | MAUI controls, platform APIs, ChallengePass algorithms |
| `CipherBank-app.ChallengePass` | Challenge/pass suites, account-key sources, A1/A2 crypto composition, key-share ports | UI, database access, long-lived cleartext secrets |
| `CipherBank-app` | MAUI composition root, views, ViewModels, platform adapters | Domain policy, manual SQL, static service locators |
| `CipherBank-app.Analyzers` | Repository-structure Roslyn diagnostics (CPM, AssemblyInfo, Core SQL, retired names) | Product behavior |
| `CipherBank-app.Tests` | Unit, architecture, options, crypto, and persistence regression tests | Shared mutable fixtures or production substitutes |
| Integration/E2E projects | HTTP boundaries and complete user journeys | Reimplementation of product behavior |

Dependencies point inward: MAUI and ChallengePass may depend on Core; Core never depends on either. Tests may depend on the layer they verify.

## Structural rules

1. Package versions live only in `Directory.Packages.props`. Project files declare package identity and asset metadata without `Version=`.
2. Assembly metadata lives in the owning `.csproj`. Do not add `Properties/AssemblyInfo.cs`.
3. Constructor injection is the default. Depend on focused interfaces, not dependency bags, static service locators, or broad API objects.
4. Use production names for production and stateful development implementations. `Mock*` is reserved for test doubles; prefer Moq for a small collaborator contract and `InMemory*` for behavior that intentionally keeps state.
5. Bounded background dispatch uses an injected `TaskScheduler` and `PriorityQueue<TElement,TPriority>`. Do not hand-sort mutable work lists or call `Task.Run` inside domain services.
6. Routine database work uses `CipherBankDbContext` and EF Core `Migrate()`. Schema changes are a new EF migration plus a previous-migration upgrade test. Production code does not own SQL command text. Unmatched prototype SQLite files (no `__EFMigrationsHistory`) are wiped, not repaired.
7. Prefer framework facilities (`ArgumentNullException.ThrowIfNull`, `TimeProvider`, `Math.Sign`, spans, cryptographic zeroization, options validation) over local substitutes.
8. Repository-owned configuration is separated by theme under `config/`, documented by a neighboring `README.md`, represented by typed options, validated at startup, and free of secrets.
9. One primary type per C# file. The filename matches the primary type.

## Function and API documentation

New or materially changed functions include a short summary of purpose. Security, dispatch, persistence, and E2E methods also state expected use frequency (`High`, `Medium`, or `Low`) and scope when that context is not obvious.

Public APIs document units, failure behavior, ownership of returned buffers, and cancellation semantics where their types do not make those facts obvious.

Do not write comments that merely narrate syntax. Comments preserve decisions, invariants, and caller obligations.

## ChallengePass security invariants

- Private keys, KEM shared secrets, seeds, channel keys, and derived key material are wiped in `finally` blocks across success, cancellation, malformed peer input, and exceptions.
- Returned secret arrays have explicit ownership. A cache or fixture zeroes a displaced value before replacement and clears retained values on disposal.
- A2 identity adoption, key-share establishment, and pass construction remain on the fused, gated path. Do not restore the split `SetDeviceIdentity` sequence.
- Configuration may select an installed suite; it never contains key material. Unknown suites fail validation.
- Cryptographic algorithms and wire identifiers are versioned independently. Changing a wire value requires compatibility tests and migration notes.

## UI and design system

The UI contract is in `docs/style/README.md` and `CipherBank-app/Resources/Styles/AGENTS.md`.

- `Colors.xaml` owns semantic color tokens.
- `Typography.xaml` owns the type scale and approved font roles.
- `Styles.xaml` owns control defaults and component styles.
- Views consume tokens and named styles. Do not add literal hex colors or ad hoc font families to pages.
- Inter is the interface-copy family; Space Grotesk is reserved for display, section, and financial-value hierarchy.
- Interactive targets remain at least 44×44 device-independent units. Meaning is never conveyed by color alone.

Copy from `templates/ui/` for new pages and from `templates/service/` or `templates/config/` for new capabilities. Update the template in the same change when a repository convention changes.

## Quality and Sonar

- `TreatWarningsAsErrors` remains enabled. Allow lists are narrow, documented, and shrinking; NuGet resolution, nullable, cancellation, and security warnings are not parked.
- `CipherBank-app.Analyzers` is the architecture gate; it runs on every `dotnet build`.
- Local C# Sonar parity is opt-in through `./scripts/lint-csharp.sh`; CI Sonar remains the merge authority.
- Sonar must analyze production folders and interfaces. Exclusions require a specific generated/vendor reason and the narrowest possible path.
- Resolve findings on the earliest stack layer that owns the code, then merge upward.
- A local `.compliance/` overlay is optional and untracked. Do not commit it.

## Required verification

Run the narrowest relevant test first, then the complete gates for a review-ready change:

```bash
dotnet test CipherBank-app.Analyzers.Tests/CipherBank-app.Analyzers.Tests.csproj
./scripts/lint.sh
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj /p:CollectCoverage=false
dotnet build CipherBank-app.ChallengePass/CipherBank-app.ChallengePass.csproj
dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-android
```

Changes to XAML resources also require a light/dark visual pass at compact and large text settings. Changes to persistence require clean-database and upgrade-shape tests. Changes to ChallengePass require A1, A2, malformed-input, cancellation, and zeroization coverage.

## E2E and sensitive artifacts

- Appium story tests use stable `CB-*`/`US-*` traits and fail if a selected wave resolves zero tests.
- Package reset uses `adb shell pm clear com.companyname.cipherbankapp`.
- Journals, recovery pulls, and diagnostics belong under gitignored `artifacts/` and must never be committed.
- PINs, mnemonics, tokens, keys, PANs, and full bank coordinates are never logged in production.

## Repository map

| Path | Scope |
|---|---|
| `CipherBank-app/AGENTS.md` | Host (composition/startup) + UI (Views/ViewModels) |
| `CipherBank-app.Core/AGENTS.md` | Core/domain, currently also carrying Application/Infrastructure concerns |
| `CipherBank-app.Core/Persist/AGENTS.md` | Persist (EF Core `Migrate()`, LocalDb, sync) |
| `CipherBank-app.Tests/AGENTS.md` | Unit tests |
| `CipherBank-app.Analyzers/AGENTS.md` | Repository-structure Roslyn analyzers |
| `CipherBank-app.IntegrationTests/AGENTS.md` | Integration tests |
| `CipherBank-app.ChallengePass/AGENTS.md` | Challenge/pass suites and A1/A2 crypto |
| `CipherBank-app.E2ETests/AGENTS.md` | End-to-end tests |
| `config/sonar/AGENTS.md` | Gate ownership and analyzer/suppression governance |

## Licensing

Do not add or change a repository license, file-header ownership policy, or third-party attribution without the owner's explicit choice. Package additions require license and maintenance review.
