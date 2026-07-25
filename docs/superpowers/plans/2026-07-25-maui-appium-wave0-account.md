# MAUI Appium Wave 0–1 (Harness + Account) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship a reproducible Android Appium harness and make CB-ACCOUNT-001 / CB-ACCOUNT-002 (plus PIN mismatch, wrong quiz, and PIN change) completable on the MAUI Shell, with failures producing gap notes that drive the next feature work.

**Architecture:** Vertical slices. Dedicated Support objects own in-memory session state (driver, journal, device profile). Emulator boot, Appium server, and APK install run as separate process-backed objects behind `scripts/e2e-android.sh`. Story Facts call page objects; soft-returns under `E2E_RUN=1` are removed.

**Tech Stack:** .NET 10 MAUI (`net10.0-android`), xUnit, Appium.WebDriver 5, UiAutomator2, AVD `CipherBank_API34`, bash harness.

**Spec:** `docs/superpowers/specs/2026-07-25-maui-appium-story-completion-design.md`

## Global Constraints

- Follow repo-root `AGENTS.md`: every function documents logical purpose + Use High|Medium|Low + Scope; in-memory ownership on dedicated objects; background/separate-process for boot/Appium/install; max 2 loop layers; prefer ternary and dictionary dispatch.
- MAUI package id: `com.companyname.cipherbankapp` (wipe with `adb shell pm clear com.companyname.cipherbankapp`).
- Journal PINs/mnemonics in test artifacts (dev/test builds).
- `E2E_RUN=1` → skip not allowed as silent pass; missing harness → fail fast; story gaps → Fact fails + `docs/tests/gaps/CB-*.md`.
- Default journaled PIN via `E2E_TEST_PIN` (document default `246810`); PIN-change story must set a second journaled PIN and continue with it.
- Do not mass-rewrite untouched legacy files; upgrade touched functions to AGENTS.md style.

---

## File map

| Path | Responsibility |
|------|----------------|
| `AGENTS.md` | Coding standards (already added) |
| `scripts/e2e-android.sh` | Boot AVD, build/install APK, start Appium, run filter |
| `scripts/lib/android-env.sh` | Shared ANDROID_HOME / PATH exports |
| `CipherBank-app.E2ETests/Support/StoryJournal.cs` | In-memory + on-disk journal (PIN, mnemonic, steps) |
| `CipherBank-app.E2ETests/Support/AppiumFixture.cs` | Driver lifecycle façade |
| `CipherBank-app.E2ETests/Support/EmulatorReset.cs` | `pm clear` / reinstall helpers |
| `CipherBank-app.E2ETests/Support/DeviceState.cs` | Fresh / Sealed profile setup |
| `CipherBank-app.E2ETests/Support/GapNotes.cs` | Write `docs/tests/gaps/CB-*.md` on failure |
| `CipherBank-app.E2ETests/Stories/StoryProcedures.cs` | Imported CB-ACCOUNT steps |
| `CipherBank-app.E2ETests/Tests/AccountStories.cs` | Account Facts |
| `CipherBank-app/.../ChangePin*` (if missing) | Shell UI for PIN change (gap-driven) |
| `docs/tests/e2e-tests.md`, `STORY_ID_MAP.md` | Runner docs |
| `docs/tests/gaps/` | Failure → next-plan queue |

---

### Task 1: Confirm AGENTS.md propagation

**Files:**
- Verify: `AGENTS.md`
- Verify: `design_handoff_cipherbank/starter/AGENTS.md`
- Verify: `docs/tests/e2e-tests.md` (pointer)
- Verify: `docs/superpowers/specs/2026-07-25-maui-appium-story-completion-design.md` (points to AGENTS.md)

**Interfaces:**
- Consumes: none
- Produces: coding-standard contract for all later tasks

- [ ] **Step 1: Confirm the three sections exist in root AGENTS.md**

Run: `grep -n "Function documentation\|Object ownership\|Complexity limits" AGENTS.md`

Expected: three section headings present.

- [ ] **Step 2: Confirm Expo starter AGENTS.md has the shared Coding standards block**

Run: `grep -n "Coding standards (shared with MAUI" design_handoff_cipherbank/starter/AGENTS.md`

Expected: match.

- [ ] **Step 3: Commit if not already committed with this PR’s docs**

```bash
git add AGENTS.md design_handoff_cipherbank/starter/AGENTS.md \
  docs/tests/e2e-tests.md \
  docs/superpowers/specs/2026-07-25-maui-appium-story-completion-design.md
git commit -m "$(cat <<'EOF'
docs: adopt AGENTS.md coding standards for Shell and E2E

EOF
)"
```

---

### Task 2: Android env helper + e2e harness script

**Files:**
- Create: `scripts/lib/android-env.sh`
- Create: `scripts/e2e-android.sh`
- Modify: `docs/tests/e2e-tests.md` (document script)

**Interfaces:**
- Consumes: AVD `CipherBank_API34`, `~/.local/dotnet/dotnet`, `~/Android/Sdk`
- Produces: `./scripts/e2e-android.sh --story CB-ACCOUNT-001` runnable entrypoint

- [ ] **Step 1: Create `scripts/lib/android-env.sh`**

```bash
#!/usr/bin/env bash
# Exports Android + .NET paths for E2E harness.
# Use: High (every e2e script invocation). Scope: process-wide shell env.
export ANDROID_HOME="${ANDROID_HOME:-$HOME/Android/Sdk}"
export ANDROID_SDK_ROOT="$ANDROID_HOME"
export DOTNET_ROOT="${DOTNET_ROOT:-$HOME/.local/dotnet}"
export PATH="$DOTNET_ROOT:$ANDROID_HOME/emulator:$ANDROID_HOME/platform-tools:$ANDROID_HOME/cmdline-tools/latest/bin:$PATH"
export CB_MAUI_PACKAGE="${CB_MAUI_PACKAGE:-com.companyname.cipherbankapp}"
export CB_AVD="${CB_AVD:-CipherBank_API34}"
```

- [ ] **Step 2: Create `scripts/e2e-android.sh`** with dictionary-style arg dispatch (`--story`, `--wave`, `--all`), starting emulator only if `adb devices` has no `emulator-`, waiting for `sys.boot_completed`, building with `EmbedAssembliesIntoApk=true`, locating APK under `CipherBank-app/bin/Debug/net10.0-android/`, `adb install -r`, starting Appium on `:4723` if needed, then:

```bash
E2E_RUN=1 TEST_PLATFORM=android \
  ANDROID_APK_PATH="$APK" \
  E2E_TEST_PIN="${E2E_TEST_PIN:-246810}" \
  E2E_JOURNAL_DIR="${E2E_JOURNAL_DIR:-artifacts/e2e-journal}" \
  dotnet test CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj \
    --filter "$FILTER" --nologo
```

Every bash function in the script must have a one-line comment: purpose + Use + Scope.

- [ ] **Step 3: Make executable and dry-run help**

Run: `chmod +x scripts/e2e-android.sh scripts/lib/android-env.sh && ./scripts/e2e-android.sh --help`

Expected: prints usage including `--story`, `--wave`, `--all`.

- [ ] **Step 4: Commit**

```bash
git add scripts/lib/android-env.sh scripts/e2e-android.sh docs/tests/e2e-tests.md
git commit -m "$(cat <<'EOF'
chore: add MAUI Android Appium e2e harness script

EOF
)"
```

---

### Task 3: StoryJournal + GapNotes support objects

**Files:**
- Create: `CipherBank-app.E2ETests/Support/StoryJournal.cs`
- Create: `CipherBank-app.E2ETests/Support/GapNotes.cs`
- Create: `docs/tests/gaps/.gitkeep`

**Interfaces:**
- Consumes: `E2E_JOURNAL_DIR`, `E2E_TEST_PIN`
- Produces: `StoryJournal` with `Pin`, `AlternatePin`, `Mnemonic`, `RecordStep`, `Flush`; `GapNotes.Write(storyId, step, expected, actual, proposedFix)`

- [ ] **Step 1: Add `StoryJournal.cs`**

```csharp
namespace CipherBank_app.E2ETests.Support;

/// <summary>
/// Holds in-memory credentials and step log for one E2E run; flushes to disk for diagnosis.
/// Use: High (every device story). Scope: per-test / per-fixture session.
/// </summary>
public sealed class StoryJournal
{
    public string Pin { get; private set; }
    public string AlternatePin { get; private set; }
    public string? Mnemonic { get; private set; }
    private readonly List<string> _steps = new();
    private readonly string _dir;

    public StoryJournal(string? pin = null, string? alternatePin = null, string? dir = null)
    {
        Pin = pin ?? Environment.GetEnvironmentVariable("E2E_TEST_PIN") ?? "246810";
        AlternatePin = alternatePin
            ?? Environment.GetEnvironmentVariable("E2E_TEST_PIN_ALT")
            ?? "135791";
        _dir = dir
            ?? Environment.GetEnvironmentVariable("E2E_JOURNAL_DIR")
            ?? "artifacts/e2e-journal";
    }

    /// <summary>Stores mnemonic for quiz/PIN flows. Use: Medium. Scope: account stories.</summary>
    public void SetMnemonic(string mnemonic) => Mnemonic = mnemonic;

    /// <summary>Swaps active PIN after a successful change-PIN flow. Use: Medium. Scope: account stories.</summary>
    public void PromoteAlternatePin() => (Pin, AlternatePin) = (AlternatePin, Pin);

    /// <summary>Appends a journal line. Use: High. Scope: per-story session.</summary>
    public void RecordStep(string line) => _steps.Add($"{DateTimeOffset.UtcNow:o} {line}");

    /// <summary>Writes journal file including PIN/mnemonic for emulator diagnosis. Use: High. Scope: process artifacts.</summary>
    public void Flush(string storyId)
    {
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, $"{storyId}.journal.txt");
        File.WriteAllLines(path, new[]
        {
            $"story={storyId}",
            $"pin={Pin}",
            $"altPin={AlternatePin}",
            $"mnemonic={Mnemonic ?? "(none)"}",
        }.Concat(_steps));
    }
}
```

- [ ] **Step 2: Add `GapNotes.Write`**

Writes/overwrites `docs/tests/gaps/{storyId}.md` with story ID, broken step, expected, actual, proposed fix. Comment: Use Medium, Scope repo docs.

- [ ] **Step 3: Build E2E project**

Run: `export PATH="$HOME/.local/dotnet:$PATH" && dotnet build CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj -v q`

Expected: 0 errors.

- [ ] **Step 4: Commit**

```bash
git add CipherBank-app.E2ETests/Support/ docs/tests/gaps/.gitkeep
git commit -m "$(cat <<'EOF'
feat(e2e): add story journal and gap-note writers

EOF
)"
```

---

### Task 4: AppiumFixture, EmulatorReset, DeviceState

**Files:**
- Create: `CipherBank-app.E2ETests/Support/AppiumFixture.cs`
- Create: `CipherBank-app.E2ETests/Support/EmulatorReset.cs`
- Create: `CipherBank-app.E2ETests/Support/DeviceState.cs`

**Interfaces:**
- Consumes: `StoryJournal`, env `E2E_RUN`, `ANDROID_APK_PATH`, `CB_MAUI_PACKAGE`
- Produces: `AppiumFixture.CreateOrThrow()`, `EmulatorReset.ClearAppData()`, `DeviceState.FreshAsync` / `SealedAsync`

- [ ] **Step 1: Implement `AppiumFixture`**

- If `E2E_RUN` unset → return null fixture / tests use xUnit Skip
- If `E2E_RUN=1` and Appium/APK missing → **throw** (fail fast)
- Owns `AppiumDriver` in memory; `Dispose` quits driver
- Prefer dictionary for platform options (`android` / `ios` → factory delegates) instead of long if/else

- [ ] **Step 2: Implement `EmulatorReset.ClearAppData`**

Runs `adb shell pm clear com.companyname.cipherbankapp` (package from env). Use: Medium. Scope: Fresh profile.

- [ ] **Step 3: Implement `DeviceState`**

```csharp
public enum DeviceProfile { Fresh, Sealed }

/// <summary>
/// Establishes deterministic device custody state before a story Fact.
/// Use: High (each account/device story). Scope: Appium session.
/// </summary>
public sealed class DeviceState
{
    // Fresh: clear + launch → Welcome
    // Sealed: Fresh + run Welcome→Keys→Quiz→SetPin using journal PIN; journal mnemonic
}
```

Loop nesting: quiz fill stays in `BackupQuizPage.AnswerFromMnemonic` (already ≤2 layers).

- [ ] **Step 4: Build**

Run: `dotnet build CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj -v q`

Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add CipherBank-app.E2ETests/Support/
git commit -m "$(cat <<'EOF'
feat(e2e): add Appium fixture and device state profiles

EOF
)"
```

---

### Task 5: Remove soft-return false-greens from smoke / onboarding Facts

**Files:**
- Modify: `CipherBank-app.E2ETests/Tests/CoraShellSmokeTests.cs`
- Create: `CipherBank-app.E2ETests/Tests/AccountStories.cs` (stub Facts moved here)

**Interfaces:**
- Consumes: `AppiumFixture`, `DeviceState`, `StoryJournal`
- Produces: Facts that Skip when `E2E_RUN` unset; Fail when run requested but preconditions wrong

- [ ] **Step 1: Refactor gating**

Replace `if (!CanRun) return;` with:

```csharp
if (!_fixture.IsEnabled)
{
    // Explicit skip — not a pass
    return; // prefer Xunit.Skip.If or Assert.Skip(.NET) if available on net10
}
```

Prefer `Assert.Skip("E2E_RUN not set")` when the test SDK supports it; otherwise use `[Fact(Skip=...)]` only for permanently parked stories — runtime skip for disabled E2E.

When `E2E_RUN=1` and Welcome expected but Unlock shown → fail + `GapNotes.Write(...)`.

- [ ] **Step 2: Move CB-ACCOUNT-001 / US-ONB-04 into `AccountStories.cs`** using `DeviceState.Fresh` + journal.

- [ ] **Step 3: `dotnet test` without E2E_RUN**

Run: `dotnet test CipherBank-app.E2ETests --nologo`

Expected: skips or explicit skip messages; **0 failed**.

- [ ] **Step 4: Commit**

```bash
git add CipherBank-app.E2ETests/Tests/
git commit -m "$(cat <<'EOF'
fix(e2e): stop soft-passing device stories when E2E_RUN is off or wrong

EOF
)"
```

---

### Task 6: StoryProcedures for CB-ACCOUNT-* + catalog update

**Files:**
- Create: `CipherBank-app.E2ETests/Stories/StoryProcedures.cs`
- Modify: `CipherBank-app.E2ETests/Stories/StoryCatalog.cs`
- Copy reference: import step titles from scaffold `docs/USER_STORIES.md` / zip catalog (CB-ACCOUNT-001/002)

**Interfaces:**
- Consumes: scaffold procedure text
- Produces: `StoryProcedures.Account001Steps` / `Account002Steps` as ordered id→description maps (dictionary)

- [ ] **Step 1: Add procedure dictionaries** keyed by step id (`open`, `complete-form`, …) for Account 001/002.

- [ ] **Step 2: Set catalog status** for CB-ACCOUNT-001 → Executable (after Task 7 green); until then Partial/Blocked with surface note. Include state profile field if extending `StoryEntry`.

- [ ] **Step 3: Commit**

```bash
git add CipherBank-app.E2ETests/Stories/
git commit -m "$(cat <<'EOF'
feat(e2e): import CB-ACCOUNT procedures into StoryProcedures

EOF
)"
```

---

### Task 7: Prove Wave 0 canary — CB-ACCOUNT-001 on emulator

**Files:**
- Modify: `AccountStories.cs` (full create flow)
- Modify: page objects as needed (`WelcomePage`, `KeysPage`, `BackupQuizPage`, `SetPinPage`) — AGENTS.md comments on touched methods
- Modify: `docs/tests/STORY_ID_MAP.md`

**Interfaces:**
- Consumes: harness script, DeviceState.Fresh, StoryJournal
- Produces: green `CB_ACCOUNT_001` on `CipherBank_API34`

- [ ] **Step 1: Start harness canary**

Run: `./scripts/e2e-android.sh --story CB-ACCOUNT-001`

Expected: AVD up, APK installed, Appium up, test executes.

- [ ] **Step 2: If fails, write gap note and fix Shell/AutomationId/page object until green**

Gap path: `docs/tests/gaps/CB-ACCOUNT-001.md`

- [ ] **Step 3: Confirm journal file contains PIN + mnemonic**

Run: `ls artifacts/e2e-journal/ && head artifacts/e2e-journal/*ACCOUNT*`

Expected: journaled pin/mnemonic present.

- [ ] **Step 4: Commit**

```bash
git add CipherBank-app.E2ETests/ CipherBank-app/Views/ docs/tests/ artifacts/e2e-journal/.gitignore
git commit -m "$(cat <<'EOF'
feat(e2e): complete CB-ACCOUNT-001 create-account on Android emulator

EOF
)"
```

---

### Task 8: US-ONB-03 wrong quiz + US-ONB-04 PIN mismatch (device)

**Files:**
- Modify: `AccountStories.cs`
- Modify: `BackupQuizPage.cs` / `SetPinPage.cs` if needed

- [ ] **Step 1: Fact `US_ONB_03_WrongQuizWords_BlocksAdvance`** — `AnswerWrong` → `VerifyExpectingError` → assert error; must not reach SetPin.

- [ ] **Step 2: Fact `US_ONB_04_PinMismatch_BlocksSeal`** — already sketched; ensure uses journal PIN + mismatch; assert error; journal flush.

- [ ] **Step 3: Run**

`./scripts/e2e-android.sh --wave account` (or filter both Facts)

Expected: both green on Fresh device (may need clear between Facts — fixture handles).

- [ ] **Step 4: Commit**

```bash
git commit -am "$(cat <<'EOF'
feat(e2e): add onboarding negative stories US-ONB-03 and US-ONB-04

EOF
)"
```

---

### Task 9: PIN change (Shell UI if missing + story)

**Files (likely create — Shell currently has no Change-PIN surface):**
- Create: `CipherBank-app/Views/ChangePinPage.xaml` (+ code-behind)
- Create: `CipherBank-app/ViewModels/ChangePinViewModel.cs`
- Modify: `AppShell.xaml`, `Routes.cs`, `MauiProgram.cs`, `ProfilePage` entry point
- Create: `CipherBank-app.E2ETests/PageObjects/ChangePinPage.cs`
- Modify: `AccountStories.cs` — Fact for change PIN
- Unit tests in `CipherBank-app.Tests` for ViewModel validation

**Interfaces:**
- Consumes: `IPinService` / `IAppSession` (extend if needed with `ChangePinAsync(old, new)`)
- Produces: user can change PIN; E2E journals old→new and unlocks with new PIN

- [ ] **Step 1: Write failing unit test for ChangePinViewModel** (mismatch / too short / success)

- [ ] **Step 2: Implement ViewModel + page + route + Profile button with AutomationIds (`ChangePinEntry`, `ChangePinConfirmEntry`, `ChangePinSubmitButton`, …)

All new methods: AGENTS.md comment style.

- [ ] **Step 3: E2E Fact `CB_ACCOUNT_PIN_CHANGE_DynamicPin`:**
  1. DeviceState.Sealed with journal Pin
  2. Navigate Profile → Change PIN
  3. Set AlternatePin
  4. `journal.PromoteAlternatePin()`
  5. Lock/unlock (or relaunch) with new Pin
  6. Assert Home

- [ ] **Step 4: Run on emulator; gap-note + fix until green**

- [ ] **Step 5: Commit**

```bash
git commit -am "$(cat <<'EOF'
feat: add Change PIN Shell flow and Appium story with journaled dynamic PIN

EOF
)"
```

---

### Task 10: CB-ACCOUNT-002 recover account

**Files:**
- Create: `CipherBank-app.E2ETests/PageObjects/RestoreBackupPage.cs`
- Modify: `AccountStories.cs`
- Possibly extend export-backup helpers already in Profile for creating a recovery file in Fresh→export→clear→restore path

**Interfaces:**
- Consumes: existing mnemonic backup export UI (Profile), `RestoreBackupPage` AutomationIds
- Produces: green recover Fact or failing gap note that schedules export UX fixes

- [ ] **Step 1: Page object for RestoreBackup**

- [ ] **Step 2: Fact `CB_ACCOUNT_002_RecoverAccount`**
  1. Sealed → export recovery file (journal password)
  2. `pm clear` / Fresh
  3. Welcome → Restore from backup → password → SetPin (or unlock path per Shell)
  4. Assert Home / same custody

- [ ] **Step 3: Run; on failure write `docs/tests/gaps/CB-ACCOUNT-002.md` and implement minimal Shell fixes until green**

- [ ] **Step 4: Update `STORY_ID_MAP.md` + catalog Executable

- [ ] **Step 5: Commit**

```bash
git commit -am "$(cat <<'EOF'
feat(e2e): complete CB-ACCOUNT-002 recover-account story on emulator

EOF
)"
```

---

### Task 11: Docs closeout for Wave 0–1

**Files:**
- Modify: `docs/tests/e2e-tests.md`, `docs/tests/STORY_ID_MAP.md`, `docs/tests/README.md`
- Modify: `docs/README.md` if needed
- Ensure `artifacts/e2e-journal/` is gitignored

- [ ] **Step 1: Document `./scripts/e2e-android.sh`, journal pins, gap notes, package id**

- [ ] **Step 2: Mark Wave 0–1 stories Executable in STORY_ID_MAP

- [ ] **Step 3: Final harness run**

`./scripts/e2e-android.sh --wave account`

Expected: account Facts pass (or only explicit parks skipped).

- [ ] **Step 4: Commit**

```bash
git commit -am "$(cat <<'EOF'
docs(tests): close out Wave 0–1 Appium account story runner

EOF
)"
```

---

## Spec coverage checklist

| Spec requirement | Task |
|------------------|------|
| AGENTS.md style standards | 1 (done in PR prep) |
| `e2e-android.sh` harness | 2 |
| Journal credentials | 3, 7 |
| Gap notes on failure | 3, 7–10 |
| No false-green | 5 |
| Device profiles Fresh/Sealed | 4 |
| CB-ACCOUNT-001 | 7 |
| US-ONB-03 / 04 | 8 |
| Dynamic / change PIN | 9 |
| CB-ACCOUNT-002 | 10 |
| Docs / package id | 2, 11 |

## Placeholder scan

No TBD steps; PIN defaults and package id are explicit. Change-PIN UI is called out as create-if-missing (current Shell has no ChangePin page).
