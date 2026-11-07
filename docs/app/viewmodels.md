# ViewModels

All ViewModels use CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`).

---

## LoginViewModel

**File**: `ViewModels/LoginViewModel.cs`

**Dependencies**: `ILogger`, `IProductClient`, `INavigationService`, `IDialogService`

**Properties**: Username, Password, IsBusy, ErrorMessage

**Commands**:
- `SignInCommand` – Validates input, calls `IProductClient.CreateSessionAsync` (username/password remain UI-required, not sent), navigates via `INavigationService` on success.

**Methods**: `CancelLogin()` – Cancels in-flight login.

---

## DashboardViewModel

**File**: `ViewModels/DashboardViewModel.cs`

**Dependencies**: `ILogger`, `IProductClient`, `IErrorHandler`, `INavigationService`, `IDialogService`

**Properties**: Cryptocurrencies, SelectedCrypto, IsLoading, IsRefreshing, ErrorMessage, TotalPortfolioValue

**Commands**:
- `LoadPricesCommand` – Loads crypto prices via `LoadCryptoPricesAsync(isRefresh: false)`.
- `RefreshPricesCommand` – Pull-to-refresh; calls `LoadCryptoPricesAsync(isRefresh: true)`.
- `NavigateToPurchaseCommand` – Navigates via `INavigationService` to `Routes.PurchaseWithSymbol(SelectedCrypto.Symbol)`.
- `NavigateToWalletsCommand` – Navigates to Wallet page.
- `ViewCryptoDetailsCommand` – Shows alert via `IDialogService`.

**Methods**: `OnDisappearing()` – Cancels operations.

---

## WalletViewModel

**File**: `ViewModels/WalletViewModel.cs`

**Dependencies**: `ILogger`, `IProductClient`, `IErrorHandler`, `INavigationService`, `IDialogService`

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

**Dependencies**: `ILogger`, `IPublicQuoteService`, `IProductClient`, `IErrorHandler`, `INavigationService`, `IDialogService`

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

**Dependencies**: `ILogger`, `ISettingsService`, `IProductSessionStore`, `IAppSession`, `INavigationService`, `IDialogService`, `IHealthCheckClient`

**Properties**: ApiEndpoint, ThemeMode, NotificationsEnabled, BiometricEnabled, AutoLockTimeout, DefaultCurrency, IsTesting, IsSaving, StatusMessage, IsStatusSuccess

**Static options**: ThemeModes, Currencies, AutoLockOptions

**Commands**:
- `SaveSettingsCommand` – Validates endpoint, persists to ISettingsService, applies theme.
- `TestConnectionCommand` – Uses `IHealthCheckClient` (cert pinning) to hit `/health`.
- `ResetToDefaultsCommand` – Calls `ISettingsService.ResetToDefaults()`, loads, saves.
- `LogoutCommand` – Confirms via `IDialogService`, calls LogoutAsync, navigates via `INavigationService`.
- `ShowAboutCommand` – Shows about dialog.

**Methods**: `OnDisappearing()` – Cancels operations.
