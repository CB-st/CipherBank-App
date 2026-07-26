# Task 4 report: AppiumFixture, EmulatorReset, DeviceState

## Status

Complete.

## Files created

- `CipherBank-app.E2ETests/Support/AppiumFixture.cs` — `AppiumFixture.CreateOrThrow()` returns `null` when
  `E2E_RUN` is unset (tests Skip), and throws when `E2E_RUN=1` but the Appium server is unreachable, the
  platform has no driver factory, or the APK/app path env var / file is missing (fail fast, no soft pass).
  Platform → driver-builder dispatch uses a `Dictionary<string, Func<Uri, AppiumDriver>>` (`android`/`ios`)
  instead of if/else. Owns the `AppiumDriver` + a `StoryJournal`; `Dispose()` quits/disposes the driver.
- `CipherBank-app.E2ETests/Support/EmulatorReset.cs` — `ClearAppData(package?)` runs
  `adb shell pm clear <package>` (package resolved from `CB_MAUI_PACKAGE`, default
  `com.companyname.cipherbankapp`) and throws if adb doesn't report `Success`.
- `CipherBank-app.E2ETests/Support/DeviceState.cs` — `DeviceProfile { Fresh, Sealed }` enum;
  `DeviceState.FreshAsync()` clears app data then relaunches via `IInteractsWithApps.ActivateApp` to land on
  Welcome; `SealedAsync()` = Fresh + drives Welcome→Keys→Quiz→SetPin through the real page objects using the
  journal PIN, journaling the mnemonic shown on Keys.

## Files staged for compile (page objects, WIP from earlier session)

`PageObjects/WelcomePage.cs`, `KeysPage.cs`, `BackupQuizPage.cs`, `SetPinPage.cs` — required by
`DeviceState.SealedAsync()`; staged as-is (no rewrites). `HomePage.cs` was already tracked, untouched.

Not staged/committed: `Stories/`, `Tests/StoryBacklogTests.cs`, `Tests/CoraShellSmokeTests.cs` (Task 5+ scope).

## Build

`dotnet build CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj -v q` → **0 errors**, 95 pre-existing
style warnings (StyleCop/CA), none introduced by new files beyond the same conventions already present
project-wide (e.g. missing file-header banners, matches existing `Support/*.cs` style).

## Commits

`f87f4a6` — `feat(e2e): add Appium fixture and device state profiles`
(`Support/AppiumFixture.cs`, `Support/EmulatorReset.cs`, `Support/DeviceState.cs` +
`PageObjects/{WelcomePage,KeysPage,BackupQuizPage,SetPinPage}.cs`)

## Test summary

No automated tests executed (no emulator/Appium available in this environment); verified via successful
`dotnet build` only, as instructed for this task.

## Concerns

- `DeviceState.RelaunchApp` casts to `IInteractsWithApps.ActivateApp(package)`; confirmed the interface/method
  exists in Appium.WebDriver 5.0.0 via package XML docs, but it has not been exercised against a real
  Android/iOS driver yet — worth a first live-emulator smoke pass once Task 5+ wires an actual Fact through
  `DeviceState`.
- `AppiumFixture.EnsureAppiumServerReachable` does a synchronous `HttpClient` call inside a static factory
  (`.GetAwaiter().GetResult()`); acceptable here since `CreateOrThrow()` is called once per fixture from
  synchronous xUnit collection-fixture setup, but flag if a future caller needs this on a hot path.
- Did not add file-header banners (SA1633) to new files since no existing file in the project has one either
  (pre-existing convention gap, not introduced by this task).
