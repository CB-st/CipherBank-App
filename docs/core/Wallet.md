# Wallet

## Purpose

Immutable record representing a user's cryptocurrency wallet as returned by wallet API operations.

## Source
- `CipherBank-app.Core/Models/Wallet.cs`

## Contract

Positional members: `Id`, `CryptoSymbol` (`AssetSymbol`), `CryptoName`, `Balance`, `Address`, and `CreatedAt`. `FormattedBalance` formats to eight decimal places followed by the symbol; `HasBalance` is true only when `Balance > 0`.

## Dependencies/Inputs

Constructed from wallet identity, asset, balance, address, and creation time. `CryptoSymbol` uses the Core `AssetSymbol` type.

## Failure behavior

The type declares no explicit validation or failure behavior.

## Tests/Validation
- [`WalletTests.cs`](../../CipherBank-app.Tests/Models/WalletTests.cs)

## Related docs
- [WalletService](../app/WalletService.md)
- [Core models guide](models.md)
