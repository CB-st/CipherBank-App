# Views

All pages are ContentPage with `x:DataType` for compile-time binding.

---

## App.xaml

**File**: `App.xaml`

Merged dictionaries: `Colors.xaml`, `Styles.xaml`. Registers converters:

| Key | Converter |
|-----|-----------|
| InvertedBoolConverter | Inverts bool |
| StringToBoolConverter | Non-empty string → true |
| PriceChangeColorConverter | BoolToColorConverter (Green/Red) |
| StatusColorConverter | BoolToColorConverter (#D4EDDA / #F8D7DA) |

---

## AppShell

**File**: `AppShell.xaml`

Shell with `FlyoutBehavior="Flyout"`. Routes:

| Route | ContentTemplate | Notes |
|-------|----------------|
| LoginPage | LoginPage | FlyoutItemIsVisible=False, NavBarHidden |
| MainTabs (TabBar) | Dashboard, Wallets, Buy, Settings | Tab bar |
| MainPage | MainPage | FlyoutItemIsVisible=False |

---

## MainPage

**File**: `MainPage.xaml`, `MainPage.xaml.cs`

Legacy home page. Minimal content.

---

## LoginPage

**File**: `Views/LoginPage.xaml`

**Bindings**: Username, Password, IsBusy, ErrorMessage

**Commands**: SignInCommand

**UI**: Title "CipherBank", "Welcome Back", Username Entry, Password Entry, error Label, Sign In Button, ActivityIndicator. Uses InvertedBoolConverter for IsEnabled when busy.

---

## DashboardPage

**File**: `Views/DashboardPage.xaml`

**Bindings**: IsRefreshing, RefreshPricesCommand, IsLoading, ErrorMessage, Cryptocurrencies, SelectedCrypto, ViewCryptoDetailsCommand, NavigateToPurchaseCommand

**UI**: RefreshView, "Market Overview" header, ActivityIndicator, error Border, CollectionView of CryptoCurrency (symbol, name, price, percent change). EmptyView with Refresh button. Quick actions: "Buy Crypto", "My Wallets". Uses PriceChangeColorConverter for percent change color.

**Code-behind**: `OnWalletsClicked` navigates to `//WalletPage`.

---

## WalletPage

**File**: `Views/WalletPage.xaml`

**Bindings**: TotalBalanceUsd, IsLoading, ErrorMessage, Wallets, SelectedWallet, SendToAddress, SendAmount, IsSending, SendCryptoCommand, Transactions, IsLoadingTransactions

**UI**: Portfolio summary card (TotalBalanceUsd), CollectionView of wallets (horizontal), Send Crypto section (address, amount, Send button), Recent Transactions CollectionView. EmptyView for wallet/transactions.

---

## PurchasePage

**File**: `Views/PurchasePage.xaml`

**Bindings**: IsLoading, ErrorMessage, AvailableCryptos, SelectedCrypto, AmountText, Amount, Fee, TotalCost, SetPresetAmountCommand, PurchaseCryptoCommand, IsPurchasing

**UI**: Picker for crypto, selected crypto info, amount Entry, quick amount buttons ($25, $50, $100, $500), Order Summary (amount, price, fee, total), Purchase button. Disclaimer.

---

## SettingsPage

**File**: `Views/SettingsPage.xaml`

**Bindings**: ApiEndpoint, UseMocks, ThemeMode, DefaultCurrency, BiometricEnabled, AutoLockTimeout, NotificationsEnabled, StatusMessage, IsStatusSuccess, SaveSettingsCommand, TestConnectionCommand, ResetToDefaultsCommand, LogoutCommand, ShowAboutCommand

**UI**: API Configuration (endpoint, Use Mock Data switch, Test Connection), Appearance (Theme, Currency), Security (Biometric, Auto-Lock), Notifications, Status message, Save/Reset buttons, Account (Log Out, About CipherBank).

**Note**: AutoLockOptions Picker uses hardcoded Items; SettingsViewModel has `AutoLockOptions` array that may not match.
