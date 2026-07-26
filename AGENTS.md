# AGENTS.md — CipherBank-App (MAUI shipping Shell)

Guidance for agents and developers working in this repository. **MAUI is the product destination.** Expo under `design_handoff_cipherbank/starter/` is a temporary contract lab; it will be deprecated or moved to a separate power-user repo later.

Read first: this file → `docs/tests/STORY_ID_MAP.md` (when touching E2E) → feature-specific plans under `docs/superpowers/`.

## Coding standards (mandatory for new and touched code)

These three rules apply to MAUI Shell, Core, E2E (`CipherBank-app.E2ETests`), and harness scripts. Gap-driven feature work inherits them. When editing legacy code, bring **touched** functions up to this convention; do not mass-rewrite untouched files in the same change.

Canonical copy also lives in `docs/superpowers/specs/2026-07-25-maui-appium-story-completion-design.md` (Implementation style). **This `AGENTS.md` is the day-to-day source of truth** for implementers.

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
- Design: `docs/superpowers/specs/2026-07-25-maui-appium-story-completion-design.md`
- Failures under `E2E_RUN=1` must **fail** (not soft-pass) and produce gap notes in `docs/tests/gaps/`.
- Package wipe for MAUI: `adb shell pm clear com.companyname.cipherbankapp` (not Expo’s `com.cipherbank.app`).
- Dev/test builds may **journal** PINs, mnemonics, and related values for diagnosis.
- JDK: `scripts/lib/android-env.sh` exports `JAVA_HOME` (default `$HOME/.local/jdk-17`, override-respecting) and
  prepends `$JAVA_HOME/bin` to `PATH` — both the MAUI Android build and Appium's UiAutomator2 driver need `java`
  on PATH.

## Expo lab

If you are working inside `design_handoff_cipherbank/starter/`, also follow that folder’s `AGENTS.md` (Expo-specific golden rules). Coding standards above still apply to new/touched TypeScript there.
