# Appium E2E contract

This contract supplements the repository root for `CipherBank-app.E2ETests`.
Characterize existing observable journeys before refactoring them. Reserve this
tier for high-value user stories; exhaustive branch coverage belongs in
`CipherBank-app.Tests`.

## Ownership and process boundaries

- The project owns story inventory, page objects, device profiles, Appium session lifecycle, host-side diagnostics, and failure evidence.
- Blocking or independently running work—emulator boot, Appium startup, APK install, ADB pulls, log capture, and long polls—stays in dedicated support objects or the root harness script. Story facts call a small façade.
- Page objects own selectors and screen actions. Story facts own user intent and assertions. Do not duplicate selectors inside tests.
- State that spans steps belongs to `StoryJournal`, `DeviceState`, the shared fixture, or another focused owner; do not use static mutable bags.

## Story execution

- Executable stories carry one or more stable `[Trait("Story", StoryIds.*)]` attributes. Harness filters use `Story=...`, then preflight discovery and fail when a filter matches zero tests.
- Device facts use `SkippableFact` only to report a real skipped result when `E2E_RUN` is absent. Once enabled, missing prerequisites and story failures fail the run.
- Wrap every executable device body in `StoryRunner`; it records `docs/tests/gaps/` evidence and rethrows.
- Establish the declared device profile before exercising the story. Reset with package `com.companyname.cipherbankapp`, never the Expo package id.
- Each harness **session** uninstalls then reinstalls so PIN and LocalDb do not survive across installs (new-device). Fresh stories also `pm clear` in-process. Sealed smoke uses `noReset` only after that session wipe.

## Documentation and complexity

- New or materially changed methods state purpose, normal-use frequency (`High`, `Medium`, or `Low`), and scope.
- Keep loops at no more than two levels. Use dictionaries for stable story, wave, status, or platform dispatch instead of long conditional chains.
- One primary type lives in each C# file and the filename matches it.

## Sensitive artifacts

- PINs, mnemonics, recovery passwords, page dumps, and pulled recovery files are synthetic lab data but still sensitive. Read them from environment/gitignored files and retain them only under `artifacts/` or an explicit temporary directory.
- Never commit journals, recovery files, screenshots containing secrets, or real credentials. Production code must never expose the diagnostic journaling behavior.

## Verification

```bash
dotnet test CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj
./scripts/e2e-android.sh --wave account
bash scripts/validate-structure.sh
```

The first command runs host contracts while device facts skip when `E2E_RUN` is absent. Run the Appium command from the repository root on a configured Android host.
