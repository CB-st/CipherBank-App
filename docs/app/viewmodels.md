# ViewModels

All ViewModels use CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`).

---

## MainPageViewModel

**File**: `ViewModels/MainPageViewModel.cs`

Placeholder ViewModel for MainPage. Empty implementation.

---

## LoginViewModel

**File**: `ViewModels/LoginViewModel.cs`

**Dependencies**: `ILogger<LoginViewModel>`, `IAuthService`

**Properties**: Username, Password, IsBusy, ErrorMessage

**Commands**:
- `SignInCommand` – Validates input, calls `LoginAsync`, navigates to `//DashboardPage` on success. Handles HttpRequestException, Canceled, InvalidOperationException.

**Methods**: `CancelLogin()` – Cancels in-flight login.

---

## DashboardViewModel

**File**: `ViewModels/DashboardViewModel.cs`

**Dependencies**: `ILogger<DashboardViewModel>`, `ICryptoApiService`

**Properties**: Cryptocurrencies, SelectedCrypto, IsLoading, IsRefreshing, ErrorMessage, TotalPortfolioValue

**Commands**:
- `LoadPricesCommand` – Loads crypto prices, populates Cryptocurrencies.
- `RefreshPricesCommand` – Pull-to-refresh; same logic.
- `NavigateToPurchaseCommand` – Navigates to `//PurchasePage?symbol={SelectedCrypto.Symbol}`.
- `ViewCryptoDetailsCommand` – Shows alert with selected crypto details.

**Methods**: `OnDisappearing()` – Cancels operations.

---

## WalletViewModel

**File**: `ViewModels/WalletViewModel.cs`

**Dependencies**: `ILogger`, `IWalletService`, `ITransactionService`, `ICryptoApiService`

**Properties**: Wallets, Transactions, SelectedWallet, TotalBalance, TotalBalanceUsd, IsLoading, IsLoadingTransactions, ErrorMessage, SendToAddress, SendAmount, IsSending

**Reactions**: `OnSelectedWalletChanged` – Loads transactions when wallet changes.

**Commands**:
- `LoadWalletsCommand` – Loads wallets, computes TotalBalanceUsd from prices.
- `LoadTransactionsCommand` – Loads transactions for SelectedWallet.
- `SendCryptoCommand` – Validates, confirms, sends via `SendCryptoAsync`, refreshes.
- `CreateWalletCommand` – Creates wallet for symbol; parameter from command.

**Methods**: `OnDisappearing()` – Cancels operations.

---

## PurchaseViewModel

**File**: `ViewModels/PurchaseViewModel.cs`

**Implements**: `IQueryAttributable` (for `?symbol=` query param)

**Dependencies**: `ILogger`, `ICryptoApiService`, `ITransactionService`

**Properties**: AvailableCryptos, SelectedCrypto, Amount, TotalCost, Fee, IsPurchasing, IsLoading, ErrorMessage, AmountText

**Constants**: `FeePercentage = 0.015` (1.5%)

**Reactions**:
- `OnSelectedCryptoChanged` – Recalculates total.
- `OnAmountTextChanged` – Parses amount, recalculates.

**Commands**:
- `LoadAvailableCryptosCommand` – Loads cryptos for purchase.
- `CalculateTotalCostCommand` – Computes Fee and TotalCost.
- `PurchaseCryptoCommand` – Confirms, purchases, shows success, optionally navigates to WalletPage.
- `SetPresetAmountCommand` – Sets amount from USD value (e.g. $25, $50).

**Methods**:
- `ApplyQueryAttributes` – Handles `symbol` query; pre-selects crypto.
- `OnDisappearing()` – Cancels operations.

---

## SettingsViewModel

**File**: `ViewModels/SettingsViewModel.cs`

**Dependencies**: `ILogger`, `ISettingsService`, `IAuthService`

**Properties**: ApiEndpoint, UseMocks, ThemeMode, NotificationsEnabled, BiometricEnabled, AutoLockTimeout, DefaultCurrency, IsTesting, IsSaving, StatusMessage, IsStatusSuccess

**Static options**: ThemeModes, Currencies, AutoLockOptions

**Commands**:
- `SaveSettingsCommand` – Validates endpoint, persists to ISettingsService, applies theme.
- `TestConnectionCommand` – GET `{ApiEndpoint}/health` to test connectivity.
- `ResetToDefaultsCommand` – Resets to defaults, saves.
- `LogoutCommand` – Confirms, calls LogoutAsync, navigates to LoginPage.
- `ShowAboutCommand` – Shows about dialog.

**Methods**: `OnDisappearing()` – Cancels operations.
