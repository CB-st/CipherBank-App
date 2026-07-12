# CipherBank `/v1` API Contract (App ↔ Backend)

Canonical shapes the **live API must return** so the Expo app can switch off mocks (`EXPO_PUBLIC_USE_MOCK=false`). Fixture sources live in [`fixtures/`](./fixtures/). POS detail: [`POS_API.md`](./POS_API.md). Original sketch: [`../API.md`](../API.md).

**Conventions**
- Base: `https://api.cipherbank.dev/v1` · Stream: `wss://api.cipherbank.dev/v1/stream`
- Auth: `Authorization: Bearer <token>` (except `POST /session`)
- Amounts in asset units are **strings**; USD display fields may be numbers
- Mutations accept `Idempotency-Key`; errors: `{ "code", "message", "detail"? }`
- **Never** accept mnemonic / PAN / CVV from the client

---

## Endpoint index

| Method | Path | Source |
|--------|------|--------|
| GET | `/portfolio` | `fixtures/portfolio.json` |
| GET | `/assets` | `fixtures/assets.json` |
| GET | `/rates` | `fixtures/rates.json` |
| GET | `/recipients` | `fixtures/recipients.json` |
| GET | `/activity` | `fixtures/activity.json` |
| GET/PUT | `/prefs` | `fixtures/prefs.json` |
| GET | `/vault/binaries` | `fixtures/vault-binaries.json` |
| GET | `/vault/cards` | `fixtures/vault-cards.json` |
| GET | `/receive/:asset` | `fixtures/receive.json` (keyed by asset) |
| GET | `/history?range=&granularity=&symbols=&from=&to=` | bulk series (OHLC-capable) |
| GET | `/wallets?symbol=` | list wallet accounts |
| GET | `/wallets/:id` | wallet detail + sync |
| POST | `/wallets` | create/import managed · unmanaged · watch |
| POST | `/wallets/:id/refresh` | kick sync |
| POST | `/session` · `/session/refresh` | computed tokens |
| POST | `/quotes` | computed `{ quoteId, rate, amountOut, expiresAt, fee }` |
| POST | `/convert` · `/transfers` · `/payments` | `202` accepted + stream settle |
| POST | `/receive/request` | computed URI |
| POST | `/recipients` · `/banks/link` | stubs |
| POST | `/vault/binaries` · `/vault/cards` · `/vault/cards/:id/delete` | vault writes |
| POST | `/pos/sessions` · `/pos/authorize` · `/pos/confirm` | see POS_API.md |
| GET | `/pos/sessions/:id` | session status |
| WSS | `/stream` | `balance.update`, `rate.tick`, `*.settled`, `pos.settled` |

---

## 1 · Portfolio — `GET /portfolio`

```json
{
  "total": 128432.19,
  "change24h": { "amount": 3012.4, "pct": 2.4 },
  "holdings": [
    {
      "symbol": "BTC",
      "name": "Bitcoin",
      "glyph": "₿",
      "type": "crypto",
      "amount": "1.204",
      "usdValue": 76104.22,
      "change24h": 1.8,
      "wallets": [
        {
          "id": "wal_btc_primary",
          "label": "Primary",
          "amount": "1.004",
          "usdValue": 63420.18,
          "address": "bc1qxy2kgdygjrsqtzq2n0yrf2493p83kkfjhx0wlh",
          "derivationPath": "m/84'/0'/0'/0/0",
          "source": "local"
        }
      ]
    }
  ]
}
```

`type`: `crypto` | `fiat` | `security`. Optional `note` (e.g. `shielded`, `instant ACH`).

`wallets` (crypto): one or more accounts under the asset. `source`: `local` | `watch` | `server`. Aggregates `amount` / `usdValue` on the holding should match the sum of wallets. Clients may also attach local drafts (AsyncStorage) until chain read / derivation is live — see `POST` wallet slots below once backend-owned.

---

## 2 · Assets catalog — `GET /assets`

```json
{
  "assets": [
    {
      "symbol": "BTC",
      "name": "Bitcoin",
      "glyph": "₿",
      "type": "crypto",
      "decimals": 8,
      "enabled": true
    },
    {
      "symbol": "AAPL",
      "name": "Apple",
      "glyph": "◆",
      "type": "security",
      "decimals": 4,
      "enabled": false,
      "badge": "NEW"
    }
  ]
}
```

---

## 3 · Rates — `GET /rates`

Live **price cache snapshot** for Convert and Home (short TTL; clients should use `staleTime` ~5–15s). Maps to CipherBank-src PriceCache / HTTP `/quote` · `/iquote` when live (currency codes: app `XMR` ↔ backend `MONERO`).

```json
{
  "rates": [
    { "symbol": "BTC", "usd": 63204.18, "change24h": 1.8 },
    { "symbol": "XMR", "usd": 160.0, "change24h": 0.4 }
  ],
  "generatedAt": 1720900000000,
  "ttlMs": 10000
}
```

---

## 3b · Wallets — Monero-first product surface

Hybrid light wallets. See also [`docs/MONERO_LINK.md`](../docs/MONERO_LINK.md).

**Modes**

| `mode` | Spend key | Server role | App `source` |
|--------|-----------|-------------|--------------|
| `managed` | Server (wallet-rpc) | Create + sync + balance | `server` |
| `unmanaged` | Device only | View-key sync / balance via wallet-rpc | `local` |
| `watch` | None | Address metadata only | `watch` |

**Never** send mnemonic, spend key, or BIP39 seed in these bodies. Unmanaged may send **viewKey + address** once at registration.

### `GET /wallets?symbol=XMR`

```json
{
  "wallets": [
    {
      "id": "wal_xmr_1",
      "symbol": "XMR",
      "label": "Primary",
      "mode": "managed",
      "address": "4…",
      "balance": "12.5",
      "unlockedBalance": "12.5",
      "restoreHeight": 3100000,
      "sync": { "height": 3100500, "target": 3100500, "state": "synced" }
    }
  ]
}
```

### `POST /wallets`

```json
{
  "symbol": "XMR",
  "label": "Cold view",
  "mode": "unmanaged",
  "address": "4…",
  "viewKey": "…",
  "restoreHeight": 3100000
}
```

Managed: `{ "symbol": "XMR", "label": "Managed", "mode": "managed" }`  
Watch: `{ "symbol": "XMR", "label": "Watch", "mode": "watch", "address": "4…" }`

→ `{ walletId, symbol, label, mode, address, sync }`

### `GET /wallets/:id`

Same fields as list item (single object).

### `POST /wallets/:id/refresh`

→ `{ id, sync }` — kicks wallet-rpc refresh / auto-refresh.

### Future — `POST /wallets/:id/transfer`

Documented only. Managed: server builds transfer. Unmanaged: client-side sign (out of this contract slice).

---

## 4 · Recipients — `GET /recipients`

```json
{
  "recipients": [
    {
      "id": "maya",
      "name": "Maya Chen",
      "handle": "maya@cipherbank.id",
      "bank": { "label": "Chase", "last4": "4021", "rail": "ACH" },
      "initials": "MC"
    },
    {
      "id": "sunset",
      "name": "Sunset Property Mgmt",
      "handle": "sunset@property.pay",
      "memo": "Rent · due Jul 1",
      "initials": "SP"
    }
  ]
}
```

---

## 5 · Activity — `GET /activity`

```json
{
  "items": [
    {
      "id": "tx_1",
      "kind": "convert",
      "status": "settled",
      "title": "Converted BTC → USD",
      "amount": "31570.44",
      "asset": "USD",
      "counterpart": null,
      "createdAt": 1720900000000
    }
  ],
  "nextCursor": null
}
```

`kind`: `convert` | `transfer` | `payment` | `receive`. Cursor-paginated when `nextCursor` is set.

---

## 6 · Preferences — `GET/PUT /prefs`

```json
{
  "homeOrder": ["cora", "balance", "quickActions", "performance", "assets"],
  "homeVisible": {
    "cora": true,
    "balance": true,
    "quickActions": true,
    "performance": true,
    "assets": true
  },
  "valuesHiddenOnLaunch": false,
  "coraEnabled": true,
  "defaultSendSpeed": "instant",
  "appearance": "dark"
}
```

`defaultSendSpeed`: `instant` | `ach`. `appearance`: `dark` (default) | `light`. Local AsyncStorage mirrors; `PUT` syncs cross-device.

---

## 7 · Vault binaries — `GET /vault/binaries`

```json
{
  "binaries": [
    {
      "id": "bin_primary",
      "label": "Primary wallet binary",
      "kind": "server_shard",
      "status": "active",
      "createdAt": 1720900000000
    }
  ]
}
```

Server-held wallet binary **references** only — not private keys.

---

## 8 · Vault cards — `GET /vault/cards`

```json
{
  "cards": [
    {
      "id": "card_tok_nfc_bench_4242",
      "brand": "Visa",
      "last4": "4242",
      "expMonth": 12,
      "expYear": 2028,
      "processorToken": "tok_mock_nfc_bench_4242",
      "hardwareTest": true,
      "label": "NFC bench Visa ·••• 4242",
      "createdAt": 1720640800000
    }
  ]
}
```

Processor tokens only. `hardwareTest: true` marks NFC lab cards (`EXPO_PUBLIC_POS_REQUIRE_TEST_CARD`).

---

## 9 · Receive — `GET /receive/:asset`

Per-asset object (fixture is a map; handler returns one entry):

```json
{
  "handle": "cora@cipherbank.id",
  "address": "bc1q…",
  "uri": "bitcoin:bc1q…",
  "qr": "bitcoin:bc1q…"
}
```

`POST /receive/request` `{ asset, amount }` → same fields plus `amount` and amount-bearing `uri`/`qr`.

---

## 10 · History — bulk market / portfolio series

`GET /history?range=1M&granularity=1h&symbols=BTC,ETH,XMR&from=&to=`

Bulk block for charts. Live Convert pricing uses `/rates` + `/quotes`, not this endpoint.

```json
{
  "series": [
    {
      "label": "Wallet",
      "symbol": "WALLET",
      "granularity": "1h",
      "points": [
        { "t": 1720900000000, "v": 100000, "o": 99800, "h": 100500, "l": 99500, "c": 100000 }
      ]
    }
  ],
  "meta": { "source": "mock", "generatedAt": 1720900000000 }
}
```

| Param | Values | Notes |
|-------|--------|-------|
| `range` | `1D` \| `1W` \| `1M` \| `1Y` \| `ALL` | Preset window (ignored if `from`+`to` set) |
| `granularity` | `1m` \| `5m` \| `1h` \| `1d` | Candle / point specificity |
| `symbols` | comma list | Extends legacy `compare=` |
| `from` / `to` | unix ms | Custom bulk window |

`v` is the chart close (same as `c` when OHLC present). CipherBank-src has no history feeder yet — see `MONERO_LINK.md`.

---

## 11 · Session

`POST /session` → `{ token, refreshToken, expiresAt, userId }`  
`POST /session/refresh` `{ refreshToken }` → new tokens  

Custody keys stay on-device; session proves device/user only.

---

## 12 · Quotes / Convert / Transfers / Payments

### `POST /quotes` `{ from, to, amount }`

```json
{
  "quoteId": "q_…",
  "from": "BTC",
  "to": "USD",
  "rate": 63204.18,
  "amountOut": "31602.09",
  "expiresAt": 1720900015000,
  "fee": "0.00"
}
```

### `POST /convert` `{ quoteId, amount }` + Idempotency-Key

→ `{ "txId", "status": "accepted" }` then stream `convert.settled`.

### `POST /transfers` `{ recipient, amount, source, speed }`

`speed`: `instant` | `ach` → `{ "txId", "status": "accepted" }`.

### `POST /payments` `{ recipient, total, sources: [{ asset, value }] }`

Sum of sources must cover `total` or `422 mix_undercovered` → `{ "paymentId", "status": "accepted" }`.

---

## 13 · POS / tap-to-pay

See [`POS_API.md`](./POS_API.md).

Summary:
1. `POST /pos/sessions` → `pending_auth`
2. `POST /pos/authorize` `{ sessionId, sources, cardId, deviceAttestation }` → ephemeral `presentment.tokenRef`
3. `POST /pos/confirm` → `ready_to_present`
4. NFC / mock payload: `{ v: 1, sessionId, tokenRef, merchantId? }` — **no PAN**

Errors: `pos_expired`, `insufficient_funds`, `wallet_locked`, `test_card_required`, `card_not_found`.

---

## 14 · Stream events

```json
{ "type": "balance.update", "payload": { /* Portfolio */ } }
{ "type": "rate.tick", "payload": { "pair": "BTC/USD", "rate": 63204.18, "ts": 1720900000000 } }
{ "type": "convert.settled", "payload": { "txId": "…", "amountOut": "…" } }
{ "type": "transfer.settled", "payload": { "txId": "…", "arrivedAt": 1720900000000 } }
{ "type": "payment.settled", "payload": { "paymentId": "…", "breakdown": [] } }
{ "type": "pos.settled", "payload": { "sessionId": "…", "receiptId": "…", "amount": "…", "currency": "USD" } }
```

---

## Stable error codes

| code | meaning |
|------|---------|
| `not_found` | Unknown path / id |
| `quote_expired` | Quote past `expiresAt` |
| `insufficient_funds` | Balance / POS funding short |
| `mix_undercovered` | Pay-mix under 100% |
| `custody_local_only` | Mnemonic leaked in body |
| `wallet_locked` | Missing POS attestation |
| `pos_expired` | POS session expired |
| `test_card_required` | Lab requires `hardwareTest` card |
| `card_not_found` | Unknown vault card |
| `method_not_allowed` | Unsupported HTTP method |

---

## App screen → API map

| Screen | Endpoints |
|--------|-----------|
| Home | `GET /portfolio`, `GET /history`, `GET /rates` |
| Convert | `GET /rates` (cache), `POST /quotes`, `POST /convert` + stream |
| Send | `POST /transfers`, recipients |
| Pay | `POST /payments` |
| Receive | `GET /receive/:asset` |
| Assets / Add wallet | `POST /wallets`, `GET /wallets` (XMR); local derive for BTC/ETH |
| Profile / prefs | `GET/PUT /prefs`, vault lists |
| Tap to pay lab | `/pos/*`, vault cards, local unlock |

---

## Switching to live API

```bash
EXPO_PUBLIC_USE_MOCK=false
EXPO_PUBLIC_API_BASE=https://your-api.example/v1
EXPO_PUBLIC_WSS=wss://your-api.example/v1/stream
```

Diff live responses against this document and `fixtures/*.json`. Keep mocks for offline UI work.
