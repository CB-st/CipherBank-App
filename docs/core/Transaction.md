# Transaction

## Purpose

Immutable record representing a cryptocurrency transaction returned by transaction API operations.

## Source
- `CipherBank-app.Core/Models/Transaction.cs`

## Contract

Positional members: `Id`, `Type`, `Amount`, `CryptoSymbol`, nullable `FromAddress` and `ToAddress`, `Timestamp`, `Status`, and `FeeAmount`. `Type` and `Status` use `JsonStringEnumConverter`. Computed members are `FormattedAmount`, `FormattedFee` (eight decimal places and symbol), `TypeDescription`, `IsOutgoing` (send or purchase), `IsComplete` (confirmed), and `IsPending` (pending).

## Dependencies/Inputs

Constructed from transaction data, `AssetSymbol`, `TransactionType`, and `TransactionStatus`; no collaborators.

## Failure behavior

The type declares no explicit validation or failure behavior.

## Tests/Validation
- [`TransactionTests.cs`](../../CipherBank-app.Tests/Models/TransactionTests.cs)

## Related docs
- [TransactionService](../app/TransactionService.md)
- [Core models guide](models.md)
