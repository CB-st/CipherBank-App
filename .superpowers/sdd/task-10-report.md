# Task 10 report — CB-ACCOUNT-002 recover account, proved on Android emulator

**Status:** DONE
**Base commit:** `10e66e7`
**Branch:** `feat/cora-redesign-maui`

---

## 1. What shipped

The recover-account story runs entirely through product surfaces: the app creates the ciphered recovery
file, Android's own document picker hands it back, and the app decrypts it. Nothing in the test fabricates,
decrypts or rewrites recovery content.

### Shell (`CipherBank-app`) — the one real gap this story hit

`Profile → "Create and save backup file"` did not actually save anything. `BackupFileService` wrote the file
to app **cache**, opened the share sheet, and deleted the file in a `finally`. Two consequences: a user who
dismissed the chooser was told "Backup created" and got no file, and on Android `Share.RequestAsync` returns
as soon as the chooser launches, so the delete could yank the file out from under the app the user picked.
An app-cache file is also erased by `pm clear`, which is exactly what a recovery story must survive.

Rather than add a test-only export path, the export was made to do what its button says:

- `IBackupFileService` — `SaveAndShareAsync` split into `SaveRecoveryFileAsync` (durable save, returns the
  user-facing location) and `ShareRecoveryFileAsync` (opt-in copy to the share sheet).
- `BackupFileService` — Android now publishes the file into the shared **Downloads** collection via
  MediaStore (`OperatingSystem.IsAndroidVersionAtLeast(29)`-guarded; other platforms return null and keep the
  share-only behaviour). The share staging copy is pruned on the *next* export instead of deleted mid-share.
- `ProfileViewModel.ExportBackupAsync` — reports where the file landed ("Saved to Download/<name>.") and
  offers **Share / Done** instead of unconditionally opening a chooser. Sharing an encrypted recovery phrase
  into another app is now something the user asks for.
- `Views/ProfilePage.xaml` — AutomationIds on the reveal controls (`ProfileRevealPinEntry`,
  `ProfileRevealMnemonicButton`, `ProfileMnemonicRevealLabel`), the phrase label `IsVisible`-bound through
  `StringToBoolConverter` (Task 8 pattern) so it cannot be asserted while empty.
- `Views/RestoreBackupPage.xaml` — `RestoreBackupErrorLabel` and the new `RestoreBackupFileStatusLabel` are
  both `IsVisible`-bound, so neither the error nor the file-selected assertion can pass vacuously.

### E2E (`CipherBank-app.E2ETests`)

- `Support/Adb.cs` (new) — single owner of adb process invocation (`Run`/`Shell`/`ShellLines`, 60 s timeout).
  `EmulatorReset` now uses it instead of its own private `RunAdb`.
- `Support/RecoveryFileVault.cs` (new) — owns the export artifact for a run: clears stale exports, waits for
  the app's file to appear in Downloads, pulls a host copy to `artifacts/e2e-recovery/`, hashes it, and — only
  if a device reset ever removed it — pushes the *same bytes* back and re-scans the media volume.
- `Support/DeviceDiagnostics.cs` (new) — dumps Appium page source to `artifacts/e2e-diagnostics/` when a story
  meets an unexpected screen; the failure message carries the path. This is how the one real locator bug below
  was found.
- `Support/StoryJournal.cs` — `RecoveryPassword` (env `E2E_RECOVERY_PASSWORD`, default
  `Cb-Emu-Recovery-2026`, 20 chars ≥ the app's 12-char minimum), journaled with the other run secrets.
- `PageObjects/` — `RestoreBackupPage`, `AndroidDocumentPickerPage` (DocumentsUI), `StepUpPinPromptPage` and
  `NativeAlertPage` (platform dialogs) are new; `ProfilePage` gained `ExportRecoveryFile` / `RevealMnemonic`,
  `WelcomePage.OpenRestoreFromBackup` now returns the page object, and `BasePage` gained `ScrollTo`
  (UiScrollable by resource id), `HideKeyboard` and `WaitForNonEmptyText`.
- `Tests/AccountStories.cs` — `CB_ACCOUNT_002_RecoverAccount`; `SealedHomeOrFail` now takes the story id so
  its gap note names the right story; procedure journaling generalised to any `StoryProcedures` map.

---

## 2. Device verification — `CipherBank_API34` / `emulator-5554`, `com.companyname.cipherbankapp`

```
$ ./scripts/e2e-android.sh --story CB-ACCOUNT-002
Passed!  - Failed:     0, Passed:     1, Skipped:     0, Total:     1, Duration: 2 m 10 s
```

Full Fresh-device regression (the Profile XAML and export-service edits touch Task 7/8/9 stories):

```
$ E2E_RUN=1 ... dotnet test CipherBank-app.E2ETests --filter "FullyQualifiedName~AccountStories"
Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 4 m 41 s
```

(CB-ACCOUNT-001/US-ONB-01, US-ONB-03, US-ONB-04, CB-ACCOUNT-PIN-CHANGE, CB-ACCOUNT-002.)

| Run | Result |
|-----|--------|
| `dotnet test CipherBank-app.Tests --nologo` (full unit suite) | **Passed! Failed: 0, Passed: 262, Total: 262** |
| `dotnet test CipherBank-app.E2ETests --nologo` (no `E2E_RUN`) | **Skipped! Failed: 0, Skipped: 15, Total: 15** |
| `dotnet build CipherBank-app -f net10.0-android -c Debug` | Build succeeded, 0 errors (XAML validated) |
| `dotnet build CipherBank-app.IntegrationTests` | 0 errors |

`docs/tests/gaps/` is empty — no unimplemented Shell UX blocked the story, so no gap note was written.

### Journal from the final run

```
story=CB-ACCOUNT-002
pin=246810
recoveryPassword=Cb-Emu-Recovery-2026
mnemonic=index ignore delay bright reduce lyrics village again grain copy eyebrow eternal
02:01:34Z device: sealed vault with PIN=246810
02:02:18Z device: app exported cipherbank-recovery-20260726-020217.cbr.json (382 bytes,
          sha256=9503ec05b18791102c1a9e9731c6d96eb6108eb7ee4e9a02c25c4dfe038dc357)
02:02:18Z device: cleared app data (Fresh)
02:02:21Z device: wiped the wallet; recovery file restored to picker by harness=False
02:02:23Z step:open - Open account recovery.
02:02:25Z step:enter - Enter account identifier and recovery material.
02:02:31Z device: rejected restore with wrong recovery password=6202-yrevoceR-umE-bC
02:02:37Z step:submit - Submit recovery.
02:02:45Z step:restore - Unlock the backup and enroll the device if required.
02:03:18Z device: revealed phrase on the recovered wallet matches the pre-wipe custody
02:03:18Z step:complete - Enter the recovered account.
```

All five `StoryProcedures.Account002Steps` ids are consumed. **`restored to picker by harness=False`** on
every green run: the app's own MediaStore copy survived `pm clear`, so the adb re-push path never fired and
the picker read the file the app wrote.

---

## 3. Same-custody proof

Landing on Home proves nothing — a fresh wallet also lands on Home. Custody equivalence is proven twice:

**In-app (the assertion the Fact makes).** After the restore seals, the story goes back to
`Profile → Vault → Reveal mnemonic`, answers the step-up PIN challenge, and compares the revealed phrase
with the phrase the Keys screen showed *before* the wipe:

```csharp
MnemonicHelper.Normalize(revealed).Should().Be(MnemonicHelper.Normalize(originalMnemonic), ...);
```

That phrase comes from `ICustodyService.ExportMnemonic()` — the sealed blob on the recovered device — so it
can only match if the device holds the original custody.

**Artifact (independent, offline).** The pulled export decrypts to the same phrase with the journaled
password, using only the format the app wrote (PBKDF2-SHA256, 600 000 iterations, AES-GCM):

```
$ python3 (pbkdf2_hmac + AESGCM over artifacts/e2e-recovery/cipherbank-recovery-20260726-015154.cbr.json)
decrypted mnemonic: orchard magic sadness void soft elder simple girl toward eternal member ivory
$ grep '^mnemonic=' artifacts/e2e-journal/CB-ACCOUNT-002.journal.txt
mnemonic=orchard magic sadness void soft elder simple girl toward eternal member ivory
```

The file header is exactly the app's `cipherbank-recovery-v1` document
(`FORMAT/KDF/ITERATIONS=600000/SALT_B64/NONCE_B64/TAG_B64/CIPHERTEXT_B64/CREATED_AT/HINT`), and no plaintext
phrase appears anywhere in it.

### Why the Fact cannot pass on a no-op

1. `RecoveryFileVault` clears stale exports first and **fails the story** if no new export appears within 45 s.
2. `RestoreBackupFileStatusLabel` must be visible *and* non-empty — a cancelled pick cannot look like success.
3. A wrong recovery password (the journaled one reversed, so still ≥12 chars and genuinely wrong) must produce
   a visible, non-empty `RestoreBackupErrorLabel` and leave the user on the restore page.
4. Only then does the correct password reach SetPin → Home.
5. The revealed phrase must equal the pre-wipe phrase.

---

## 4. Real bug found on device (fixed, not worked around)

The first run failed at the very last step and the second failed at the export step-up. The page-source dump
written by the new `DeviceDiagnostics` explained both:

- MAUI's `DisplayPromptAsync` builds an **AppCompat** dialog, so its panel ids are namespaced to the app
  (`com.companyname.cipherbankapp:id/custom`), not `android:id/custom`, while the buttons keep the framework
  ids (`android:id/button1`/`button2`). The prompt's field is now matched by id suffix, with an unscoped
  fallback that is safe because a platform dialog owns the foreground window — while one is up the page source
  contains the dialog alone.
- Revealing the phrase pushes its label below the fold, and Android drops off-screen views from the
  accessibility tree, so the label has to be scrolled to before it is read.

Neither was a product defect; both were page-object gaps and were fixed there.

---

## 5. Secrets hygiene

`git check-ignore -v` confirms the journal (`artifacts/e2e-journal/`), the pulled recovery files
(`artifacts/e2e-recovery/`) and the page-source dumps (`artifacts/e2e-diagnostics/`) are all ignored; the two
new directories ship only a `.gitignore` placeholder. `git ls-files` tracks no journal, recovery file or
`.cbr.json`. The recovery password is a synthetic dev/test value, env-overridable, and lives only in
`StoryJournal`.

---

## 6. Concerns / follow-ups

- **Downloads is now a real destination for an encrypted recovery file.** It is AES-GCM-sealed under a
  user-chosen 12+ character password and CipherBank never receives it, but it is world-readable to apps with
  storage access, which is a product decision worth an explicit review. A "save to a location you choose"
  (`ACTION_CREATE_DOCUMENT`) variant would be the stricter option.
- `SaveRecoveryFileAsync` returns null on non-Android targets, so those builds still finish through the share
  sheet only. Nothing regressed there, but it is unimplemented rather than done.
- Unlike CB-ACCOUNT-PIN-CHANGE, this story's negative leg spends no `PinService` failed-attempt slot: the
  wrong secret is the recovery password, which never reaches the PIN gate.
- `ProfileRevealPinEntry` exists in the Vault card but `RevealMnemonicAsync` ignores it (the step-up prompt
  collects the PIN instead). Pre-existing dead UI; left alone, now merely reachable by id.
- Stories that follow this one on the same emulator inherit whatever the run left in `/sdcard/Download`;
  `RecoveryFileVault.ClearDeviceExports()` handles CipherBank exports, nothing else.
