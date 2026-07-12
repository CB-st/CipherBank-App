# POS / Tap-to-Pay API — backend checklist

Consumer CipherBank wallets fund a **tokenized card presentment** at point of sale: unlock custody → authorize crypto sources → mint ephemeral processor token → present `tokenRef` over NFC (or mock tap).

Base: `/v1` · Auth: `Authorization: Bearer <token>` · Amounts: **strings** · Idempotency on mutating POSTs.

## Flow

```
POST /pos/sessions
  → pending_auth
POST /pos/authorize  (+ deviceAttestation, sources[], cardId)
  → authorized + presentment { tokenRef, last4, brand, ttlMs }
POST /pos/confirm
  → ready_to_present | settled
NFC / mock tap carries { sessionId, tokenRef } only
```

Optional stream: `{ "type": "pos.settled", "payload": { "sessionId", "receiptId", "amount", "currency" } }`

## Endpoints

### `POST /pos/sessions`

Request:
```json
{
  "merchantId": "merchant_lab_sunset",
  "amount": "42.50",
  "currency": "USD",
  "posDeviceId": "lab_mock_pos",
  "label": "Coffee"
}
```

Response `201`:
```json
{
  "sessionId": "pos_…",
  "merchantId": "merchant_lab_sunset",
  "amount": "42.50",
  "currency": "USD",
  "label": "Coffee",
  "status": "pending_auth",
  "expiresAt": 1720900060000
}
```

### `POST /pos/authorize` + `Idempotency-Key`

Request:
```json
{
  "sessionId": "pos_…",
  "sources": [{ "asset": "BTC", "value": "0.0007" }],
  "cardId": "card_tok_nfc_bench_4242",
  "fundingQuoteId": null,
  "deviceAttestation": "unlocked_local_custody_v1"
}
```

Response `200`:
```json
{
  "sessionId": "pos_…",
  "status": "authorized",
  "ephemeralCardTokenId": "eph_…",
  "presentment": {
    "tokenRef": "ptr_…",
    "last4": "4242",
    "brand": "Visa",
    "ttlMs": 60000
  }
}
```

Server responsibilities:
- Verify session not expired.
- Verify `deviceAttestation` (wallet unlocked) — **never** accept mnemonic/PAN/CVV.
- Mediate crypto → card rail (reuse convert/pricing).
- Mint short-lived **processor** ephemeral token bound to `cardId`.
- If lab flag `POS_REQUIRE_TEST_CARD`: reject non-`hardwareTest` cards with `test_card_required`.

### `POST /pos/confirm` + `Idempotency-Key`

Request: `{ "sessionId": "pos_…" }`  
Response: `{ "sessionId", "status": "ready_to_present" | "settled" }`

### `GET /pos/sessions/:id`

Returns current `PosSession` including `presentment` when authorized.

## Error codes

| code | when |
|------|------|
| `pos_expired` | session past `expiresAt` |
| `insufficient_funds` | sources do not cover amount |
| `wallet_locked` | missing/invalid attestation |
| `nfc_not_supported` | client-only; server may ignore |
| `test_card_required` | lab mode + non-hardwareTest card |
| `card_not_found` | unknown `cardId` |
| `mix_undercovered` | sum(sources) < amount |

## NFC payload (client → terminal)

```json
{ "v": 1, "sessionId": "pos_…", "tokenRef": "ptr_…", "merchantId": "…" }
```

No PAN. Full EMV/HCE APDU mapping is processor-specific and outside this contract.

## Security rules

1. Recovery mnemonic stays on-device forever.
2. Card vault stores processor tokens + display metadata only.
3. Ephemeral presentment TTL ≤ 60s; reject presentment after expiry.
4. Idempotent authorize/confirm.
