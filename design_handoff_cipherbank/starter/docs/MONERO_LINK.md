# Monero link map — app `/v1` ↔ CipherBank-src

Mock-first product API for hybrid XMR light wallets and market data, aligned to the existing C++ MoneroRPC adapter and PriceCache. **No spend keys or seeds leave the device for unmanaged wallets.**

## Architecture today (CipherBank-src)

```
Mobile → HTTPS api.cipherbank.money
           ├─ POST /quote, /iquote  → PriceCache (Coinbase/Kraken; XMR included)
           └─ POST /currencies     → Wallet_Handler → MoneroRPC.daemonGetInfo()
                                              ↓ (internal only)
                                    MoneroRPC_ExternalAPIAdapter
                                      → https://crypto.sandbox.cipherbank.money/monero/json_rpc
                                         daemon :18081 | wallet-rpc :18088
```

Public HTTP today does **not** expose wallet CRUD, balance, or sync. Those methods exist on `MoneroRPC_ExternalAPIAdapter` for server-side use.

## Hybrid custody model

| Mode | Client holds | Server holds | wallet-rpc call |
|------|--------------|--------------|-----------------|
| **managed** | Nothing (address + balances via API) | Spend + view (wallet file) | `create_wallet` / `restore_deterministic_wallet` |
| **unmanaged** | Spend key (+ seed) on device | View-only wallet for sync | `generate_from_keys` (viewKey + address; no spendKey) |
| **watch** | Address only | Optional metadata | None / validate_address only |

### Invariants

1. Unmanaged **spend key** and Monero seed never in API bodies, logs, or AsyncStorage plaintext.
2. Managed spend material never returned to the client.
3. Unmanaged may register **viewKey** once for server-side sync; prefer not to persist viewKey on device after registration (fingerprint only in local drafts).

## `/v1` ↔ wallet-rpc method map

| App endpoint | Upstream (Wallet_Handler → MoneroRPC) |
|--------------|----------------------------------------|
| `POST /wallets` mode=managed | `walletCreateWallet` (+ `walletGetAddress`) |
| `POST /wallets` mode=unmanaged | `walletGenerateFromKeys` (omit spendKey) |
| `GET /wallets`, `GET /wallets/:id` | `walletGetBalance`, `walletGetAddress`, `walletGetHeight` |
| `POST /wallets/:id/refresh` | `walletAutoRefresh` / refresh |
| Future transfer (managed) | `walletTransfer` / `walletTransferSplit` |
| Future transfer (unmanaged) | Client constructs; optional `walletRelayTx` / describe |

Daemon probe (currency availability): `daemonGetInfo` — already used by `/currencies`.

Transport notes (CipherBank-src `agents/adapters/monero_rpc.md`): JSON-RPC POST to `/json_rpc`, snake_case fields, full response shapes when stubbing.

## Market data

Authoritative public wire format: [`PUBLIC_API.md`](./PUBLIC_API.md) · [`CB_InitialAPIRef.html`](./CB_InitialAPIRef.html).

| App | CipherBank public API |
|-----|------------------------|
| Rates cache / P2–P3 | `POST /currencies` + `POST /iquote` (1 → USD) |
| Convert input amount | `POST /iquote` `{ INPUT_AMOUNT, INPUT_CURRENCY, OUTPUT_CURRENCY }` |
| Convert reverse | `POST /quote` `{ INPUT_CURRENCY, OUTPUT_AMOUNT, OUTPUT_CURRENCY }` |
| `GET /history` bulk | **Not implemented** in CipherBank-src — needs OHLC feeder |
| ~~`GET /rates`~~ / ~~`POST /quotes`~~ | Deprecated app conveniences |

## Currency code alias

| App symbol | Public API / backend `Currency` |
|------------|----------------------------------|
| `XMR` | `MONERO` · Coinbase `XMR` · Kraken `XXMRZ` |
| `BTC` | `BITCOIN` |
| `USD` | `USD` |

## Backend follow-up (not this Expo slice)

1. Wallet_Handler product messages wrapping create / generate_from_keys / get_balance / get_address / refresh.
2. HTTP_Handler: expose `/v1/wallets*` (or message-style equivalents) + `XMR`↔`MONERO` alias.
3. History service or external OHLC feeder for bulk `/history` (range + granularity + from/to).

## Client storage (Expo)

| Data | Where |
|------|--------|
| Unmanaged spend / seed | Device SecureStore custody (separate from BTC BIP39 if needed later) |
| Wallet metadata (id, label, mode, address, sync) | AsyncStorage drafts + `/wallets` server list |
| View key | Sent once on `POST /wallets`; not kept in AsyncStorage |
| Live rates | React Query cache on `GET /rates` (~5–15s staleTime) |
