# BalanceResponse

## Purpose

Private transport record used by `WalletService` to deserialize the wallet balance endpoint response.

## Source
- `CipherBank-app/Services/WalletService.cs` (nested `BalanceResponse` record)

## Contract

The record contains one decimal positional member, `Balance`. `GetWalletBalanceAsync` reads it from `GET api/v1/wallets/{id}/balance` and returns the decimal value.

## Dependencies/Inputs

JSON response content from the wallet API; consumed only by `WalletService`.

## Failure behavior

A null deserialization result causes `InvalidOperationException`; non-success HTTP responses follow `WalletService` error mapping. It is private implementation detail and is not a public Core model.

## Tests/Validation
- [`WalletServiceTests.cs`](../../CipherBank-app.Tests/Services/WalletServiceTests.cs)

## Related docs
- [WalletService](WalletService.md)
- [Wallet](../core/Wallet.md)
