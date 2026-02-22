# E2E Tests

**Project**: CipherBank-app.E2ETests

End-to-end tests for critical user journeys using Appium. Requires a running Appium server and a device or emulator.

## Structure

```
CipherBank-app.E2ETests/
├── PageObjects/
│   ├── BasePage.cs         # Common wait/click/enter helpers
│   ├── LoginPage.cs
│   ├── DashboardPage.cs
│   ├── WalletPage.cs
│   └── PurchasePage.cs
└── Tests/
    └── CriticalUserJourneyTests.cs
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

## Page Object Model

### BasePage

- `WaitForElement(By)` – Wait for visible element
- `ClickElement(By)` – Click element
- `EnterText(By, string)` – Clear and type
- `GetElementText(By)` – Get text
- `IsElementDisplayed(By)` – Visibility check
- `WaitForPageLoad()` – Override in subclasses

### LoginPage

- `EnterUsername`, `EnterPassword` – Input
- `ClickLogin` – Click Sign In
- `LoginAs(username, password)` – Full login → DashboardPage
- `IsErrorDisplayed`, `GetErrorMessage` – Error state
- `LoginWithBiometric` – Biometric login (if available)

**Element IDs**: UsernameEntry, PasswordEntry, LoginButton, ErrorLabel, BiometricLoginButton

### DashboardPage

- `GetWelcomeMessage`, `GetTotalBalance` – Displayed data
- `GoToWallet` → WalletPage
- `GoToPurchase` → PurchasePage
- `GoToSettings` – Navigate to settings
- `Logout` → LoginPage
- `Refresh` – Refresh data
- `IsLoggedIn`, `HasRecentTransactions` – State checks

**Element IDs**: WelcomeLabel, TotalBalanceLabel, WalletButton, PurchaseButton, SettingsButton, LogoutButton, RefreshButton, RecentTransactionsList

### WalletPage

- `SendCrypto(address, amount)` – Send flow
- `HasTransactionHistory` – Transaction list visible

### PurchasePage

- `CompletePurchase(symbol, usdAmount)` – Purchase flow
- `IsPurchaseSuccessful`, `GetSuccessMessage` – Result
- `GoBack` – Navigate back

## CriticalUserJourneyTests

| Test | Flow |
|------|------|
| LoginFlow_WithValidCredentials_ShouldShowDashboard | Login → verify dashboard |
| LoginFlow_WithInvalidCredentials_ShouldShowError | Invalid login → error |
| PurchaseFlow_CompletePurchase_ShouldSucceed | Login → Purchase → complete BTC purchase |
| SendFlow_CompleteSend_ShouldSucceed | Login → Wallet → send crypto |
| LogoutFlow_ShouldReturnToLogin | Login → Logout → login page |
| CriticalPath_LoginPurchaseLogout_ShouldComplete | Login → Purchase ETH → Wallet → Logout |

## AutomationId Requirements

Page objects use `By.Id()` which maps to `AutomationId` in MAUI. All interactive elements used by E2E tests **must** have `AutomationId` set in XAML.

| Page | Required AutomationIds |
|------|-------------------------|
| LoginPage | UsernameEntry, PasswordEntry, LoginButton, ErrorLabel |
| DashboardPage | WelcomeLabel, TotalBalanceLabel, WalletButton, PurchaseButton, RefreshButton, ErrorLabel, RecentTransactionsList |
| WalletPage | WalletBalanceLabel, WalletAddressLabel, RecipientAddressEntry, SendAmountEntry, SendButton, TransactionHistoryList, ErrorLabel |
| PurchasePage | CryptoSelector, AmountEntry, PurchaseButton, FeeLabel, ErrorLabel |

When adding new UI elements used by E2E flows, add the corresponding `AutomationId` and update PageObjects.
