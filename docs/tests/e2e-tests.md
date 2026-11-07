# E2E Tests

**Project**: CipherBank-app.E2ETests

End-to-end tests for critical user journeys using Appium. Requires a running Appium server and a device or emulator.

**Coding standards** for E2E and Shell work: repo-root [AGENTS.md](../../AGENTS.md) — Function documentation (Use High/Medium/Low + Scope), Object ownership & process boundaries, Complexity limits (max 2 loop layers; prefer ternary / dictionary dispatch).

## Structure

```
CipherBank-app.E2ETests/
├── PageObjects/
│   ├── BasePage.cs
│   ├── UnlockPage.cs
│   ├── HomePage.cs
│   ├── ConvertPage.cs
│   ├── SendPage.cs
│   ├── ReceivePage.cs
│   ├── PosLabPage.cs
│   └── … (legacy Login/Dashboard/Wallet/Purchase)
└── Tests/
    ├── CoraShellSmokeTests.cs
    ├── AccountStories.cs
    └── HarnessFilterContractTests.cs / … (host-only harness Facts)
```

## Dependencies

- **CipherBank-app.Core** – Referenced (models; PageObjects use Selenium/Appium types)
- **Appium.WebDriver** – Appium client
- **Selenium.WebDriver** – WebDriver base
- **FluentAssertions** – Assertions
- **xUnit** – Test framework

## Prerequisites

1. **Appium server** running (default: `http://localhost:4723`)
2. **Android emulator** or **iOS simulator** (or physical device)
3. **Built app** (APK or .app)

## Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| TEST_PLATFORM | `android` or `ios` | android |
| ANDROID_APK_PATH | Path to APK | /path/to/CipherBank.apk |
| ANDROID_DEVICE | Device/emulator name | Android Emulator |
| IOS_APP_PATH | Path to .app | /path/to/CipherBank.app |
| IOS_DEVICE | Simulator/device name | iPhone 15 |
| IOS_VERSION | iOS version | 17.0 |
| E2E_TEST_PIN | Unlock PIN for sealed test wallet | *(required — see Harness credentials)* |
| E2E_TEST_PIN_ALT | Alternate PIN for change-PIN stories | *(required)* |
| E2E_RECOVERY_PASSWORD | Recovery-file password (CB-ACCOUNT-002) | *(required)* |

## Harness credentials (debug only)

PIN, alternate PIN, and recovery-file password used by Appium stories are **synthetic lab values for local/CI emulator diagnosis**. They are not product secrets and must not live as string literals in shipping or harness **source**. Journals that record them flush under gitignored `artifacts/` only.

**Setup (once per machine):**

```bash
cp docs/tests/e2e-local.env.example artifacts/e2e-local.env
# edit artifacts/e2e-local.env — fill the three values
```

Or export `E2E_TEST_PIN`, `E2E_TEST_PIN_ALT`, and `E2E_RECOVERY_PASSWORD` in the shell / CI job.

**Suggested local lab values** (CipherBank_API34 debugging reference — copy into the gitignored file, do not commit):

| Variable | Suggested lab value |
|----------|---------------------|
| `E2E_TEST_PIN` | `246810` |
| `E2E_TEST_PIN_ALT` | `135791` |
| `E2E_RECOVERY_PASSWORD` | `Cb-Emu-Recovery-2026` (12+ chars as the app requires) |

Committed template (placeholders only): [`e2e-local.env.example`](e2e-local.env.example).  
Never commit `artifacts/e2e-local.env`, `artifacts/e2e-journal/`, or recovery pulls.

## Public forks, org secrets, and third-party databases

This repository is public; the org's pipeline credentials are GitHub secrets
and are never exposed to forks. The test suites are presence-gated so a clone
with no configuration **skips** credential-bound work instead of failing:

- Without `E2E_RUN=1`, device Facts skip — plain `dotnet test` on any machine
  stays green with zero org values.
- With `E2E_RUN=1` but missing credentials, the harness fails closed with a
  message naming the variables to set — it never falls back to embedded
  defaults, because there are none in source.
- The org pipeline maps its secrets (`E2E_TEST_PIN`, `E2E_TEST_PIN_ALT`,
  `E2E_RECOVERY_PASSWORD`, optional `ANDROID_CERT_PINS`) into these same
  variables. Pipeline wallet-backup testing targets the **Sandbox**
  environment (`config/network/endpoints.json` default); Production is
  reserved for real profile backups and is never a test target.

To run the full suite against **your own** backend (the app is config-driven —
many databases, many devices):

1. Point `config/network/endpoints.json` at your API hosts.
2. Fill `artifacts/e2e-local.env` (or CI secrets) with your own synthetic
   PIN/recovery values.
3. Optionally set `ANDROID_CERT_PINS` with your own current+backup SPKI pins
   (`docs/config/CERTIFICATE_PINNING_SETUP.md` has the recipe and the
   release-gate contract).
4. Run `scripts/e2e-android.sh` as documented above — nothing in the harness
   assumes the CipherBank org's infrastructure.

## Local Android harness (`scripts/e2e-android.sh`)

Wave 0 one-shot runner for `CipherBank_API34`. Boots the AVD if not already
attached, builds the MAUI app (`-f net10.0-android -c Debug
-p:EmbedAssembliesIntoApk=true`), **uninstalls any leftover package**, installs
the APK (`adb install`, not `-r`), `pm clear`s application data so PIN/LocalDb
do not survive across sessions, starts Appium on `:4723` if it isn't already
up, then runs the requested slice of `CipherBank-app.E2ETests`.

```bash
./scripts/e2e-android.sh --story CB-ACCOUNT-001   # one story
./scripts/e2e-android.sh --wave account           # one wave (account|market|wallets|fund|pay|cards)
./scripts/e2e-android.sh --all                    # full suite
./scripts/e2e-android.sh --help                   # usage
```

`--wave account` runs every Wave 0–1 account/onboarding Fact in `AccountStories.cs`: `CB-ACCOUNT-001`,
`US-ONB-03`, `US-ONB-04`, `CB-ACCOUNT-PIN-CHANGE`, and `CB-ACCOUNT-002`. Selection is based on stable
`Story` traits rather than test method names. See the `WAVE_STORIES` map in `scripts/e2e-android.sh`.

Env/path setup (`ANDROID_HOME`, `ANDROID_SDK_ROOT`, `DOTNET_ROOT`, `CB_MAUI_PACKAGE`,
`CB_AVD`) lives in `scripts/lib/android-env.sh` and is sourced automatically.
MAUI package id for this harness: `com.companyname.cipherbankapp` (not Expo's
`com.cipherbank.app` — see repo-root [AGENTS.md](../../AGENTS.md)).

## CoraShellSmokeTests

| Test | Flow |
|------|------|
| `US_LCK_01_CNV_01_RCV_01_Unlock_ConvertQuote_ReceiveQr` | Unlock → Convert lock → Receive QR |
| `US_HOM_05_SND_01_HomeChart_ConvertPickers_SendAch` | Home chips + hide → Convert pickers → Send ACH fields |
| `US_POS_01_CB_PAY_003_PosLabSimulate` | PosLab start + simulate (fails with gap note if unreachable) |

## AutomationId Requirements

Page objects use `By.Id()` which maps to `AutomationId` in MAUI. All interactive elements used by E2E tests **must** have `AutomationId` set in XAML.

| Page | Required AutomationIds |
|------|-------------------------|
| UnlockPage | UnlockPinEntry, UnlockButton, UnlockErrorLabel |
| HomePage | HomeTotalUsdLabel, HomeHideBalancesButton, HomeRange1dButton, HomeRange1wButton, HomeRange1mButton, HomeRange1yButton, HomeConvertButton, HomeSendButton, HomeReceiveButton, HomePayButton |
| ConvertPage | ConvertFromPicker, ConvertToPicker, ConvertAmountEntry, ConvertLockQuoteButton, ConvertSubmitButton |
| SendPage | SendSavedPayeesPicker, SendRecipientEntry, SendAchPayeeNameEntry, SendAchHolderEntry, SendAchBankEntry, SendAchRoutingEntry, SendAchAccountEntry, SendAchAccountTypePicker, SendAchMemoEntry, SendAchSavePayeeButton, SendAmountEntry, SendSpeedPicker, SendSubmitButton |
| ReceivePage | ReceiveRefreshButton, ReceiveQrImage, ReceiveAddressLabel |
| PosLabPage | PosStartSessionButton, PosSimulateButton |
| ProfilePage | ProfileChangePinButton, ProfileLockButton, ProfileBackupPasswordEntry, ProfileBackupPasswordConfirmEntry, ProfileBackupHintEntry, ProfileExportBackupButton, ProfileRevealPinEntry, ProfileRevealMnemonicButton, ProfileMnemonicRevealLabel |
| RestoreBackupPage | RestoreBackupPickFileButton, RestoreBackupFileStatusLabel, RestoreBackupPasswordEntry, RestoreBackupOpenButton, RestoreBackupErrorLabel |
When adding new UI elements used by E2E flows, add the corresponding `AutomationId` and update PageObjects.
