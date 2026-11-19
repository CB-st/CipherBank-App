# CryptoApiService

## Purpose

Implements `ICryptoApiService` to retrieve cryptocurrency prices, price history, and search results from the API.

## Source
- `CipherBank-app/Services/CryptoAPIService.cs`
- Contract: `CipherBank-app.Core/Services/ICryptoAPIService.cs`

## Contract

- `GetCryptoPricesAsync(cancellationToken)` reads `GET api/v1/crypto/prices`; null list becomes empty.
- `GetCryptoPriceAsync(symbol, cancellationToken)` reads `GET api/v1/crypto/price/{escaped symbol}`.
- `GetPriceHistoryAsync(symbol, period, cancellationToken)` reads `GET api/v1/crypto/history/{escaped symbol}?period={escaped period}`.
- `SearchCryptoAsync(query, cancellationToken)` reads `GET api/v1/crypto/search?q={escaped query}`; null list becomes empty.

Interface convenience overloads supply `CancellationToken.None` when omitted.

## Dependencies/Inputs

Injected `ILogger<CryptoApiService>` and `HttpClient`; `AssetSymbol`, period, query, and optional cancellation token. Responses deserialize into Core `CryptoCurrency` and `PriceHistory` models.

## Failure behavior

Null symbols and blank period/query fail argument validation. A null single-price/history response or a 404 maps to `KeyNotFoundException`; other HTTP request failures are wrapped in `InvalidOperationException`. Null collection responses return empty lists. Cancellation and JSON parse failures propagate.

## Tests/Validation
- [`CryptoAPIServiceTests.cs`](../../CipherBank-app.Tests/Services/CryptoAPIServiceTests.cs)

## Related docs
- [CryptoCurrency](../core/CryptoCurrency.md)
- [PriceHistory](../core/PriceHistory.md)
- [PricePoint](../core/PricePoint.md)
- [App services guide](services.md)
