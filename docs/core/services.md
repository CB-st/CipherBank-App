# Core Services

Interfaces and utility classes in `CipherBank-app.Core`.

---

## IProductClient

**File**: `V1/IProductClient.cs`

Product `/v1` contract (session, portfolio, wallets, transfer, convert). Shell wires `HttpProductClient` on the pinned/rate-limited pipeline. Core DI does not register `IProductClient`.

---

## IPublicQuoteService

**File**: `Services/IPublicQuoteService.cs`

Public market quotes (`/currencies`, `/quote`, `/iquote`). Shell registers `PublicApiClient`.

---

## AddressValidate

**File**: `Wallets/AddressValidate.cs`

Production wallet-address checks used by `AddWalletViewModel`. NBitcoin for BTC/LTC/DOGE, regex for ETH, alphabet-only XMR. Do not add a second address library.
