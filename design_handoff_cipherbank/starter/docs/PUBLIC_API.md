# CipherBank Public API — wire standards

Canonical runtime reference (generated from CipherBank-src):

- HTML (PriceCache only): [`CB_InitialAPIRef.html`](./CB_InitialAPIRef.html)
- HTML (full app surface): [`CB_FullAPIRef.html`](./CB_FullAPIRef.html) — regenerate: `node scripts/generate-api-ref.mjs`
- Host: `api.cipherbank.money`
- OpenAPI: `https://api.cipherbank.money/docs/openapi.json` (when published)

App env: `EXPO_PUBLIC_PUBLIC_API_BASE` (default `https://api.cipherbank.money`). Paths are **not** under `/v1`.

## Standards (all public + new market-facing routes)

| Rule | Value |
|------|--------|
| Transport | HTTP/1.1 · `Content-Type: application/json` · `Accept: application/json` |
| Field names | **SCREAMING_SNAKE_CASE** (`INPUT_AMOUNT`, `OUTPUT_CURRENCY`) |
| Currency codes | Full names where defined: `BITCOIN`, `MONERO`, `USD` (not `BTC` / `XMR`) |
| Amounts | JSON **number (double)** — not decimal strings |
| Query style | **POST** with JSON body (empty `{}` when no fields) |
| Status codes | `200` ok · `406` Accept · `415` Content-Type · `417` parse/type · `422` business invalid · `424` dependency down |

### JSON value representations

As documented in the HTML: int64 / uint64 / double / UTF-8 string / boolean with stated ranges.

## Endpoints (live today)

| Method | Path | Purpose |
|--------|------|---------|
| POST | `/test` | Connectivity → `{}` |
| POST | `/currencies` | `{ "CURRENCIES": ["BITCOIN","MONERO","USD"] }` |
| POST | `/iquote` | Given `INPUT_AMOUNT` → compute `OUTPUT_AMOUNT` |
| POST | `/quote` | Given `OUTPUT_AMOUNT` → compute `INPUT_AMOUNT` |

### `/iquote` example

```json
{
  "INPUT_AMOUNT": 0.0015,
  "INPUT_CURRENCY": "BITCOIN",
  "OUTPUT_CURRENCY": "USD"
}
```

→

```json
{
  "INPUT_AMOUNT": 0.0015,
  "INPUT_CURRENCY": "BITCOIN",
  "OUTPUT_AMOUNT": 100.0,
  "OUTPUT_CURRENCY": "USD"
}
```

## App mapping

| UI ticker | Public code |
|-----------|-------------|
| BTC | BITCOIN |
| XMR | MONERO |
| USD | USD |
| ETH | ETHEREUM |
| LTC | LITECOIN |
| DOGE | DOGECOIN |

Codec: `src/lib/publicCurrency.ts` · Client: `src/lib/publicApiClient.ts` · Feature: `src/features/market/publicMarket.api.ts`.

| App need | Public call |
|----------|-------------|
| Rates cache / P2–P3 refresh | `POST /currencies` + `POST /iquote` (1 → USD) per code |
| Convert lock (input amount) | `POST /iquote` |
| Convert reverse (output amount) | `POST /quote` |

UI still displays short tickers; encoding happens at the API boundary.

## Product `/v1` vs public API

| Surface | Base | Wire style |
|---------|------|------------|
| **Public** (PriceCache) | `api.cipherbank.money` | SCREAMING_SNAKE · POST · doubles |
| **Product** (session, portfolio, ACH, POS, convert settle) | `EXPO_PUBLIC_API_BASE` (`…/v1`) | SCREAMING_SNAKE on wire · REST methods · encode via `wireFormat.ts` |

**All** CipherBank-src HTTP routes use SCREAMING_SNAKE. See [`CB_FullAPIRef.html`](./CB_FullAPIRef.html) for the complete endpoint catalog.

### Deprecated app conveniences

| Old | Replacement |
|-----|-------------|
| `GET /rates` | `POST /currencies` + `POST /iquote` |
| `POST /quotes` `{ from, to, amount }` | `POST /iquote` (lock TTL remains client-side until product lock exists) |

Mock handlers still accept the old paths for one release.

## Never on this API

Mnemonic, spend keys, PIN, PAN/CVV, full ACH account numbers — same custody rules as product `/v1`.
