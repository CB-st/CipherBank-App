# CipherBank repository contract

This file governs the M1a → M4 stack. More specific `AGENTS.md` files apply in their subtrees and may tighten these rules.

Start with this file, then read the nearest subtree contract and the relevant documentation index in `docs/README.md`.

## Stack and ownership

| Layer | Owns | Must not own |
| --- | --- | --- |
| `CipherBank-app.Core` | Domain models, application services, persistence ports, EF Core, scheduling, product client contract | MAUI controls, platform APIs, ChallengePass algorithms |
| `CipherBank-app.ChallengePass` | Challenge/pass suites, account-key sources, A1/A2 crypto composition, key-share ports | UI, database access, long-lived cleartext secrets |
| `CipherBank-app` | MAUI composition root, views, ViewModels, platform adapters | Domain policy, manual SQL, static service locators |
| `CipherBank-app.Tests` | Unit, architecture, options, crypto, and persistence regression tests | Shared mutable fixtures or production substitutes |
| Integration tests | HTTP and persistence boundaries | Reimplementation of product behavior |
| `CipherBank-app.E2ETests` | Appium lifecycle, stable story catalog, page objects, device profiles, diagnostics, and gap evidence | Product policy, hard-coded credentials, or silent E2E passes |

Dependencies point inward: MAUI and ChallengePass may depend on Core; Core never depends on either. Tests may depend on the layer they verify.

## Structural rules

1. Package versions live only in `Directory.Packages.props`. Project files declare package identity and asset metadata without `Version=`.
2. Assembly metadata lives in the owning `.csproj`. Do not add `Properties/AssemblyInfo.cs`.
3. Constructor injection is the default. Depend on focused interfaces, not dependency bags, static service locators, or broad API objects.
4. Use production names for production and stateful development implementations. `Mock*` is reserved for test doubles; prefer Moq for a small collaborator contract and `InMemory*` for behavior that intentionally keeps state.
5. Bounded background dispatch uses an injected `TaskScheduler` and `PriorityQueue<TElement,TPriority>`. Do not hand-sort mutable work lists or call `Task.Run` inside domain services.
6. Routine database work uses `CipherBankDbContext` and EF Core. Compatibility SQL is centralized in `CipherBank-app.Core/Persist/Sql/LocalDbSql.cs`; no other production file owns SQL command text.
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
- Manrope is the interface-copy family, Space Grotesk owns display and financial hierarchy, and Space Mono is reserved for PIN/code/status roles.
- Interactive targets remain at least 44×44 device-independent units. Meaning is never conveyed by color alone.

Copy from `templates/ui/` for new pages, `templates/e2e/` for executable stories, and `templates/service/` or `templates/config/` for new capabilities. Update the template in the same change when a repository convention changes.

## Agentic dispatch and feature modules

`config/agentic/dispatch.json` is the machine-readable routing map for repository work. Start a non-trivial feature with `cipherbank-dispatch`, select the smallest focused workflow, and keep its dispatch packet with the implementation evidence rather than production source.

- Cross-layer feature registration uses one `Add__FEATURE__Feature` extension at the MAUI composition root. Registration is explicit; do not discover modules with reflection or resolve services through a locator.
- Runtime services still use focused interfaces and constructor injection. A composition module is an assembly-time registration boundary, not a dependency bag or runtime service.
- Shared resources remain owned by their canonical subsystem. Features reference shared styles, options, ports, and adapters; they do not copy them into feature-local folders.
- Feature-local resources live below the owning feature and are promoted to the shared resource dictionary only when the role repeats across features.
- Reusable work orders and module/resource scaffolds live under `templates/dispatch/`, `templates/feature/`, and `templates/resource/`.
- `docs/agentic/README.md` defines the dispatch lifecycle; `docs/agentic/RESOURCE_OWNERSHIP.md` defines placement and access rules.

## Quality and Sonar

- `TreatWarningsAsErrors` remains enabled. Allow lists are narrow, documented, and shrinking; NuGet resolution, nullable, cancellation, and security warnings are not parked.
- `scripts/validate-structure.sh` is the architecture gate. It checks central packages, assembly metadata, SQL ownership, required contracts, design-system files, and retired terminology.
- Local C# Sonar parity is opt-in through `./scripts/lint-csharp.sh`; CI Sonar remains the merge authority.
- Sonar must analyze production folders and interfaces. Exclusions require a specific generated/vendor reason and the narrowest possible path.
- Resolve findings on the earliest stack layer that owns the code, then merge upward.

## Required verification

Run the narrowest relevant test first, then the complete gates for a review-ready change:

```bash
bash scripts/validate-structure.sh
./scripts/lint.sh
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj /p:CollectCoverage=false
dotnet build CipherBank-app.ChallengePass/CipherBank-app.ChallengePass.csproj
dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-android
dotnet test CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj
```

Changes to XAML resources also require a light/dark visual pass at compact and large text settings. Changes to persistence require clean-database and upgrade-shape tests. Changes to ChallengePass require A1, A2, malformed-input, cancellation, and zeroization coverage.

## E2E and sensitive artifacts

- Appium story tests use stable `CB-*`/`US-*` traits and fail if a selected wave resolves zero tests.
- Device-bound facts run through `StoryRunner`, fail when `E2E_RUN=1`, and write a gap note before rethrowing.
- Emulator, Appium, APK-install, diagnostics, and recovery-file work belongs to dedicated lifecycle objects behind the harness façade, not inside story bodies.
- Package reset uses `adb shell pm clear com.companyname.cipherbankapp`.
- Journals, recovery pulls, and diagnostics belong under gitignored `artifacts/` and must never be committed.
- PINs, mnemonics, tokens, keys, PANs, and full bank coordinates are never logged in production.

## Licensing

Do not add or change a repository license, file-header ownership policy, or third-party attribution without the owner's explicit choice. Package additions require license and maintenance review.
