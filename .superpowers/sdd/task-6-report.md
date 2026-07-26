# Task 6 report: StoryProcedures for CB-ACCOUNT-* + catalog update

## Status

Complete.

## What changed

- **`Stories/StoryProcedures.cs`** (new) — `Account001Steps` / `Account002Steps`, each an ordered
  `IReadOnlyDictionary<string, string>` of step id → description, imported verbatim from the Playwright
  scaffold at `/tmp/cb-pw-scaffold/cipherbank-playwright-scaffold/` (`docs/USER_STORIES.md` procedure
  lists, cross-checked against `artifacts/story-manifest.json` `steps[].id`/`steps[].action`). Data only —
  no Playwright runner ported; ids match the brief exactly (`open`, `complete-form`, `submit`, `backup`,
  `complete` for 001; `open`, `enter`, `submit`, `restore`, `complete` for 002).
- **`Stories/StoryCatalog.cs`** — extended `StoryEntry` with an optional trailing
  `DeviceProfile? RequiredProfile` field (from `Support/DeviceState.cs`'s existing `Fresh`/`Sealed` enum;
  `null` default keeps every other positional `new(...)` entry compiling unchanged). Tagged both
  CB-ACCOUNT-001 and CB-ACCOUNT-002 with `DeviceProfile.Fresh` (both start signed-out). Left
  **CB-ACCOUNT-001 status at `Partial`** (not `Executable` — Task 7's emulator canary is the promotion
  gate) and refreshed its surface note to mention the imported procedure + pending canary; CB-ACCOUNT-002
  stays `Backlog` with a note that its procedure is imported but page objects aren't wired.

## Verification

```
export PATH="$HOME/.local/dotnet:$PATH"
dotnet build CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj -v q   # 0 errors
dotnet test CipherBank-app.E2ETests --nologo                              # Failed: 0, Passed: 0, Skipped: 12, Total: 12
```

No `E2E_RUN` set, so the 12 pre-existing Facts/Theory all report `[SKIP]` (unchanged from Task 5) — this
task added no new Facts, only static catalog/data.

## Commits

- `2b59bc4` — `feat(e2e): import CB-ACCOUNT procedures into StoryProcedures`

## Concerns

- None blocking. `StoryProcedures` isn't consumed by any Fact yet (Task 7 will wire `AccountStories` steps
  against these ids); until then it's inert catalog data, so there's no runtime signal to verify beyond
  build+test green.
- `RequiredProfile` is currently only populated for the two CB-ACCOUNT-* entries; other catalog rows
  (`CbFund001`, `CbPay003`, etc.) are left `null` rather than backfilled, since that's outside this task's
  file scope (`StoryCatalog.cs`/`StoryProcedures.cs` only, per brief).
