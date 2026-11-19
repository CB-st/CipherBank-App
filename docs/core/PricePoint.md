# PricePoint

## Purpose

Immutable value record for one observation in a price history series.

## Source
- `CipherBank-app.Core/Models/PricePoint.cs`

## Contract

Positional members are `Timestamp` (`DateTimeOffset`), `Price` (`decimal`), and optional nullable `Volume` (`decimal?`, default `null`).

## Dependencies/Inputs

Constructed from the timestamp, price, and optional volume; no collaborators.

## Failure behavior

The type declares no validation or failure behavior.

## Tests/Validation
- Gap: no dedicated `PricePoint` model test file was found under `CipherBank-app.Tests/Models`.
- [`CryptoAPIServiceTests.cs`](../../CipherBank-app.Tests/Services/CryptoAPIServiceTests.cs) covers deserialized price-history responses.

## Related docs
- [PriceHistory](PriceHistory.md)
- [CryptoApiService](../app/CryptoApiService.md)
