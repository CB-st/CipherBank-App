# API.md — Cipherbank Service Interface

The server-side contract the app consumes and the **open API standard** banks/developers embed. Everything is versioned under `/v1`. This is the checklist of services to build to support the app.

Conventions:
- **Base:** `https://api.cipherbank.dev/v1` · **Stream:** `wss://api.cipherbank.dev/v1/stream`
- **Auth:** `Authorization: Bearer <token>` on all endpoints except `POST /session`. Custody keys are generated on-device and **never** transmitted.
- **Money:** all amounts are **strings** in asset units (bignumber-safe). USD values are numbers only for display fields.
- **Idempotency:** every mutating POST accepts an `Idempotency-Key` header; the server must dedupe and return the original result on retry.
- **Errors:** non-2xx returns `{ "code": string, "message": string, "detail"?: object }`. Use stable machine `code`s (e.g. `quote_expired`, `insufficient_funds`, `rate_moved`).
- **Async settlement:** slow work (network settlement, multi-asset mediation) returns an **accepted** ack immediately with a `txId`/status; the definitive result arrives over the stream. Clients render optimistically in between.

---

## Services overview

| # | Service | Purpose | Key endpoints |
|---|---|---|---|
| 1 | **Session & Custody** | Auth, device binding, biometrics | `POST /session`, `POST /session/refresh` |
| 2 | **Portfolio** | Aggregated balances across assets | `GET /portfolio`, `GET /assets` |
| 3 | **Quotes** | Time-boxed conversion rates | `POST /quotes` |
| 4 | **Convert** | Crypto ⇄ fiat conversion | `POST /convert`, `GET /convert/:id` |
| 5 | **Transfers** | Send / ACH | `POST /transfers`, `GET /transfers/:id` |
| 6 | **Payments (mix)** | Pay a bill with multiple sources | `POST /payments`, `GET /payments/:id` |
| 7 | **Receive** | Handle + address + requests | `GET /receive/:asset`, `POST /receive/request` |
| 8 | **Recipients** | Saved people & linked banks | `GET/POST /recipients`, `POST /banks/link` |
| 9 | **Activity** | Unified transaction history | `GET /activity` |
| 10 | **Rates/Market** | Reference prices & 24h change | `GET /rates`, stream `rate.tick` |
| 11 | **Stream** | Real-time balances & settlement | `WSS /stream` |

---

## 1 · Session & Custody
Keys live on-device. The server authenticates the device/user and issues short-lived tokens.

`POST /session` → prove ownership (signed challenge from the on-device key) → `{ token, refreshToken, expiresAt, userId }`
`POST /session/refresh` `{ refreshToken }` → new token.

Server builds: challenge/nonce issuance, signature verification against the user's public key, device registry, token lifecycle. **No private keys or mnemonics ever stored server-side.**

## 2 · Portfolio & Assets
`GET /portfolio` →
```json
{
  "total": 128432.19,
  "change24h": { "amount": 3012.40, "pct": 2.4 },
  "holdings": [
    { "symbol": "BTC", "name": "Bitcoin", "glyph": "\u20BF", "type": "crypto",
      "amount": "1.204", "usdValue": 76104.22, "change24h": 1.8 },
    { "symbol": "USD", "name": "US Dollar", "glyph": "$", "type": "fiat",
      "amount": "8204.00", "usdValue": 8204.00, "change24h": 0, "note": "instant ACH" }
  ]
}
```
`GET /assets` → catalog of supported assets (symbol, name, glyph, type, decimals, enabled, `note`). Drives selectors. Supported today: **crypto** BTC, ETH, DOGE, XMR (shielded), LTC · **fiat** USD, EUR, JPY · **security** (coming — return `enabled:false`, `badge:"NEW"`).

Server builds: balance aggregation across custody sources, USD valuation via the rates service, 24h deltas. Optimize for a fast summary read (time-of-flight): consider a cheap `?summary=true` returning total + top holdings first.

## 3 · Quotes
`POST /quotes` `{ from, to, amount }` → `{ quoteId, from, to, rate, amountOut, expiresAt, fee }`
- `expiresAt` is epoch ms; the client counts down and re-quotes on expiry.
- Quote must be **honored at settlement** if redeemed before `expiresAt` with its `quoteId`.

Server builds: pricing engine, spread/fee policy (design promises **$0 conversion fee** to the user — model any spread server-side), short-lived quote store keyed by `quoteId`.

## 4 · Convert
`POST /convert` `{ quoteId, amount }` + `Idempotency-Key` → `202 { txId, status: "accepted" }`
`GET /convert/:id` → `{ txId, status: "accepted|settling|settled|failed", rate, amountOut, fee, settledAt? }`
- On expired/invalid quote → `409 { code: "quote_expired" }` (client re-quotes, blocks stale confirm).
- Definitive settlement pushed via stream `convert.settled`.

Server builds: quote redemption + lock, execution against liquidity, **instant** internal settlement to the user's balance, shielded-swap handling for privacy assets (XMR).

## 5 · Transfers (Send / ACH)
`POST /transfers` `{ recipient, amount, source, speed }` (`speed: "instant"|"ach"`) + `Idempotency-Key` → `202 { txId, status }`
`GET /transfers/:id` → status + `arrivesAt`.
- Cross-asset: if `source` isn't the recipient's currency, server converts first (quote internally) then sends.
- Privacy: counterparties see a **handle**, not the user's identity.

Server builds: instant internal rail + external ACH rail, recipient resolution (handle ↔ bank), speed routing, settlement events (`transfer.settled`).

## 6 · Payments (Pay with a mix) — the differentiator
`POST /payments` `{ recipient, total, sources: [ { asset, value } ] }` + `Idempotency-Key` → `202 { paymentId, status }`
`GET /payments/:id` → status + per-source breakdown.
- Server validates `sum(sources.value) == total`; else `422 { code: "mix_undercovered" }`.
- Server **mediates the multi-asset exchange in real time** and settles **clean single-currency funds** to the recipient — the recipient never sees the mix.
- Sources can include crypto, fiat, and (later) securities.

Server builds: atomic multi-leg exchange (all-or-nothing across sources), mediation/settlement to recipient rail, receipt with per-source breakdown.

## 7 · Receive
`GET /receive/:asset` → `{ handle, address, uri, qr }` (address/URI for the chosen asset).
`POST /receive/request` `{ asset, amount }` → shareable request `{ handle, amount, uri, qr }`.

Server builds: per-asset address derivation/rotation, payment-request URIs, QR payloads.

## 8 · Recipients & Banks
`GET /recipients` · `POST /recipients` `{ handle | bank }` → saved list.
`POST /banks/link` → ACH bank linking (provider handshake, e.g. Plaid) → linked account token.

## 9 · Activity
`GET /activity?cursor=&limit=` → paginated unified history (converts, sends, payments, receives) with status, amounts, counterparties (as handles). Cursor-paginated for infinite scroll.

## 10 · Rates / Market
`GET /rates?symbols=BTC,ETH,...` → reference prices + 24h change (backs portfolio valuation & tickers). Real-time updates via stream `rate.tick`.

## 11 · Stream (WSS)
Client connects after auth. Server pushes:
```
{ "type": "balance.update",   "payload": <Portfolio> }
{ "type": "rate.tick",        "payload": { "pair": "BTC/USD", "rate": 63204.18, "ts": 1720900000000 } }
{ "type": "convert.settled",  "payload": { "txId": "...", "amountOut": "31570.44" } }
{ "type": "transfer.settled", "payload": { "txId": "...", "arrivedAt": 1720900000000 } }
{ "type": "payment.settled",  "payload": { "paymentId": "...", "breakdown": [ ... ] } }
```
Client writes these into the React Query cache (`socket.ts`) so multi-second settlement updates the UI without polling. Server builds: authenticated pub/sub, per-user channels, reconnection/resume (last-event-id).

---

## Build priority (server)
1. **Session** (unblocks everything) → **Portfolio + Assets + Rates** (proves the read path & loading UX).
2. **Quotes + Convert + Stream** (proves rate-lock + optimistic settlement — the core promise).
3. **Transfers + Receive + Recipients/Banks**.
4. **Payments (mix)** — depends on Convert/pricing; the differentiator.
5. **Activity**, then **Securities** support across Assets/Quotes/Payments.

## Cross-cutting requirements
- Idempotency store for all mutations · rate limiting · audit log.
- Consistent error `code`s (`quote_expired`, `insufficient_funds`, `mix_undercovered`, `rate_moved`, `recipient_unresolved`).
- Compliance hooks (KYC/AML, travel rule) without leaking counterparty identity to the app (handles only).
- **Public-standard hygiene:** version (`/v1`), publish OpenAPI, keep request/response shapes stable — these same endpoints are what partner banks embed.
