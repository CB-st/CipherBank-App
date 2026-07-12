Canonical shapes: **[`API_CONTRACT.md`](./API_CONTRACT.md)** (all fixtures + computed endpoints). POS: [`POS_API.md`](./POS_API.md).

When `EXPO_PUBLIC_USE_MOCK=true`, [`apiClient`](../lib/apiClient.ts) routes all traffic through this folder instead of the network. Fixture JSON is the **expected response contract** the real API must match.

## Switch

| Env | Behavior |
|-----|----------|
| `EXPO_PUBLIC_USE_MOCK=true` | In-process handlers + artificial latency + mock stream |
| `EXPO_PUBLIC_USE_MOCK=false` | Live `EXPO_PUBLIC_API_BASE` / `EXPO_PUBLIC_WSS` |

Also: `EXPO_PUBLIC_MOCK_HAS_WALLET=true` skips onboarding so UI work lands on the tab shell.

## Fixture → endpoint map

| Fixture / handler | Method | Path | Notes |
|-------------------|--------|------|-------|
| `fixtures/portfolio.json` | GET | `/portfolio` | Holdings, total, 24h change |
| `fixtures/assets.json` | GET | `/assets` | Catalog; securities `enabled:false`, `badge:"NEW"` |
| `fixtures/rates.json` | GET | `/rates` | Reference USD prices |
| `fixtures/recipients.json` | GET | `/recipients` | Saved people / bills |
| `fixtures/activity.json` | GET | `/activity` | Unified history page |
| `fixtures/receive.json` | GET | `/receive/:asset` | handle, address, uri, qr |
| `fixtures/prefs.json` | GET/PUT | `/prefs` | Home layout, privacy, Cora, default send speed |
| `fixtures/vault-binaries.json` | GET | `/vault/binaries` | Server-held wallet binary refs |
| `fixtures/vault-cards.json` | GET | `/vault/cards` | Card **processor tokens** (never PANs); `hardwareTest` for NFC lab |
| computed | POST | `/vault/binaries` | Register binary metadata |
| computed | POST | `/vault/cards` | Tokenize card (mock) |
| computed | POST | `/vault/cards/:id/delete` | Remove card token |
| — | POST | `/pos/sessions` | Start POS session (see `POS_API.md`) |
| — | POST | `/pos/authorize` | Crypto → ephemeral card token |
| — | POST | `/pos/confirm` | ready_to_present |
| — | GET | `/pos/sessions/:id` | Session status |
| computed in `handlers.ts` | GET | `/history?range=&compare=` | Chart series (WALLET + compare) |
| computed | POST | `/quotes` | `{ quoteId, rate, amountOut, expiresAt, fee }` · TTL 15s |
| computed | POST | `/convert` | `202 { txId, status:"accepted" }` → stream `convert.settled` |
| computed | POST | `/transfers` | accepted → `transfer.settled` |
| computed | POST | `/payments` | validates mix coverage; `mix_undercovered` if under |
| computed | POST | `/receive/request` | amount-request URI |
| computed | POST | `/session` · `/session/refresh` | mock tokens |
| computed | POST | `/recipients` · `/banks/link` | stubs |

## Hybrid vault rules

1. **Recovery mnemonic is local-only.** Any request body containing `mnemonic` / recovery seed is rejected with `custody_local_only`.
2. **Server binaries** are references (`id`, `label`, `kind`, `status`) — not private key material in the clear.
3. **Card tokens** store `processorToken` + display fields (`brand`, `last4`, exp). Never raw PAN/CVV.
4. Client caches card list for UI; authoritative store is the server vault.

**POS / tap-to-pay:** full contract in [`POS_API.md`](./POS_API.md). Lab requires `hardwareTest: true` cards when `EXPO_PUBLIC_POS_REQUIRE_TEST_CARD=true`.

## Preferences contract

`UserPrefs` shape:

```json
{
  "homeOrder": ["cora", "balance", "quickActions", "performance", "assets"],
  "homeVisible": { "cora": true, "balance": true, "quickActions": true, "performance": true, "assets": true },
  "valuesHiddenOnLaunch": false,
  "coraEnabled": true,
  "defaultSendSpeed": "instant"
}
```

Local AsyncStorage is source of truth on device; `PUT /prefs` syncs when online.

## Contract rules (backend must honor)

1. **Amounts are strings** in asset units (never floats for balances/payments).
2. **USD display fields** may be numbers (`usdValue`, `total`).
3. **Errors:** `{ code, message, detail? }` with stable codes (`quote_expired`, `insufficient_funds`, `mix_undercovered`, `custody_local_only`, `not_found`).
4. **Mutations:** accept with `{ txId|paymentId, status: "accepted" }`, then settle over the stream.
5. **Idempotency:** `Idempotency-Key` header — mock stores and replays the first response.
6. **Quotes:** honor `quoteId` until `expiresAt` (epoch ms). Client countdown is display-only.

## Stream events (mock + live)

Same shapes as `API.md` §11:

- `balance.update` → replaces portfolio query cache
- `rate.tick` → ticker cache
- `convert.settled` / `transfer.settled` / `payment.settled` → invalidate portfolio + activity

Mock settlements fire ~1.2s after an accepted mutation (`stream.ts`).

## Editing fixtures

Change JSON under `fixtures/`, reload the app. Prefer editing fixtures over hardcoding screen data so the UI and API stay aligned.
