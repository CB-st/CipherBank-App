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
    ├── CoraShellSmokeTests.cs   # preferred
    └── CriticalUserJourneyTests.cs  # legacy (skipped when Cora Shell is primary)
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
| E2E_TEST_PIN | Unlock PIN for sealed test wallet | 123456 |

## Local Android harness (`scripts/e2e-android.sh`)

Wave 0 one-shot runner for `CipherBank_API34`. Boots the AVD if not already
attached, builds the MAUI app (`-f net10.0-android -c Debug
-p:EmbedAssembliesIntoApk=true`), installs the APK, starts Appium on `:4723`
if it isn't already up, then runs the requested slice of
`CipherBank-app.E2ETests`.

```bash
./scripts/e2e-android.sh --story CB-ACCOUNT-001   # one story
./scripts/e2e-android.sh --wave account           # one wave (account|market|wallets|fund|pay|cards)
./scripts/e2e-android.sh --all                    # full suite
./scripts/e2e-android.sh --help                   # usage
```

Env/path setup (`ANDROID_HOME`, `ANDROID_SDK_ROOT`, `DOTNET_ROOT`, `CB_MAUI_PACKAGE`,
`CB_AVD`) lives in `scripts/lib/android-env.sh` and is sourced automatically.
MAUI package id for this harness: `com.companyname.cipherbankapp` (not Expo's
`com.cipherbank.app` — see repo-root [AGENTS.md](../../AGENTS.md)).

## CoraShellSmokeTests

| Test | Flow |
|------|------|
| Smoke_UnlockHomeConvertReceive_ShouldSucceed | Unlock → Convert lock → Receive QR |
| Smoke_ParitySurfaces_ChartHideConvertAch_ShouldExist | Home chips + hide → Convert pickers → Send ACH fields |
| Smoke_PosLabSimulate_ShouldRun | PosLab start + simulate (soft-skip if off route) |

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
| LoginPage (legacy) | UsernameEntry, PasswordEntry, LoginButton, ErrorLabel |
| DashboardPage (legacy) | WelcomeLabel, TotalBalanceLabel, WalletButton, PurchaseButton, RefreshButton, ErrorLabel, RecentTransactionsList |
| WalletPage (legacy) | WalletBalanceLabel, WalletAddressLabel, RecipientAddressEntry, SendAmountEntry, SendButton, TransactionHistoryList, ErrorLabel |
| PurchasePage (legacy) | CryptoSelector, AmountEntry, PurchaseButton, FeeLabel, ErrorLabel |

When adding new UI elements used by E2E flows, add the corresponding `AutomationId` and update PageObjects.
