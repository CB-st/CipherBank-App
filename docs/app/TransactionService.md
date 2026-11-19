# TransactionService

## Purpose

Implements `ITransactionService` for transaction history, purchase, send, and status HTTP operations.

## Source
- `CipherBank-app/Services/TransactionService.cs`
- Contract: `CipherBank-app.Core/Services/ITransactionService.cs`

## Contract

- `GetTransactionHistoryAsync(walletId, cancellationToken)` reads `GET api/v1/transactions?walletId={escaped id}`; a null list becomes empty.
- `PurchaseCryptoAsync(symbol, amount, cancellationToken)` posts `{ Symbol, Amount }` to `api/v1/transactions/purchase` and returns a `Transaction`.
- `SendCryptoAsync(fromWalletId, toAddress, amount, cancellationToken)` posts `{ FromWalletId, ToAddress, Amount }` to `api/v1/transactions/send` and returns a `Transaction`.
- `GetTransactionStatusAsync(transactionId, cancellationToken)` reads `GET api/v1/transactions/{escaped id}/status`; deserializes private `StatusResponse(Status)` and returns the status.

Interface convenience overloads supply `CancellationToken.None` when omitted.

### Purchase and send

Purchase requires a non-null symbol and positive amount. Send additionally requires nonblank wallet ID and destination and checks destination length is 20–100 characters before posting. The checks are local guards; send source comments characterize address validation as basic, with additional validation server-side.

## Dependencies/Inputs

Injected `ILogger<TransactionService>` and `HttpClient`; identifiers, asset symbol, amount, destination, and cancellation token. Private nested request records are `PurchaseRequest(Symbol, Amount)` and `SendRequest(FromWalletId, ToAddress, Amount)`; status uses `StatusResponse(Status)`.

## Failure behavior

Blank identifiers/destination and nonpositive amounts fail argument validation; null purchase symbol fails `ArgumentNullException`; short/long destination fails `ArgumentException`. History/status 404s map to `KeyNotFoundException`. Purchase 400 maps to `ArgumentException`, 402 to `InvalidOperationException`. Send 400 maps to `ArgumentException`, 404 to `KeyNotFoundException`, and 422 to `InvalidOperationException` for insufficient balance. Other `HttpRequestException`s are wrapped in `InvalidOperationException`. Null purchase/send/status bodies also cause `InvalidOperationException`. Cancellation and JSON parse failures propagate.

## Tests/Validation
- [`TransactionServiceTests.cs`](../../CipherBank-app.Tests/Services/TransactionServiceTests.cs) mocks `ITransactionService` (with interface convenience overloads); it does not instantiate or exercise the concrete HTTP implementation.

## Related docs
- [Transaction](../core/Transaction.md)
- [App services guide](services.md)
