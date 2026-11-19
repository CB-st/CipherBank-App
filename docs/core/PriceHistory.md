# PriceHistory

## Purpose

Immutable record containing a cryptocurrency's price points over a requested period; used in history API responses.

## Source
- `CipherBank-app.Core/Models/PriceHistory.cs`

## Contract

Positional members: `Symbol`, `PricePoints` (`List<PricePoint>`), `StartDate`, and `EndDate`. `HighPrice`, `LowPrice`, and `AveragePrice` summarize points, returning zero for an empty list. `PriceChange` is last minus first and returns zero with fewer than two points. `PercentChange` divides that change by the first price and multiplies by 100; it returns zero with fewer than two points or a zero first price.

## Dependencies/Inputs

Constructed from an asset symbol, point list, and period bounds; computations use the list in its given order.

## Failure behavior

An empty list is handled as described above. The type does not declare validation for null lists.

## Tests/Validation
- [`PriceHistoryTests.cs`](../../CipherBank-app.Tests/Models/PriceHistoryTests.cs) covers the model.
- [`CryptoAPIServiceTests.cs`](../../CipherBank-app.Tests/Services/CryptoAPIServiceTests.cs) covers history service responses.

## Related docs
- [PricePoint](PricePoint.md)
- [CryptoApiService](../app/CryptoApiService.md)
