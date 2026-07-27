# AGENTS.md — CipherBank-App (MAUI shipping Shell)

Guidance for agents and developers working in this repository. **MAUI is the product destination.** Expo / `design_handoff_cipherbank/` is **out of the MAUI merge path** (no Core/Shell dependency).

Read first: this file → `docs/BUILD_LOG.md` (what shipped and how it connects) → `docs/tests/STORY_ID_MAP.md` (when touching E2E).

## Coding standards (mandatory for new and touched code)

These three rules apply to MAUI Shell, Core, E2E (`CipherBank-app.E2ETests`), and harness scripts. Gap-driven feature work inherits them. When editing legacy code, bring **touched** functions up to this convention; do not mass-rewrite untouched files in the same change.

**This `AGENTS.md` is the day-to-day source of truth** for implementers. Historical design notes are condensed in `docs/BUILD_LOG.md`.

### Function documentation

Every function (including private helpers) gets a brief comment that states:

1. **What it does logically** (purpose, not a line-by-line restatement)
2. **Call frequency:** `High` | `Medium` | `Low` — how often it is expected to run in normal use
3. **Scope:** how far its application reaches (e.g. single page object, per-story fixture, process-wide harness, Shell session)

```csharp
/// <summary>
/// Advances BackupQuiz by filling Word #N prompts from the journaled mnemonic.
/// Use: High (every create/recover path). Scope: BackupQuizPage / account stories.
/// </summary>
```

### Object ownership & process boundaries

- Keep **in-memory state** on dedicated objects whose job is to hold what the app or test run needs to stay smooth (session, journal, device profile, rates cache handles, page objects) — not scattered locals or static bags.
- Work that can block or run independently (emulator boot, Appium server, APK install, logcat, long polls, background sync) lives in **separate objects** that can run as **background / separate processes** from the UI or story Fact path. Callers talk to a small façade; the façade owns lifecycle.
- Prefer one clear owner per concern over god-objects.

### Complexity limits

- **Nesting:** loops at most **two layers deep** in any function. Deeper work is broken into named helpers (each with the documentation convention above).
- **Branches:** prefer **ternary** expressions and **hashmap / dictionary dispatch** (status → handler, story id → procedure, profile → setup) over long `if` / `else if` / `switch` chains when the mapping is stable.
- Keep each function doing one job; extract when a block needs its own frequency/scope note.

## E2E / Appium (story completion)

- Runner of record: `CipherBank-app.E2ETests` (Appium). Story IDs: `CB-*` / `US-*` from the Playwright scaffold catalog.
- Design history: `docs/BUILD_LOG.md`
- Failures under `E2E_RUN=1` must **fail** (not soft-pass) and produce gap notes in `docs/tests/gaps/`.
- Package wipe for MAUI: `adb shell pm clear com.companyname.cipherbankapp` (not Expo’s `com.cipherbank.app`).
- Dev/test builds may **journal** PINs, mnemonics, and related values for diagnosis.
- JDK: `scripts/lib/android-env.sh` exports `JAVA_HOME` (default `$HOME/.local/jdk-17`, override-respecting) and
  prepends `$JAVA_HOME/bin` to `PATH` — both the MAUI Android build and Appium's UiAutomator2 driver need `java`
  on PATH.
- Filter executable stories by `[Trait("Story", …)]` (`Story=CB-…`), never only by method-name substrings.
- Gap notes: wrap Facts in `StoryRunner` / shared failure recorder — do not rely on each path remembering `GapNotes.Write`.
- Keep `TreatWarningsAsErrors=true` with a **narrow, shrinking** `WarningsNotAsErrors` allowlist (style IDs). Never wholesale-disable the gate; never park NU1608 / nullable / CA1001 / CA2016 / CA1305 / security in WNAE permanently.
- Gitignore `artifacts/` (journals, recovery pulls, diagnostics hold PIN/mnemonic-shaped data).

## Sonar typology, stages, and hard-won methods

**Dashboard / artifacts:** https://sonar.cipherbank.money — project `CB-st_CipherBank-App_6f7fd196-021a-4b20-a3f2-9094fa18ab2c`. Prefer CI `sonar-context-<sha>` (`issues.json`) when the PR annotation is missing. Policy softens vs must-fix: `docs/SONAR_GATE.md`. Structural SA1402/SA1649 inventory: `docs/SONAR_STRUCTURAL_PLAN.md`. Stage design: `docs/superpowers/specs/2026-07-26-sonar-stage1-mechanical-design.md`.

### Typology (what shows up on this stack)

| Family | Typical rules | Treat as |
|--------|---------------|----------|
| CRITICAL / HIGH csharp | S2339 public const, S2360 optional params, S1541/S3776 complexity, S1067 compound conditions, S2302, S2365, S131 | **Fix in Stage 1** — do not soft-pass the gate |
| Structural StyleCop | SA1402 one-type-per-file, SA1649 filename matches first type | **Stage 2** — plan callers, then split/rename (same namespace; no API rename unless required) |
| Mechanical style | IDE0008 explicit types, SA1201/SA1202/SA1204 member order, SA1633/SA1636 headers | Stage 1 mechanical / Stage 3 burn-down |
| MAJOR signal | S109 magic numbers (protocol/crypto), S3358 nested ternaries | Name constants / extract helpers |
| Deferred with reason | S6354 UtcNow→IClock, S4055 ResourceManager, S4004/S3956 mutable DTO collections | Documented in `SONAR_GATE.md` — do not “fix” with large redesigns mid-gate |
| INFO / LOW cosmetic | leftover docs/style | Batch only if gate still red |

### Remediation stages (order is mandatory)

1. **Stage 1 — mechanical + CRITICAL:** style levers + HIGH csharp; no SA1402 mass moves yet.
2. **Stage 2 — structural:** fill **callers** (Core, ChallengePass, **Shell**, Tests, E2E), then split/rename per `SONAR_STRUCTURAL_PLAN.md`.
3. **Stage 3 — medium/minor/info:** only after Stage 2 plan is executed/reviewed.

**Landing:** fix on the **earliest owning stack branch** (`maui-m1` Core → `m2` ChallengePass → `m3` Shell → `m4` E2E), then merge up. Do not dump shared Core-only fixes solely onto M4 tip.

### Shell compile gate (non-negotiable)

`dotnet test` green on Core is **not** enough. Every Stage 1/2 change that touches Core/ChallengePass public surface must also:

```bash
dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-android
```

Stage 1 regression we hit: Core gained required `CancellationToken` members; Shell ViewModels still called zero-arg shapes → Shell failed while 267+ tests passed. Compatibility pattern: **zero-token default interface methods (DIMs)** on Core interfaces forwarding to the CT-required member — do **not** reintroduce optional parameters on abstract interface slots (S2360).

### Cross-PR duplication comment

When the canonical fix lives on a later stacked PR but an earlier layer still shows the smell (or a later PR re-touches the same site):

```csharp
// Sonar: issue resolved in M{N} PR (https://github.com/CB-st/CipherBank-App/pull/{N}/…), edit here is duplication
```

Do not spam this on routine mechanical edits that only merge upward without re-editing.

### Stage 2 file structure (SA1402 / SA1649)

- **One primary type per file**; filename matches that type (`IFoo.cs` / `Foo.cs` / `FooResult.cs`).
- Prefer **split only** (extract siblings, same namespace) over type renames.
- New/renamed files: copyright header with matching `file="…"`, plus purpose / Use / Scope on touched public members (this document’s coding standards).
- Before any split: map callers across Shell ViewModels, `MauiProgram`, Tests, and E2E — not only in-project refs.
- After each batch: full unit suite **and** Shell Android build.

### Common missteps (do not repeat)

| Misstep | Consequence | Do instead |
|---------|-------------|------------|
| Change Core API without Shell caller map | Shell stops compiling; tests still green | Search Shell + build MAUI csproj every batch |
| Optional params on interface members | S2360 / Sonar HIGH | Overloads or DIMs with zero-token forwarders |
| `TreatWarningsAsErrors=false` wholesale | New nullability/async smells slip in | Narrow `WarningsNotAsErrors`; shrink over time |
| Suppress NU1608 forever | Dependency lie across the graph | Upgrade packages (e.g. Nethereum ≥ 6.1) or isolate adapter |
| Filter E2E by `FullyQualifiedName~` only | Waves/stories run **zero** Facts silently | `Story=` trait + `--list-tests` preflight fail-on-empty |
| Gap notes only on boot paths | Most Fact failures leave no `docs/tests/gaps/` file | `StoryRunner` around every executable Fact |
| Claim `artifacts/` gitignored without a rule | PIN/mnemonic journals can be committed | Root `.gitignore` entry `artifacts/` |
| Advertise `APPIUM_PORT` but fixture hard-codes 4723 | Harness healthy, tests connect wrong port | Export `APPIUM_SERVER_URL` from the script |
| Mass SA1402 moves before caller plan | Huge churn, broken usings, review fatigue | Fill `SONAR_STRUCTURAL_PLAN.md`, then one folder batch |
| Soften Sonar HIGH threshold | Masks real defects | Fix HIGH; only soften MEDIUM/LOW/INFO per `SONAR_GATE.md` |
| Commit `design_handoff_cipherbank/` or workflow without `workflow` PAT | Wrong merge path / push rejected | Keep Expo handoff out of MAUI PRs; stash workflow until token ready |

### Checklist before pushing Sonar-related work

- [ ] Owning layer is the earliest stack branch that contains the smell
- [ ] Callers mapped for Shell / Tests / E2E when types or signatures move
- [ ] `dotnet test CipherBank-app.Tests` (full) green
- [ ] `dotnet build CipherBank-app -f net10.0-android` green
- [ ] Cross-PR duplication comments only where a later/earlier duplicate edit needs them
- [ ] No new permanent WNAE entries for correctness (NU*/CS86*/CA1001/CA2016/CA1305)
- [ ] New public members documented (purpose / Use / Scope)

