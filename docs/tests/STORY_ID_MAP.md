# Story ID map — MAUI Appium (design-spec E2E)

**Ownership:** Once Cora Shell reaches **Expo parity**, MAUI **Appium** (`CipherBank-app.E2ETests`) is the authoritative runner for the full design-spec user stories (`CB-*` / `US-*`).

Expo Playwright under `design_handoff_cipherbank/starter/e2e/` remains the **contract lab** until parity — same story IDs, different driver.

```mermaid
flowchart LR
  scaffold["Draw.io scaffold CB-*"]
  expo["Expo Playwright contract lab"]
  maui["MAUI Appium shipping Shell"]
  scaffold --> expo
  scaffold --> maui
  expo -->|"parity cutover"| maui
```

## Executable / partial Appium facts today

| Story IDs | Test method | Notes |
|-----------|-------------|--------|
| US-LCK-01, US-CNV-01, US-RCV-01 | `US_LCK_01_CNV_01_RCV_01_Unlock_ConvertQuote_ReceiveQr` | Needs `E2E_RUN=1` + APK |
| US-HOM-05, CB-MARKET-001, US-SND-01 | `US_HOM_05_SND_01_HomeChart_ConvertPickers_SendAch` | Chart chips + Send ACH surfaces |
| US-POS-01, CB-PAY-003 | `US_POS_01_CB_PAY_003_PosLabSimulate` | Soft-return if PosLab not on screen |
| CB-ACCOUNT-001 / US-ONB-01 | `CB_ACCOUNT_001_US_ONB_01_CreateAccount` | **Executable** — passed on `CipherBank_API34` (Task 7 canary); Welcome→Keys→Quiz→PIN→Home, procedure steps journaled |
| US-ONB-03 | `US_ONB_03_WrongQuizWords_BlocksAdvance` | **Executable** — passed on `CipherBank_API34` (Task 8); wrong backup-quiz words surface `BackupQuizErrorLabel` and block advance to SetPin |
| US-ONB-04 | `US_ONB_04_PinMismatch_BlocksSeal` | **Executable** — passed on `CipherBank_API34` (Task 8); confirm ≠ PIN surfaces `SetPinErrorLabel` and blocks seal; same Fresh-device fixture as CB-ACCOUNT-001 |

## Backlog

`StoryBacklogTests` lists remaining `CB-*` entries as skipped Theories. Catalog: `CipherBank-app.E2ETests/Stories/StoryCatalog.cs`.

## Expo testID ↔ MAUI AutomationId

See `Stories/AutomationIdMap.cs`. New controls should prefer **identical** strings where possible.

| Expo `testID` | MAUI `AutomationId` |
|---------------|---------------------|
| welcome-create | WelcomeCreateWalletButton |
| welcome-returning | WelcomeReturningButton |
| keys-continue | KeysContinueButton |
| quiz-continue | BackupQuizVerifyButton |
| pin-input / pin-confirm / pin-finish | SetPinEntry / SetPinConfirmEntry / SetPinSealButton |

## Run

```bash
# Inventory (no device)
dotnet test CipherBank-app.E2ETests --list-tests

# Device smoke (sealed-wallet unlock path)
E2E_RUN=1 TEST_PLATFORM=android ANDROID_APK_PATH=/path/to/app.apk \
  E2E_TEST_PIN=123456 \
  dotnet test CipherBank-app.E2ETests --filter "FullyQualifiedName~CoraShellSmokeTests"

# Clean-install onboarding (DeviceState.FreshAsync clears app data before each Fact — no separate flag needed)
E2E_RUN=1 TEST_PLATFORM=android ANDROID_APK_PATH=/path/to/app.apk \
  dotnet test CipherBank-app.E2ETests \
  --filter "FullyQualifiedName~CreateAccount|FullyQualifiedName~PinMismatch|FullyQualifiedName~WrongQuizWords"

# Or via the harness (boots CB_AVD, builds/installs the APK, starts Appium):
./scripts/e2e-android.sh --story CB-ACCOUNT-001
./scripts/e2e-android.sh --story US-ONB-03
./scripts/e2e-android.sh --story US-ONB-04
```

Appium server: `http://localhost:4723`.

## Related

- [e2e-tests.md](./e2e-tests.md) — Appium env + AutomationId tables
- `design_handoff_cipherbank/starter/docs/STORY_ID_MAP.md` — Expo mirror
- `design_handoff_cipherbank/starter/docs/USER_STORIES.md` — procedures
- UserStories scaffold `docs/USER_STORIES.md` — Draw.io source of truth
