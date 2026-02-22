# CipherBank App - Future Architecture Plan

```mermaid
classDiagram
    %% Application Entry Point
    class App {
        +CreateWindow() Window
    }

    class AppShell {
        +AppShell()
    }

    %% Views
    class LoginPage {
        +LoginPage(LoginViewModel)
    }

    class DashboardPage {
        +DashboardPage(DashboardViewModel)
    }

    class WalletPage {
        +WalletPage(WalletViewModel)
    }

    class PurchasePage {
        +PurchasePage(PurchaseViewModel)
    }

    class SettingsPage {
        +SettingsPage(SettingsViewModel)
    }

    %% ViewModels
    class LoginViewModel {
        +string Username
        +string Password
        +bool IsBusy
        +string ErrorMessage
        +SignInAsync() Task
    }

    class DashboardViewModel {
        +ObservableCollection~CryptoCurrency~ Cryptocurrencies
        +CryptoCurrency SelectedCrypto
        +bool IsLoading
        +LoadPricesAsync() Task
        +RefreshPricesAsync() Task
        +NavigateToPurchaseCommand ICommand
    }

    class WalletViewModel {
        +ObservableCollection~Wallet~ Wallets
        +ObservableCollection~Transaction~ Transactions
        +Wallet SelectedWallet
        +decimal TotalBalance
        +LoadWalletsAsync() Task
        +LoadTransactionsAsync() Task
        +SendCryptoCommand ICommand
    }

    class PurchaseViewModel {
        +ObservableCollection~CryptoCurrency~ AvailableCryptos
        +CryptoCurrency SelectedCrypto
        +decimal Amount
        +decimal TotalCost
        +bool IsPurchasing
        +PurchaseCryptoAsync() Task
        +CalculateTotalCost()
    }

    class SettingsViewModel {
        +string ApiEndpoint
        +bool UseMocks
        +string ThemeMode
        +bool NotificationsEnabled
        +bool BiometricEnabled
        +SaveSettingsCommand ICommand
        +TestConnectionCommand ICommand
    }

    %% Service Interfaces
    class IAuthService {
        <<interface>>
        +LoginAsync(user, password) Task~AuthToken~
        +RefreshAsync(refreshToken) Task~AuthToken~
        +GetStoredTokenAsync() Task~AuthToken~
        +IsTokenExpiredAsync() Task~bool~
        +LogoutAsync() Task
    }

    class ICryptoAPIService {
        <<interface>>
        +GetCryptoPricesAsync() Task~List~CryptoCurrency~~
        +GetCryptoPriceAsync(symbol) Task~CryptoCurrency~
        +GetPriceHistoryAsync(symbol, period) Task~PriceHistory~
        +SearchCryptoAsync(query) Task~List~CryptoCurrency~~
    }

    class IWalletService {
        <<interface>>
        +GetWalletsAsync() Task~List~Wallet~~
        +GetWalletAsync(id) Task~Wallet~
        +GetWalletBalanceAsync(id) Task~decimal~
        +CreateWalletAsync(cryptoSymbol) Task~Wallet~
    }

    class ITransactionService {
        <<interface>>
        +GetTransactionHistoryAsync(walletId) Task~List~Transaction~~
        +PurchaseCryptoAsync(symbol, amount) Task~Transaction~
        +SendCryptoAsync(fromWallet, toAddress, amount) Task~Transaction~
        +GetTransactionStatusAsync(id) Task~TransactionStatus~
    }

    class ISettingsService {
        <<interface>>
        +UseMocks bool
        +CipherBankEndpointBase string
        +ThemeMode string
        +NotificationsEnabled bool
        +BiometricAuthEnabled bool
    }

    %% Service Implementations
    class AuthService {
        -ILogger logger
        -HttpClient http
        +LoginAsync(user, password) Task~AuthToken~
        +RefreshAsync(refreshToken) Task~AuthToken~
        +GetStoredTokenAsync() Task~AuthToken~
        +IsTokenExpiredAsync() Task~bool~
        +LogoutAsync() Task
    }

    class CryptoAPIService {
        -ILogger logger
        -HttpClient http
        -IAuthService auth
        +GetCryptoPricesAsync() Task~List~CryptoCurrency~~
        +GetCryptoPriceAsync(symbol) Task~CryptoCurrency~
        +GetPriceHistoryAsync(symbol, period) Task~PriceHistory~
        +SearchCryptoAsync(query) Task~List~CryptoCurrency~~
    }

    class WalletService {
        -ILogger logger
        -HttpClient http
        -IAuthService auth
        +GetWalletsAsync() Task~List~Wallet~~
        +GetWalletAsync(id) Task~Wallet~
        +GetWalletBalanceAsync(id) Task~decimal~
        +CreateWalletAsync(cryptoSymbol) Task~Wallet~
    }

    class TransactionService {
        -ILogger logger
        -HttpClient http
        -IAuthService auth
        +GetTransactionHistoryAsync(walletId) Task~List~Transaction~~
        +PurchaseCryptoAsync(symbol, amount) Task~Transaction~
        +SendCryptoAsync(fromWallet, toAddress, amount) Task~Transaction~
        +GetTransactionStatusAsync(id) Task~TransactionStatus~
    }

    class SettingsService {
        +UseMocks bool
        +CipherBankEndpointBase string
        +ThemeMode string
        +NotificationsEnabled bool
        +BiometricAuthEnabled bool
    }

    %% Models
    class AuthToken {
        <<record>>
        +string AccessToken
        +string RefreshToken
        +DateTimeOffset ExpiresUtc
    }

    class CryptoCurrency {
        <<record>>
        +string Symbol
        +string Name
        +decimal CurrentPrice
        +decimal PriceChange24h
        +decimal PercentChange24h
        +decimal MarketCap
        +decimal Volume24h
        +string IconUrl
    }

    class Wallet {
        <<record>>
        +string Id
        +string CryptoSymbol
        +string CryptoName
        +decimal Balance
        +string Address
        +DateTimeOffset CreatedAt
    }

    class Transaction {
        <<record>>
        +string Id
        +TransactionType Type
        +decimal Amount
        +string CryptoSymbol
        +string FromAddress
        +string ToAddress
        +DateTimeOffset Timestamp
        +TransactionStatus Status
        +decimal FeeAmount
    }

    class PriceHistory {
        <<record>>
        +string Symbol
        +List~PricePoint~ PricePoints
        +DateTimeOffset StartDate
        +DateTimeOffset EndDate
    }

    class TransactionType {
        <<enumeration>>
        Purchase
        Send
        Receive
        Exchange
    }

    class TransactionStatus {
        <<enumeration>>
        Pending
        Confirmed
        Failed
        Cancelled
    }

    %% Relationships - Navigation
    App --> AppShell : creates
    AppShell --> LoginPage : routes to
    AppShell --> DashboardPage : routes to
    AppShell --> WalletPage : routes to
    AppShell --> PurchasePage : routes to
    AppShell --> SettingsPage : routes to

    %% Relationships - Views to ViewModels
    LoginPage --> LoginViewModel : binds to
    DashboardPage --> DashboardViewModel : binds to
    WalletPage --> WalletViewModel : binds to
    PurchasePage --> PurchaseViewModel : binds to
    SettingsPage --> SettingsViewModel : binds to

    %% Relationships - ViewModels to Services
    LoginViewModel --> IAuthService : uses
    DashboardViewModel --> ICryptoAPIService : uses
    WalletViewModel --> IWalletService : uses
    WalletViewModel --> ITransactionService : uses
    PurchaseViewModel --> ICryptoAPIService : uses
    PurchaseViewModel --> ITransactionService : uses
    SettingsViewModel --> ISettingsService : uses

    %% Relationships - Service Implementations
    IAuthService <|.. AuthService : implements
    ICryptoAPIService <|.. CryptoAPIService : implements
    IWalletService <|.. WalletService : implements
    ITransactionService <|.. TransactionService : implements
    ISettingsService <|.. SettingsService : implements

    %% Relationships - Service Dependencies
    AuthService --> AuthToken : returns
    CryptoAPIService --> CryptoCurrency : returns
    CryptoAPIService --> PriceHistory : returns
    CryptoAPIService --> IAuthService : uses
    WalletService --> Wallet : returns
    WalletService --> IAuthService : uses
    TransactionService --> Transaction : returns
    TransactionService --> IAuthService : uses

    %% Relationships - Models
    Transaction --> TransactionType : uses
    Transaction --> TransactionStatus : uses
```

## Future Architecture Overview

### New Pages & Features

1. **Dashboard Page**
   - Real-time cryptocurrency price tracking
   - Market overview with price charts
   - Portfolio value summary
   - Quick navigation to purchase

2. **Wallet Page**
   - View all cryptocurrency wallets
   - Display balances for each wallet
   - Transaction history
   - Send/receive functionality

3. **Purchase Page**
   - Browse available cryptocurrencies
   - Select crypto and amount to purchase
   - Price calculation
   - Purchase confirmation via CipherBank API

4. **Settings Page**
   - API endpoint configuration
   - Mock mode toggle
   - Theme selection (Light/Dark)
   - Notification preferences
   - Biometric authentication settings

### New Services

1. **ICryptoAPIService / CryptoAPIService**
   - Fetch cryptocurrency prices and market data
   - Get price history for charts
   - Search cryptocurrencies

2. **IWalletService / WalletService**
   - Manage user wallets
   - Get wallet balances
   - Create new wallets

3. **ITransactionService / TransactionService**
   - Purchase cryptocurrency
   - Send/receive crypto
   - Transaction history
   - Transaction status tracking

4. **Enhanced ISettingsService**
   - Add theme mode
   - Notification preferences
   - Biometric authentication

### New Models

1. **CryptoCurrency**: Price and market data for cryptocurrencies
2. **Wallet**: User's cryptocurrency wallet information
3. **Transaction**: Transaction records with type and status
4. **PriceHistory**: Historical price data for charts
5. **TransactionType**: Enum for transaction types
6. **TransactionStatus**: Enum for transaction status

## Implementation Priority

1. **Phase 1**: Settings Page (enhance existing service)
2. **Phase 2**: Dashboard Page with CryptoAPIService
3. **Phase 3**: Wallet Page with WalletService
4. **Phase 4**: Purchase functionality with TransactionService
