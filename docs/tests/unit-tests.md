# Unit Tests

**Project**: CipherBank-app.Tests

Tests Core models and service interfaces using mocks. Does not reference the MAUI app.

## Structure

```
CipherBank-app.Tests/
├── Models/
│   ├── CryptoCurrencyTests.cs
│   ├── PriceHistoryTests.cs
│   ├── TransactionTests.cs
│   └── WalletTests.cs
└── Services/
    ├── AddressValidatorTests.cs
    ├── AuthServiceTests.cs
    ├── CertificatePinningTests.cs
    ├── CryptoAPIServiceTests.cs
    ├── LogRedactionHelperTests.cs
    ├── RateLimiterTests.cs
    ├── RateLimitingHandlerTests.cs
    ├── TransactionServiceTests.cs
    └── WalletServiceTests.cs
```

## Dependencies

- **CipherBank-app.Core** – Models, interfaces
- **Moq** – Mocking
- **FluentAssertions** – Assertions
- **xUnit** – Test framework
- **coverlet** – Coverage

## Model Tests

| File | Tests |
|------|-------|
| CryptoCurrencyTests | FormattedPrice, FormattedPercentChange, IsPriceUp |
| PriceHistoryTests | HighPrice, LowPrice, AveragePrice, PriceChange, PercentChange |
| TransactionTests | FormattedAmount, TypeDescription, IsOutgoing, IsComplete, IsPending |
| WalletTests | FormattedBalance, HasBalance |

## Service Tests

Tests mock the interface (e.g. `Mock<IAuthService>`) and verify behavior when the mock is used.

| File | Tests |
|------|-------|
| AuthServiceTests | LoginAsync (valid/invalid), RefreshAsync, IsTokenExpiredAsync |
| CryptoAPIServiceTests | GetCryptoPricesAsync, GetCryptoPriceAsync, GetPriceHistoryAsync, SearchCryptoAsync |
| WalletServiceTests | GetWalletsAsync, GetWalletAsync, CreateWalletAsync |
| TransactionServiceTests | GetTransactionHistoryAsync, PurchaseCryptoAsync, SendCryptoAsync |
| AddressValidatorTests | IsValidAddress (BTC, ETH, SOL), format validation |
| LogRedactionHelperTests | RedactUsername, RedactWalletId, RedactAddress, RedactToken, etc. |
| RateLimiterTests | TryAcquireAsync, GetWaitTimeAsync, sliding window |
| RateLimitingHandlerTests | 429 when rate limited, pass-through when allowed |
| CertificatePinningTests | Certificate validation logic (if applicable) |

## Coverage Configuration

- **CollectCoverage**: true
- **CoverletOutputFormat**: cobertura
- **CoverletOutput**: ./coverage/
- **Threshold**: 70 (line, branch, method)
- **ExcludeByAttribute**: Obsolete, GeneratedCodeAttribute, CompilerGeneratedAttribute
