# CryptoCurrency

## Purpose

Immutable record for current market data on one asset; used in crypto price and search API responses.

## Source
- `CipherBank-app.Core/Models/CryptoCurrency.cs`

## Contract

Positional members: `Symbol` (`AssetSymbol`), `Name`, `CurrentPrice`, `PriceChange24H`, `PercentChange24H`, `MarketCap`, `Volume24H`, and nullable `IconUrl`. `IsPriceUp` is true when percent change is nonnegative. `FormattedPrice` uses invariant culture with a dollar sign and two decimal places; `FormattedPercentChange` uses invariant culture, two decimal places, and a plus sign for nonnegative values.

## Dependencies/Inputs

Constructed from market data and an optional icon URI; no collaborators.

## Failure behavior

The type declares no explicit validation or failure behavior.

## Tests/Validation
- [`CryptoCurrencyTests.cs`](../../CipherBank-app.Tests/Models/CryptoCurrencyTests.cs) covers the model.
- [`CryptoAPIServiceTests.cs`](../../CipherBank-app.Tests/Services/CryptoAPIServiceTests.cs) covers service responses using this model.

## Related docs
- [CryptoApiService](../app/CryptoApiService.md)
- [Core models guide](models.md)
