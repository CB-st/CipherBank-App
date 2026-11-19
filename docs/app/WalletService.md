# WalletService

## Purpose

Implements `IWalletService` with HTTP reads and creation against the wallet API.

## Source
- `CipherBank-app/Services/WalletService.cs`
- Contract: `CipherBank-app.Core/Services/IWalletService.cs`

## Contract

- `GetWalletsAsync(cancellationToken)` reads `GET api/v1/wallets` and returns the deserialized list; a null body becomes an empty list.
- `GetWalletAsync(id, cancellationToken)` reads `GET api/v1/wallets/{escaped id}` and returns the wallet.
- `GetWalletBalanceAsync(id, cancellationToken)` reads `GET api/v1/wallets/{escaped id}/balance`; the response is the private `BalanceResponse(decimal Balance)` record and the method returns its balance.
- `CreateWalletAsync(cryptoSymbol, cancellationToken)` posts `{ CryptoSymbol }` to `api/v1/wallets` and returns the deserialized wallet.

The interface also supplies overloads that call the cancellation-token forms with `CancellationToken.None`.

### Wallet reads

Wallet identifiers are required and escaped for path construction. A 404 maps to `KeyNotFoundException`. A null wallet body maps to `KeyNotFoundException`; a null balance body maps to `InvalidOperationException`. Successful JSON is deserialized into the Core `Wallet` model.

## Dependencies/Inputs

Injected `ILogger<WalletService>` and `HttpClient`; wallet ID, `AssetSymbol`, and optional cancellation token. `CreateWalletRequest(string CryptoSymbol)` and `BalanceResponse(decimal Balance)` are private nested transport records, not Core DTOs.

## Failure behavior

Blank IDs and null symbols fail argument validation. `HttpRequestException` for `GetWalletsAsync`, non-404 `GetWalletAsync`/`GetWalletBalanceAsync` reads, and other HTTP operations is wrapped in `InvalidOperationException`; only single-wallet and balance reads map 404 to `KeyNotFoundException`. Create conflict is reported as `InvalidOperationException` (“already exists”). JSON parse and cancellation errors are not caught by the service.

## Tests/Validation
- [`WalletServiceTests.cs`](../../CipherBank-app.Tests/Services/WalletServiceTests.cs) mocks `IWalletService` (with interface convenience overloads); it does not instantiate or exercise the concrete HTTP implementation.

## Related docs
- [Wallet](../core/Wallet.md)
- [App services guide](services.md)
