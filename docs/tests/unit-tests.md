# Unit Tests

**Project**: CipherBank-app.Tests

Tests Core models and Core service helpers. Does not reference the MAUI app.

Repository-shape rules (central package versions, AssemblyInfo, Core SQL,
retired API names) are not unit tests. They live in `CipherBank-app.Analyzers`
and are covered by `CipherBank-app.Analyzers.Tests`.

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
    ├── CertificatePinningTests.cs
    ├── LogRedactionHelperTests.cs
    ├── RateLimiterTests.cs
    └── RateLimitingHandlerTests.cs
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

| File | Tests |
|------|-------|
| AddressValidatorTests | IsValidAddress (BTC, ETH, SOL), format validation |
| LogRedactionHelperTests | RedactUsername, RedactWalletId, RedactAddress, RedactToken, etc. |
| RateLimiterTests | TryAcquireAsync, GetWaitTimeAsync, sliding window |
| RateLimitingHandlerTests | 429 when rate limited, pass-through when allowed |
| CertificatePinningTests | Certificate validation logic (if applicable) |

## Coverage Configuration

`CipherBank-app.Tests.csproj` does not set `CollectCoverage`, `CoverletOutput`,
`CoverletOutputFormat`, `Threshold`, or `ExcludeByAttribute`. Coverage is
opt-in only on the CI coverage job in
`.github/workflows/quality-gates-and-ai-review.yml`, which passes
`CollectCoverage=true`, `CoverletOutputFormat=cobertura,opencover`,
`CoverletOutput` under `reports/coverage`, and `Threshold=0`. Sonar enforces
new-code coverage.
