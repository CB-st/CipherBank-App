# E2E Tests

**Project**: CipherBank-app.E2ETests

End-to-end tests for critical user journeys using Appium. Requires a running Appium server and a device or emulator.

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
| LoginPage (legacy) | UsernameEntry, PasswordEntry, LoginButton, ErrorLabel |
| DashboardPage (legacy) | WelcomeLabel, TotalBalanceLabel, WalletButton, PurchaseButton, RefreshButton, ErrorLabel, RecentTransactionsList |
| WalletPage (legacy) | WalletBalanceLabel, WalletAddressLabel, RecipientAddressEntry, SendAmountEntry, SendButton, TransactionHistoryList, ErrorLabel |
| PurchasePage (legacy) | CryptoSelector, AmountEntry, PurchaseButton, FeeLabel, ErrorLabel |

When adding new UI elements used by E2E flows, add the corresponding `AutomationId` and update PageObjects.
