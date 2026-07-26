# Task 8 Report: US-ONB-03 + US-ONB-04 onboarding negatives (device)

**Status:** DONE

## What changed

- `CipherBank-app.E2ETests/Stories/StoryIds.cs` — added `UsOnb03 = "US-ONB-03"`.
- `CipherBank-app.E2ETests/Tests/AccountStories.cs` — added
  `US_ONB_03_WrongQuizWords_BlocksAdvance` (new Fact) and doc comments (purpose/Use/Scope,
  per AGENTS.md) on it and on the pre-existing `US_ONB_04_PinMismatch_BlocksSeal`, which I
  verified but did not need to modify — it already used the journal PIN, asserted the error
  label, and flushed the journal per the brief.
- `CipherBank-app.E2ETests/Stories/StoryCatalog.cs` — extended the `CB-ACCOUNT-001` entry's
  `MauiSurface` note to record that US-ONB-03/04 passed the Task 8 device run (no standalone
  catalog rows exist for bare `US-*` ids; they're tracked as part of the CB-ACCOUNT-001 flow,
  consistent with how US-ONB-04 was already handled).
- `docs/tests/STORY_ID_MAP.md` — added an **Executable** row for US-ONB-03, upgraded the
  US-ONB-04 row to **Executable** with device confirmation, and added `--story US-ONB-03`
  / `--story US-ONB-04` harness examples.

No page-object or app (Shell) changes were needed: `BackupQuizPage.AnswerWrong()` /
`VerifyExpectingError()` / `IsErrorDisplayed()` and `SetPinPage.SealMismatch()` /
`IsErrorDisplayed()` already existed and map to real app guards
(`BackupQuizViewModel.VerifyAsync`'s word-mismatch branch → `BackupQuizErrorLabel`,
`SetPinViewModel.SealAsync`'s `Pin != ConfirmPin` branch → `SetPinErrorLabel`) — confirmed
by reading the XAML/ViewModels before wiring the Fact, so there is no fake UI here.

## Test logic

- **US-ONB-03** (`US_ONB_03_WrongQuizWords_BlocksAdvance`): Fresh device → Welcome → Create
  wallet → Keys → Continue to BackupQuiz → fill every prompt with a deliberately wrong word
  (`AnswerWrong`) → tap Verify (`VerifyExpectingError`, no page-type change) → assert
  `BackupQuizErrorLabel` is displayed AND `BackupQuizPage.IsLoaded()` is still true (i.e. the
  user was **not** advanced to SetPin). Journal flushed under `US-ONB-03.journal.txt`.
- **US-ONB-04** (pre-existing, verified unchanged): Fresh device → Welcome → Keys → BackupQuiz
  (answered correctly) → SetPin → seal with PIN=journal PIN, confirm=journal PIN+"9" (mismatch)
  → assert `SetPinErrorLabel` is displayed. Journal flushed under `US-ONB-04.journal.txt`.

## Device runs (CipherBank_API34, emulator-5554, already booted)

```
$ ./scripts/e2e-android.sh --story US-ONB-03
==> Running: dotnet test ... --filter FullyQualifiedName~US_ONB_03
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 17 s

$ ./scripts/e2e-android.sh --story US-ONB-04
==> Running: dotnet test ... --filter FullyQualifiedName~US_ONB_04
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 20 s

$ ./scripts/e2e-android.sh --wave account   # regression check: CB-ACCOUNT-001 still green
==> Running: dotnet test ... --filter FullyQualifiedName~CB_ACCOUNT
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 25 s
```

Journals (PIN/mnemonic redacted here; real values only ever live under the git-ignored
`artifacts/e2e-journal/`):

```
story=US-ONB-03
2026-07-26T00:24:39Z device: cleared app data (Fresh)
2026-07-26T00:24:51Z device: confirmed wrong quiz words block advance to SetPin

story=US-ONB-04
2026-07-26T00:25:08Z device: cleared app data (Fresh)
2026-07-26T00:25:24Z device: confirmed mismatched PIN blocks seal
```

## Non-device path

```
$ dotnet test CipherBank-app.E2ETests --nologo
Skipped! - Failed: 0, Passed: 0, Skipped: 13, Total: 13, Duration: 45 ms
```

0 failed, both new/verified onboarding-negative Facts show as `[SKIP]` (E2E_RUN unset) rather
than a fake pass — consistent with the rest of the suite.

## Git hygiene

- `git status --porcelain artifacts/` is empty; `artifacts/e2e-journal/*` remains git-ignored
  (`.gitignore` rule pre-existing) so `US-ONB-03.journal.txt` / `US-ONB-04.journal.txt`
  (which contain the journaled PIN/mnemonic) are not tracked.
- Pre-existing unstaged modifications to `.superpowers/sdd/progress.md`,
  `.superpowers/sdd/task-4-report.md`, `.superpowers/sdd/task-6-report.md`,
  `design_handoff_cipherbank/starter/docs/PLAYWRIGHT_PLAN.md`,
  `design_handoff_cipherbank/starter/docs/STORY_ID_MAP.md`, `docs/README.md`, and
  `docs/tests/README.md` were present before this task started and are unrelated to Task 8;
  left untouched and excluded from this commit.

## Self-review notes

- Nesting/branch complexity: the new Fact is straight-line (no loops); `SealAsync` /
  `VerifyAsync` guard branches in app code were pre-existing and untouched.
- Doc comments added to both onboarding-negative Facts state purpose, `Use:` frequency, and
  `Scope:` per AGENTS.md.
- No mass rewrite of legacy files; only the four listed files were modified.
- Confirmed the assertions exercise a real app guard (read `BackupQuizViewModel.cs` /
  `SetPinViewModel.cs` before writing the Fact) rather than asserting against a stub.

## Report back

- **Status:** DONE
- Commit subject: `feat(e2e): add onboarding negative stories US-ONB-03 and US-ONB-04`
  (see `git log -1` for the short SHA; this task's base was `f351479`, no earlier commits
  amended/reset)
- US-ONB-03: **PASS** (1 passed / 0 failed) on `CipherBank_API34`
- US-ONB-04: **PASS** (1 passed / 0 failed) on `CipherBank_API34`
- Concerns: none — both negatives assert against real ViewModel guards and error-label
  AutomationIds that already existed in the Shell.
- Report file: `.superpowers/sdd/task-8-report.md`
