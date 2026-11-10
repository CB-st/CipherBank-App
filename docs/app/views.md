# Views

XAML pages are ContentPage with `x:DataType` for compile-time binding. `StartupPage` (`Views/StartupPage.cs`) is a code-only ContentPage and has no `x:DataType`.

---

## App.xaml

**File**: `App.xaml`

Merged dictionaries: `Colors.xaml`, `Styles.xaml`. Registers converters:

| Key | Converter |
|-----|-----------|
| InvertedBoolConverter | Inverts bool |
| StringToBoolConverter | Non-empty string → true |
| ObjectToBoolConverter | Non-null object → true |
| PriceChangeColorConverter | BoolToColorConverter (Green/Red) |
| StatusColorConverter | BoolToColorConverter (#D9F5E8 / #FBDDE1) |
| CoinColorConverter | Symbol → brand color (CoinBtc, CoinEth, CoinSol, CoinDefault) |
| CoinGlyphConverter | Symbol → display glyph |

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

**Bindings**: TotalBalanceUsd, IsRefreshing, RefreshWalletsCommand, IsLoading, ErrorMessage, WalletCards, FocusedWalletCard, SelectedWallet, SendToAddress, SendAmount, IsSending, SendCryptoCommand, Transactions, IsLoadingTransactions

**UI**: Portfolio summary card (TotalBalanceUsd), RefreshView (`IsRefreshing`, `RefreshWalletsCommand`), ArcCardDeck bound to `WalletCards` and `FocusedWalletCard`, Send Crypto section (address, amount, Send button), Recent Transactions CollectionView. Empty label when there are no wallet cards; EmptyView for transactions.

---

## PurchasePage

**File**: `Views/PurchasePage.xaml`

**Bindings**: IsLoading, ErrorMessage, AvailableCryptos, FocusedCrypto, SelectedCrypto, PaymentNote, AmountText, Amount, Fee, TotalCost, SetPresetAmountCommand, PurchaseCryptoCommand, IsPurchasing

**UI**: ArcCardDeck bound to `AvailableCryptos` and `FocusedCrypto`, PaymentNote entry, View All opens `AssetPickerPage`, amount Entry, quick amount buttons ($25, $50, $100, $500), Order Summary (amount, price, fee, total), Confirm Payment button. Disclaimer.

---

## AssetPickerPage

**File**: `Views/AssetPickerPage.xaml`

Modal pushed from PurchasePage (`Navigation.PushModalAsync`). iOS presentation is `FormSheet`. Bound to `PurchaseViewModel`.

**Bindings**: AvailableCryptos

**UI**: CollectionView of assets (glyph, name, symbol, price, percent change). Close button.

---

## SettingsPage

**File**: `Views/SettingsPage.xaml`

**Bindings**: ApiEndpoint, UseMocks, ThemeMode, DefaultCurrency, BiometricEnabled, AutoLockTimeout, NotificationsEnabled, StatusMessage, IsStatusSuccess, SaveSettingsCommand, TestConnectionCommand, ResetToDefaultsCommand, LogoutCommand, ShowAboutCommand

**UI**: API Configuration (endpoint, Use Mock Data switch, Test Connection), Appearance (Theme, Currency), Security (Biometric, Auto-Lock), Notifications, Status message, Save/Reset buttons, Account (Log Out, About CipherBank).

**Note**: The Auto-Lock Picker binds `ItemsSource` to `AutoLockOptions` and `SelectedItem` to `AutoLockTimeout`.
