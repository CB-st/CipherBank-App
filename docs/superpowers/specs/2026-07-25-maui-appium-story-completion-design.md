# MAUI Appium story completion — design

**Date:** 2026-07-25  
**Status:** Draft for review  
**Branch context:** `feat/cora-redesign-maui`  
**Procedure source:** `cipherbank-playwright-scaffold` (Draw.io → `CB-*` catalog)  
**Runner of record:** MAUI Appium (`CipherBank-app.E2ETests`)

## Goal

Make every scaffold `CB-*` user story **completable on the shipping MAUI Shell** via Appium. Stories are the sensor for missing UI, bugs, and next feature plans — not a greenwashing layer. Expo Playwright remains a temporary contract lab until MAUI owns the product; Expo is later deprecated or split into a separate power-user repo against the same backend.

## Approach

**Vertical slices (Approach A):** For each story in dependency order — AutomationIds → page objects → Fact → prove on local emulator → next. Failures produce gap notes that feed implementation plans.

---

## §1 — Story order & definition of done

### Waves

| Wave | Stories | MAUI surface today | Slice goal |
|------|---------|--------------------|------------|
| **0 Harness** | — | Emulator + Appium + APK install | Reproducible `E2E_RUN` / state profiles |
| **1 Account** | CB-ACCOUNT-001, CB-ACCOUNT-002 (+ US-ONB-03/04, PIN change) | Welcome / Keys / Quiz / SetPin / RestoreBackup | Create + recover + PIN dynamics |
| **2 Market** | CB-MARKET-001 | Home charts + Convert | View price / history without money movement |
| **3 Wallets** | CB-WALLET-001, CB-WALLET-002 | Home + `AddWalletPage` | User-controlled + CB checking create |
| **4 Fund** | CB-FUND-001, CB-FUND-002 | Receive (+ confirmations when Shell exposes them) | Fund surfaces asserted end-to-end |
| **5 Pay** | CB-PAY-001, CB-PAY-002, CB-PAY-003 | Pay tab + PosLab | Merchant pay + prepaid/POS |
| **6 Cards** | CB-CARD-001 | Profile vault / cards | Account prepaid create |
| **Parked (explicit)** | CB-CARD-002 (guest), CB-PREPAID-PLACEHOLDER | Guest Shell / blank drawio | Documented park only — not soft-pass |

### Definition of done for a story

1. Scaffold procedure mapped into `StoryCatalog` / `StoryProcedures`
2. Required `AutomationId`s on controls the steps touch
3. Page object(s) + Appium Fact titled with `CB-*` / `US-*`
4. Happy path exercised on emulator (`E2E_RUN=1`; Fresh/Sealed profile as required)
5. Highest-value negative(s) wired where Shell can express them; others tracked as failing/blocked with reason until UI exists
6. `docs/tests/STORY_ID_MAP.md` updated
7. If the story cannot complete: **Fact fails**, gap note written, feeds next feature/bug plan

---

## §2 — Device & Appium harness

### Local runner

Add `scripts/e2e-android.sh` that:

1. Exports `ANDROID_HOME=$HOME/Android/Sdk`, `DOTNET_ROOT=$HOME/.local/dotnet`
2. Starts AVD `CipherBank_API34` if no emulator is connected
3. Waits for boot completion
4. Builds MAUI: `-f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true`
5. Installs the generated APK
6. Starts Appium (UiAutomator2) under repo control
7. Health-checks Appium, runs requested story/filter
8. Retains Appium log, logcat, screenshots, test results, and **story journal** on failure (and optionally always)

Examples:

```bash
./scripts/e2e-android.sh --story CB-ACCOUNT-001
./scripts/e2e-android.sh --wave account
./scripts/e2e-android.sh --all
```

### Package identity

| App | ApplicationId |
|-----|---------------|
| **MAUI (this harness)** | `com.companyname.cipherbankapp` |
| Expo lab (do not use here) | `com.cipherbank.app` |

Wipe / clear for Fresh state:

```bash
adb shell pm clear com.companyname.cipherbankapp
```

### State profiles

Each story declares a profile; a shared fixture establishes it before the Fact:

| Profile | Meaning |
|---------|---------|
| `Fresh` | Reset app data; boots to Welcome |
| `Sealed` | Fresh + complete onboarding through UI (journal PIN/mnemonic) |
| `WalletReady` | Sealed + create required wallet through UI |
| `FundedMock` | WalletReady + deterministic mock/test funding |
| `CardReady` | Prerequisites for card stories through UI |

Stories are independently repeatable; do not rely on accidental leftover emulator state.

### No false-green

| Situation | Result |
|-----------|--------|
| Device run not requested (`E2E_RUN` unset) | Explicit **skip** |
| `E2E_RUN=1` but emulator/Appium/APK missing | **Fail fast** |
| Wrong screen / missing control / unmet criterion | **Fail** + gap note |
| Product explicitly parks a story | Documented **park** in catalog — not a soft pass |

---

## §3 — Architecture, diagnostics, credentials

### Layout

```
CipherBank-app.E2ETests/
  Stories/
    StoryIds.cs
    StoryCatalog.cs          # status + surface + state profile
    StoryProcedures.cs       # CB-* steps/expectations from scaffold
    AutomationIdMap.cs       # Expo bridge while Expo still exists
  Support/
    AppiumFixture.cs
    DeviceState.cs
    EmulatorReset.cs
    StoryJournal.cs          # PIN, mnemonic, step log for diagnosis
  PageObjects/
  Tests/
    AccountStories.cs
    MarketStories.cs
    WalletStories.cs
    FundStories.cs
    PayStories.cs
    CardStories.cs
    StoryBacklogTests.cs
    CoraShellSmokeTests.cs   # thin unlock/parity smoke only
```

### Scaffold → MAUI

- Zip catalog is procedure source (actions, expectations, negatives)
- Map to page objects + Shell assertions
- Do **not** port the Playwright runner into C#

### Failures drive the product backlog

When a story cannot complete:

1. Fact **fails** with a clear assertion
2. Gap note records: story ID, broken step, expected vs actual, proposed UI/feature fix
3. Gap note feeds the next implementation plan
4. `StoryCatalog` may show `Executable` / `Partial` / `Blocked` for inventory, but **Blocked ≠ silent skip** under `E2E_RUN=1`

Only explicitly parked stories (blank drawio, or product-scoped deferrals) stay out of the fail-to-plan loop.

### Credentials & journaling (dev / test builds)

- Use a **known, journaled test PIN** (env-overridable; value recorded in the story journal / run artifacts)
- Cover **PIN change**: set initial PIN → change to a new journaled PIN → unlock/continue with the new value
- Mnemonics, PINs, backup passwords, and related test secrets **may be journaled and retained in logs/screenshots** on emulator/dev builds for diagnosis
- Emulators are not live with sensitive production data; production-style redaction is a later hardening concern, not a current requirement

### Artifacts

- Appium server log
- Failed (and optional per-step) screenshots — `E2E_SCREENSHOT_EACH_STEP=1`
- Story journal (credentials used, step outcomes)
- Gap notes for failed stories

### Wave 0 exit criteria

- `scripts/e2e-android.sh` boots AVD, builds/installs MAUI APK, starts Appium, runs a filter
- Soft-return removed for `E2E_RUN=1` paths
- Package id / wipe / install docs corrected for MAUI
- One canary story proven on the local emulator with journaling on

---

## Implementation style

**Canonical day-to-day copy:** repo-root [`AGENTS.md`](../../../AGENTS.md) (Function documentation · Object ownership & process boundaries · Complexity limits). Expo lab mirror: `design_handoff_cipherbank/starter/AGENTS.md`.

All new/touched code under this design (harness, E2E, Shell, and gap-driven feature work) **must** follow those three sections. When editing legacy files, upgrade touched functions only — no mass rewrite.

---

## Implementation plan boundaries

- **First plan:** Wave 0 (harness) + Wave 1 (account stories, including PIN change and high-value negatives). Prove the loop end-to-end on `CipherBank_API34`.
- **Later plans:** one wave per plan (Market → Wallets → Fund → Pay → Cards), each starting from gap notes left by the previous wave’s failures where applicable.

## Gap notes

Write failing-story gap notes to `docs/tests/gaps/CB-*.md` (one file per story, overwritten/appended per run notes). Format: story ID, broken step, expected vs actual, proposed fix. These are the input queue for the next feature/bug plan.

## Out of scope

- Porting Playwright’s `runStoryStep` harness into C#
- Filling blank `Make Prepaid Purchase.drawio` until product supplies a flow
- Expo feature work beyond what a MAUI story needs to assert
- Production secret redaction / store-build hardening of E2E artifacts
- Permanent product decision on guest prepaid (CB-CARD-002) — park until that decision; failures against a built guest surface still apply if/when it ships

## Related

- Scaffold: `/home/skyrailmaxima/.../UserStories/cipherbank-playwright-scaffold.zip`
- `docs/tests/STORY_ID_MAP.md`, `docs/tests/e2e-tests.md`
- `design_handoff_cipherbank/starter/docs/PLAYWRIGHT_PLAN.md` (Expo lab only)
