# Task 9 report — PIN change (Shell UI + Appium story with journaled dynamic PIN)

**Status:** DONE
**Base commit:** `9ab8090`
**Branch:** `feat/cora-redesign-maui`

---

## 1. What shipped

There was no Change-PIN surface in the Shell, so this task created one and then proved it on the emulator.

### Core (`CipherBank-app.Core`)

- `Custody/PinChange.cs` (new) — `PinChangeStatus`, `PinChangeOutcome`, and `PinChangeCoordinator`.
  The coordinator owns the whole change-PIN decision path: a pure `ValidateShape` ternary chain
  (too short → mismatch → same-as-current) followed by verify-then-replace through `IPinService`, with a
  status → message dictionary instead of an if/else chain.
- `Custody/PinService.cs` — added `IPinService.ChangePinAsync(oldPin, newPin)` plus its implementation:
  `VerifyPinAsync(old)` (so failed-attempt/lockout counters apply) then `SetPinAsync(new)`. **No re-seal of
  the custody blob**, matching the existing device-secret architecture in `CustodyService` where the PIN is a
  logical gate, not the AES key.

### Shell (`CipherBank-app`)

- `ViewModels/ChangePinViewModel.cs` (new) — thin binder over `PinChangeCoordinator`; owns only the three
  entry fields plus `Error`/`Status`, wipes PINs on success/cancel/disappear.
- `Views/ChangePinPage.xaml` + `.xaml.cs` (new) — `ChangePinCurrentEntry`, `ChangePinEntry`,
  `ChangePinConfirmEntry`, `ChangePinSubmitButton`, `ChangePinCancelButton`, `ChangePinErrorLabel`,
  `ChangePinStatusLabel`. Both feedback labels bind `IsVisible` through `StringToBoolConverter` (Task 8 pattern),
  so an always-in-tree label cannot fake an assertion.
- `Views/ProfilePage.xaml` — new **Security** card at the top of the Profile scroll view holding
  `ProfileChangePinButton` and `ProfileLockButton`. The stray bottom "Lock app" button was moved into this card
  (no duplicate). Top placement keeps both controls on screen without scrolling for Appium.
- `ViewModels/ProfileViewModel.cs` — `OpenChangePinCommand`.
- `Constants/Routes.cs`, `AppShell.xaml.cs`, `MauiProgram.cs` — `ChangePin` route + `Routing.RegisterRoute`
  + DI for `PinChangeCoordinator` (singleton), `ChangePinViewModel`, `ChangePinPage`.
- `Views/UnlockPage.xaml` — `UnlockErrorLabel` now binds `IsVisible` through `StringToBoolConverter`, so the
  new revoked-PIN assertion is honest rather than passing on an invisible label.

### E2E (`CipherBank-app.E2ETests`)

- `PageObjects/ChangePinPage.cs` (new) — `Submit(current,new,confirm)`, `IsStatusDisplayed()`,
  `IsErrorDisplayed()` (both visible **and** non-empty text), `BackToProfile()`.
- `PageObjects/ProfilePage.cs` — anchors on `ProfileChangePinButton` (top of page, always on screen);
  added `OpenChangePin()` and `LockApp()`.
- `PageObjects/HomePage.cs` — `GoToProfileTab()`.
- `PageObjects/UnlockPage.cs` — `AttemptUnlockExpectingRejection()`, stricter `IsErrorDisplayed()`.
- `Tests/AccountStories.cs` — `CB_ACCOUNT_PIN_CHANGE_DynamicPin` Fact + `SealedHomeOrFail` gap-note guard.
- `Stories/StoryIds.cs` / `StoryCatalog.cs`, `docs/tests/STORY_ID_MAP.md` — updated **after** device green.

---

## 2. TDD evidence

### RED — tests written first, before any implementation

`CipherBank-app.Tests/Custody/PinChangeTests.cs` (6 Facts: confirm mismatch, too short, wrong current PIN,
reuse of current PIN, success swaps the active PIN, and `PinService.ChangePinAsync` verify-before-replace).

```
$ dotnet test CipherBank-app.Tests --nologo --filter "FullyQualifiedName~PinChangeTests"
CipherBank-app.Tests/Custody/PinChangeTests.cs(50,48): error CS0246:
  The type or namespace name 'PinChangeCoordinator' could not be found
  (are you missing a using directive or an assembly reference?)
```

### GREEN — after adding `PinChange.cs` and `IPinService.ChangePinAsync`

```
$ dotnet test CipherBank-app.Tests --nologo --filter "FullyQualifiedName~PinChangeTests"
Passed!  - Failed: 0, Passed: 6, Skipped: 0, Total: 6, Duration: 548 ms
```

(The filtered run exits non-zero purely because coverlet's 70/50/70 thresholds are evaluated against the
6-test subset; the full suite below passes the gate.)

---

## 3. Verification

| Run | Result |
|-----|--------|
| `dotnet test CipherBank-app.Tests --nologo` (full unit suite) | **Passed! Failed: 0, Passed: 254, Total: 254** |
| `dotnet test CipherBank-app.E2ETests --nologo` (no `E2E_RUN`) | **Skipped! Failed: 0, Skipped: 14, Total: 14** |
| `dotnet build CipherBank-app -f net10.0-android -c Debug` | Build succeeded (XAML validated) |
| `dotnet build CipherBank-app.IntegrationTests` | Build succeeded |

### Device — `CipherBank_API34` / `emulator-5554`, package `com.companyname.cipherbankapp`

```
$ ./scripts/e2e-android.sh --story CB-ACCOUNT-PIN-CHANGE
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1, Duration: 1 m 7 s
```

Regression of the whole Fresh-device suite (the Profile/Unlock XAML edits touch Task 7/8 stories):

```
$ dotnet test CipherBank-app.E2ETests --filter "FullyQualifiedName~AccountStories"   # E2E_RUN=1
Passed!  - Failed: 0, Passed: 4, Skipped: 0, Total: 4, Duration: 2 m 8 s
```

(CB-ACCOUNT-001/US-ONB-01, US-ONB-03, US-ONB-04, CB-ACCOUNT-PIN-CHANGE.)

### Journal — dynamic PIN, nothing hard-coded in the Fact

```
story=CB-ACCOUNT-PIN-CHANGE
pin=135791
altPin=246810
2026-07-26T00:54:43Z device: cleared app data (Fresh)
2026-07-26T00:54:48Z device: recorded mnemonic from Keys screen
2026-07-26T00:55:04Z device: sealed vault with PIN=246810
2026-07-26T00:55:18Z device: opened Change PIN with active PIN=246810
2026-07-26T00:55:35Z device: changed PIN 246810 -> 135791 (previous PIN journaled as alternate)
2026-07-26T00:55:40Z device: confirmed replaced PIN=246810 no longer unlocks
2026-07-26T00:55:45Z device: unlocked with new PIN=135791
```

`docs/tests/gaps/` is empty — no gap notes needed. Journal artifacts stay under `artifacts/` and are not committed.

---

## 4. Real gap found and fixed during the device run

The first device run failed at `HomePage.GoToProfileTab()` with a 10 s `NoSuchElementException`. Dumping
`Driver.PageSource` on Home showed why: `AppShell` declares **six** tabs but Android's `BottomNavigationView`
renders only five — Home, Convert, Pay, Send, **More** — so Receive and Profile live behind the "More"
overflow. `GoToProfileTab()` now opens "More" only when Profile is not already on the bar, which keeps it
correct on layouts (tablet/desktop, or a future five-tab Shell) where every tab is visible. This was a genuine
navigation gap in the page objects, not a product defect, so it was fixed rather than gap-noted.

## 5. Assertion strength

The Fact cannot pass on a no-op implementation:

1. `ChangePinStatusLabel` must be **visible and non-empty** (it is `IsVisible`-bound to `Status`).
2. `ChangePinErrorLabel` must be absent.
3. After lock, the **replaced** PIN must be rejected with a visible, non-empty `UnlockErrorLabel` and leave
   the user on Unlock.
4. Only then must the promoted `journal.Pin` reach Home (`HomeTotalUsdLabel`).

PIN values come from `StoryJournal.Pin` / `AlternatePin` (`E2E_TEST_PIN` default `246810`,
`E2E_TEST_PIN_ALT` default `135791`) and are swapped with `PromoteAlternatePin()` — no literals in the Fact.

## 6. Scope notes / concerns

- The story incidentally covers the **wrong-PIN-error** half of US-LCK-02, but not the
  **lockout-after-N-failures** half, so the US-LCK-02 trait was deliberately *not* attached to the Fact;
  the partial coverage is recorded in `StoryCatalog` and `docs/tests/STORY_ID_MAP.md` instead.
- Unit tests target `PinChangeCoordinator` rather than `ChangePinViewModel` directly, because
  `CipherBank-app.Tests` targets `net10.0` and cannot reference the `net10.0-android` MAUI project. This is the
  established pattern in this repo (`BackupQuiz` in Core is tested; `BackupQuizViewModel` is not). All
  Change-PIN decision logic therefore lives in Core and is covered; the ViewModel holds no branch logic of its own.
- Six tabs into a five-slot Android tab bar is a product-side UX smell worth revisiting separately
  (Receive and Profile are both one extra tap away). Out of scope for Task 9; the page object handles it.

---

## Fix follow-up

Review findings from Task 9 addressed on top of `f7014cc`.

### Important 1 — legacy PIN-derived blob safety (custody-level invariant)

Changing the PIN is no longer expressible without going through custody:

- `ICustodyService.ChangePinAsync(oldPin, newPin)` (new) returns `CustodyPinChangeResult`
  (`Changed` / `WrongPin` / `LockedOut` / `DeviceSecretMissing`). It **refuses** the change when
  `CanUnlockWithDeviceOwnerAsync()` is false, because `UnlockAsync` still accepts a legacy PIN-derived blob
  and only migrates it on a successful unlock — a hash-only swap before that would orphan the blob.
  The blob is still **never re-sealed**; the device-secret design is unchanged.
- `PinChangeCoordinator` now depends on `ICustodyService` instead of `IPinService`, and maps custody results to
  `PinChangeStatus` through a dictionary (new `PinChangeStatus.VaultNotReady` →
  "Unlock your wallet before changing your PIN."). The Shell change-PIN flow therefore *cannot* reach the raw
  PIN gate. `MauiProgram` needed no change (both are already singletons).

New unit coverage in `PinChangeTests`: refusal on a legacy blob preserves the old PIN and the mnemonic is still
recoverable with it; the same change succeeds after an unlock has migrated the blob, with the mnemonic intact
under the new PIN; and `CustodyService.ChangePinAsync` reports `DeviceSecretMissing` distinctly from `WrongPin`.
The legacy blob is seeded through `CustodyService.BlobKey`, so Core now has
`<InternalsVisibleTo Include="CipherBank-app.Tests" />`.

### Important 2 — vacuous error assertion is now real device evidence

`CB_ACCOUNT_PIN_CHANGE_DynamicPin` submits a **wrong current PIN first** (derived from the journaled PIN by
`ShiftDigits`, so nothing is hard-coded and it cannot collide with the active or requested PIN) and asserts
`ChangePinErrorLabel` is visible **and** non-empty, that no status is shown, and that the page is still loaded.
The subsequent successful change with the real current PIN proves the rejected attempt preserved state.
`ChangePinPage.Submit()` now waits for either feedback label before returning, so the assertion cannot race the
KDF-bound submit; a wait timeout is swallowed so the assertion — not a bare Appium error — reports the truth.

### Related minors

- `PinChangeCoordinator.ValidateShape` / `ChangeAsync` accept `string?`; a null new PIN is `TooShort`.
  Covered by a 4-case `Theory` plus a `ChangeAsync(null)` Fact.
- `ChangePinViewModel` no longer surfaces `Exception.Message`; it shows a fixed
  "Could not change your PIN. Please try again." and logs the exception through the existing
  `[LoggerMessage]` / `ILogger<T>` pattern already used by `SettingsViewModel` etc. (no new logging stack).
- Removed dead `ProfilePage.HasPrefsCard()` (and its now-unused locator).
- Added purpose + Use/Scope docs to `ChangePinPage.WaitForPageLoad()` (E2E), `ProfilePage.WaitForPageLoad()`,
  and the `ChangePinPage.xaml.cs` constructor.
- **Broad Profile XPath: deliberately left as-is.** Scoping it to the bottom nav would break the other half of
  the same method: when Profile is behind the "More" overflow it is rendered in a popup menu, not in
  `BottomNavigationView`, so one nav-scoped selector cannot serve both layouts. Not worth destabilising a green
  flow for locator aesthetics; the existing comment already explains the two paths.

### Verification

| Run | Result |
|-----|--------|
| `dotnet test CipherBank-app.Tests --filter "FullyQualifiedName~PinChangeTests"` | **Failed: 0, Passed: 14** (was 6) |
| `dotnet test CipherBank-app.Tests --nologo` (full unit suite) | **Failed: 0, Passed: 262, Total: 262**, coverage gate green |
| `dotnet test CipherBank-app.E2ETests --nologo` (no `E2E_RUN`) | **Failed: 0, Skipped: 14, Total: 14** |
| `dotnet build CipherBank-app -f net10.0-android -c Debug` | Build succeeded, 0 errors |
| `./scripts/e2e-android.sh --story CB-ACCOUNT-PIN-CHANGE` | **Passed! Failed: 0, Passed: 1**, 1 m 32 s |
| `AccountStories` regression (`E2E_RUN=1`) | **Passed! Failed: 0, Passed: 4**, 2 m 32 s |

Journal from the device run (note the new rejection step; `357921` is derived from the active `246810`):

```
2026-07-26T01:15:32Z device: opened Change PIN with active PIN=246810
2026-07-26T01:15:48Z device: rejected change with wrong current PIN=357921
2026-07-26T01:16:13Z device: changed PIN 246810 -> 135791 (previous PIN journaled as alternate)
2026-07-26T01:16:18Z device: confirmed replaced PIN=246810 no longer unlocks
2026-07-26T01:16:23Z device: unlocked with new PIN=135791
```

`docs/tests/gaps/` remains empty. `git check-ignore` confirms
`artifacts/e2e-journal/CB-ACCOUNT-PIN-CHANGE.journal.txt` is ignored by `artifacts/e2e-journal/.gitignore`
and untracked.

### Residual concerns

- The `VaultNotReady` message ("Unlock your wallet before changing your PIN") is accurate but can only be seen
  by a user carrying a pre-device-secret blob; the shipped Profile route already requires an unlock, so it is a
  belt-and-braces path with unit coverage but no device coverage.
- A rejected change consumes one `PinService` failed-attempt slot (`MaxFails = 5`). The story spends one of
  those per run, which is fine, but a future lockout story on the same session should account for it.
