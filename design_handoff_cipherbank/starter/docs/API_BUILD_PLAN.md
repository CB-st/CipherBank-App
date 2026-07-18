# CipherBank `/v1` API Build Plan

> **For agentic workers:** Implement phase-by-phase. Each domain section is a self-contained workstream with checkbox tasks. Contract source of truth: [`../src/mocks/API_CONTRACT.md`](../src/mocks/API_CONTRACT.md) + [`../src/mocks/POS_API.md`](../src/mocks/POS_API.md). App assumes these shapes when `EXPO_PUBLIC_USE_MOCK=false`.

**Goal:** Stand up a production-capable `/v1` backend that the Expo app can cut over to — session, balances, market data, prefs/profile, ACH/bootstrap, money movement, wallets, vault, POS — without ever accepting seed/PAN/CVV from the client.

**Architecture:** HTTP JSON API under `/v1` + authenticated WebSocket `/v1/stream`. Amounts as strings for assets; USD display may be numbers. Mutations that move money require `Idempotency-Key`. Device holds BIP39 / spend keys / full ACH account numbers; server holds public portfolio, prefs metadata, processor card tokens, optional XMR view-key fingerprint, and settlement state.

**Tech stack (suggested):** Match existing CipherBank-src (PriceCache, Monero adapters) for rates/XMR; new or extended HTTP service for session, portfolio aggregation, prefs, transfers, vault, POS. Persist with durable DB (not mock in-memory Maps). Emit stream events the app already handles in `src/lib/socket.ts`.

**Never on the wire:** mnemonic, spend key, PIN plaintext, card PAN/CVV, full ACH account number (last4 + routing OK on bootstrap/public recipients).

---

## How to use this plan

1. Resolve **open decisions** (§0) before coding money rails.
2. Build **P0 → P7** in order; later phases may stub earlier ones but must not invent new response shapes without updating `API_CONTRACT.md` + fixtures.
3. For each endpoint: OpenAPI entry → service → tests against fixture golden JSON → wire staging → app `USE_MOCK=false` smoke.
4. Track progress with the checkboxes; keep mock handlers behavior-compatible until cutover.

**App wiring status today**

| Status | Meaning |
|--------|---------|
| **Wired** | Expo feature calls this path |
| **Mock-only** | Handler/fixture exists; no (or unused) client caller |
| **Local-first** | Device SQLite/SecureStore is source of truth; API is sync/optional |

---

## 0 · Open decisions (unblock before build)

Resolve and record answers in `API_CONTRACT.md` / `ARCHITECTURE.md`:

- [ ] **Recipient source of truth:** cloud `/recipients` vs on-device `ach_recipients` vs `/account/bootstrap` — sync direction (pull-only vs bidirectional)?
- [ ] **Assets catalog:** live `GET /assets` vs keep `assetConfig.ts` as UI source?
- [ ] **Session proof:** signed device challenge vs `{ deviceBound: true }` stub — finalize attestation scheme.
- [ ] **Quote policy:** fee always `"0.00"` vs server spread; must convert reject expired/`unknown` quoteId?
- [ ] **POS funding:** asset amounts vs USD ticket — require `fundingQuoteId` / FX mediation?
- [ ] **Canonical POS settle event:** `pos.settled` vs `payment.settled` (app handles latter; contract documents former).
- [ ] **Receive requests:** implement `POST /receive/request` or keep client-built BIP21 URIs only?
- [ ] **Activity product:** build Activity tab on `GET /activity` or derive locally from settlements?
- [ ] **Bank link:** Plaid-style `POST /banks/link` scope for v1 vs manual ACH only.
- [ ] **Managed XMR:** server holds spend keys — threat model / disclosure vs self-custody branding.
- [ ] **View key retention:** persist after unmanaged register? Rotation/revocation API?
- [ ] **History WALLET series:** server portfolio NAV OHLC vs client-side conversion only.
- [ ] **Securities:** keep `enabled:false` until when?
- [ ] **Idempotency scope:** require keys on quotes / prefs PUT / wallet create?
- [ ] **Reconcile docs:** merge thin `API.md` gaps (`rate_moved`, status GETs, rates `?symbols`) into one OpenAPI from `API_CONTRACT.md`.

---

## Cross-cutting platform tasks (do once, reuse everywhere)

### Task C1: Contract & gateway

- [ ] Publish OpenAPI 3 from `API_CONTRACT.md` + `POS_API.md` (single source).
- [ ] Base URL `https://api.cipherbank.dev/v1` (or staging); WSS `wss://…/v1/stream`.
- [ ] Standard error body `{ code, message, detail? }` for all 4xx/5xx.
- [ ] Reject any body containing mnemonic / seed / spendKey / PAN / CVV → `custody_local_only`.
- [ ] CORS / mobile-safe TLS; request ID header for tracing.

### Task C2: Auth middleware

- [ ] Require `Authorization: Bearer <access>` on all routes except `POST /session` (and health).
- [ ] Access token TTL + refresh token rotation on `POST /session/refresh`.
- [ ] Bind tokens to `userId` + optional `deviceId`.
- [ ] App work item: persist tokens; uncomment Bearer attach in `apiClient.ts`; call refresh on 401.

### Task C3: Idempotency & settlement

- [ ] Store `Idempotency-Key` → response for convert, transfers, payments, POS writes, vault writes (and any other money mutation).
- [ ] Async settle job emits stream events; status resources `GET /{resource}/:id` return current state.
- [ ] Document retry semantics (safe replay within TTL window).

### Task C4: Realtime stream

- [ ] Authenticated WSS (token on connect or first message).
- [ ] Emit: `balance.update`, `rate.tick`, `convert.settled`, `transfer.settled`, `payment.settled`, **`pos.settled`**.
- [ ] Optional `Last-Event-Id` / resume (documented in `API.md`; implement if product needs reconnect catch-up).
- [ ] App: handle `pos.settled` in `socket.ts` if chosen as canonical.

### Task C5: Observability & staging

- [ ] Structured logs (no secrets); metrics for quote TTL misses, settle latency, POS auth failures.
- [ ] Staging environment with seed fixtures matching mock JSON for golden tests.
- [ ] Cutover checklist: `EXPO_PUBLIC_USE_MOCK=false` smoke matrix (Home, Convert, Send, Receive, Profile, POS lab).

---

## P0 · Session & identity

**Why first:** every other call needs a user. App: optional `POST /session` after SetPin (`finishCustodySetup`); refresh unused.

| Method | Path | App | Priority |
|--------|------|-----|----------|
| POST | `/session` | Wired (best-effort) | P0 |
| POST | `/session/refresh` | Mock-only | P0 |

### Tasks

- [ ] **User model:** `userId`, devices[], createdAt; no password if device-bound self-custody (document auth model).
- [ ] **POST `/session`:** accept `{ deviceBound?, deviceAttestation?, deviceId? }` → `{ token, refreshToken, expiresAt, userId }`.
- [ ] Challenge-response or attestation verification (per §0 decision).
- [ ] **POST `/session/refresh`:** rotate tokens; revoke old refresh.
- [ ] Rate-limit session creation; revoke-all-devices endpoint (Profile later).
- [ ] Tests: happy path, invalid attestation, expired refresh.
- [ ] App: secure token storage + Bearer header + refresh interceptor.

**Done when:** app can open a session on clean install and call an authenticated `GET /prefs` successfully against staging.

---

## P1 · Portfolio, balances, holdings

**Why:** Home shell. App: `usePortfolio` → `GET /portfolio`; merges local wallet drafts. Stream `balance.update` rewrites RQ cache.

| Method | Path | App | Priority |
|--------|------|-----|----------|
| GET | `/portfolio` | Wired | P1 |

### Response contract (summary)

```
{ total, change24h:{amount,pct}, holdings[{symbol,name,glyph,type,amount,usdValue,change24h,note?,wallets?}] }
```

### Tasks

- [ ] Aggregate balances per user across **server-known** wallets (managed XMR, linked accounts) + documented rules for **client-local** BTC/ETH/etc. (either omit and let client merge, or accept client-reported watch addresses — decide).
- [ ] Per-holding `wallets[]`: `id`, `label`, `amount`, `usdValue`, `address?`, `derivationPath?`, `source` (`local`|`watch`|`server`).
- [ ] Compute `total` + 24h change in base currency (USD first; later prefs `baseCurrency`).
- [ ] Empty portfolio for new users (match clean-install product).
- [ ] Emit `balance.update` after convert/transfer/payment/POS settle with full portfolio payload (or invalidate + client refetch — prefer payload app already expects).
- [ ] Optional `?summary=` if product wants lighter Home fetch.
- [ ] Golden test vs fixture shapes; restore rich `portfolio.demo.json` for lab seed only.
- [ ] App: remove optimistic-convert TODO once live balances stream.

**Done when:** staging user with known holdings sees matching Home totals; settle events update Home without full reload.

---

## P2 · Market data — rates, history, ticks

**Why:** Convert, charts, P2/P3 bootstrap. Upstream: CipherBank-src **public** PriceCache — wire format locked by [`PUBLIC_API.md`](./PUBLIC_API.md) / `CB_InitialAPIRef.html`.

| Method | Path / event | App | Priority |
|--------|--------------|-----|----------|
| POST | `/currencies` · `/iquote` · `/quote` | Wired (public host) | P2 |
| GET | `/history?...` | Wired (`useHistory`, P3 OHLC) | P2 |
| WSS | `rate.tick` | Wired (ticker RQ only) | P2 |
| GET | `/rates` · POST `/quotes` | Deprecated shims | — |
| GET | `/assets` | Mock-only (UI uses `assetConfig`) | P2 optional |

### Tasks — rates (public API)

- [x] App client uses SCREAMING_SNAKE `POST /currencies` + `/iquote` (mock + `publicApiClient`).
- [ ] Staging `api.cipherbank.money` against golden examples in `CB_InitialAPIRef.html`.
- [ ] Honor public status codes `406` / `415` / `417` / `422` / `424` on the client.
- [ ] Emit `rate.tick` for held pairs (not only BTC/USD); optional stream still product `/v1`.

### Tasks — history / OHLC

- [ ] Implement `GET /history?range=&granularity=&symbols=&from=&to=` → `{ series[{label,symbol,granularity,points[{t,v,o?,h?,l?,c?}]}], meta }`.
- [ ] Real OHLC feeder (not sine mock) for crypto symbols; define `WALLET` portfolio series policy (§0).
- [ ] Cap series size; reject unbounded ranges.
- [ ] Align with device SQLite `market_ohlc` as cache of this API (no new endpoints).

### Tasks — assets catalog

- [ ] Either: serve `GET /assets` as canonical catalog and migrate app off `assetConfig`, **or** mark endpoint deprecated in contract.
- [ ] Keep securities `enabled:false` until Phase 4+.

**Done when:** Convert shows live rates; Home charts load real OHLC for held symbols; ticks update without polling storms.

---

## P3 · Prefs, profile, account bootstrap

**Why:** Profile settings + returning-user clean install. Local SQLite prefs merge with remote; bootstrap upserts ACH public rows.

| Method | Path | App | Priority |
|--------|------|-----|----------|
| GET | `/prefs` | Wired | P3 |
| PUT | `/prefs` | Wired (debounced) | P3 |
| GET | `/account/bootstrap` | Wired (returning / Home pull) | P3 |
| GET | `/recipients` | Mock-only | P3 (or defer) |
| POST | `/recipients` | Mock-only | P3 (or defer) |

### Prefs shape (persist server-side)

`homeOrder`, `homeVisible`, `valuesHiddenOnLaunch`, `coraEnabled`, `defaultSendSpeed`, `appearance`, `baseCurrency`, `enabledCurrencies`, `localeInferredBase?`, `appLockIdleSec`

### Tasks — prefs

- [ ] User prefs document; GET returns full object; PUT merges partial.
- [ ] Conflict policy: last-write-wins vs version/`updatedAt` (document; app currently local-first).
- [ ] Validate enums (appearance, send speed, currencies ⊆ catalog).
- [ ] Optional Idempotency-Key on PUT (§0).

### Tasks — bootstrap

- [ ] `GET /account/bootstrap` → `{ prefs?, recipients: AchRecipientPublic[], syncedAt }`.
- [ ] Public recipient fields only: names, bankName, accountLast4, accountType, routingNumber, rail, handle, memo, initials — **never** full account number.
- [ ] Align field names with app `bootstrapAccount.ts` (`displayName`, `accountHolderName`, …).
- [ ] Auth required; return empty recipients for brand-new users.
- [ ] Tests: fixture parity with `account-bootstrap.json`.

### Tasks — cloud recipients (if bidirectional)

- [ ] Decide sync model (§0).
- [ ] If cloud is source: implement GET/POST `/recipients` and teach Send to sync; keep full account # on-device only.
- [ ] If pull-only: document `/recipients` as deprecated alias of bootstrap subset.

**Done when:** returning device: SetPin → bootstrap fills contacts; Profile prefs survive reinstall via GET after session.

---

## P4 · Quotes, convert, send, pay, activity

**Why:** Core money movement. Optimistic UI + stream settle.

| Method | Path | App | Priority |
|--------|------|-----|----------|
| POST | `/quotes` | Wired | P4 |
| POST | `/convert` | Wired | P4 |
| POST | `/transfers` | Wired | P4 |
| POST | `/payments` | Wired | P4 |
| GET | `/activity` | Mock-only | P4 |
| GET | `/convert\|transfers\|payments/:id` | Mock stubs | P4 |

### Tasks — quotes & convert

- [ ] `POST /quotes` `{ from, to, amount }` → `{ quoteId, from, to, rate, amountOut, expiresAt, fee }` with real TTL (mock 15s).
- [ ] Persist quote; rate from PriceCache; fee policy per §0.
- [ ] `POST /convert` `{ quoteId, amount }` + Idempotency-Key → `202`/`accepted` `{ txId, status }` then stream `convert.settled`.
- [ ] Reject `quote_expired`, unknown quote, amount mismatch; optional `rate_moved` if re-quote required.
- [ ] Debit/credit portfolio consistently; emit `balance.update`.
- [ ] `GET /convert/:id` status for poll fallback.

### Tasks — transfers (Send)

- [ ] `POST /transfers` `{ recipient, amount, source, speed:'instant'|'ach' }` + Idempotency-Key.
- [ ] Resolve `recipient` as id/handle against cloud and/or accept client-asserted ACH metadata for on-device payees (document).
- [ ] Validate speed, balance, rail; ACH origination may be async/stub until ODFI live.
- [ ] Stream `transfer.settled`; `GET /transfers/:id`.
- [ ] Error codes: `insufficient_funds`, `recipient_unresolved`, etc.

### Tasks — payments (Pay mix)

- [ ] `POST /payments` `{ recipient, total, sources[{asset,value}] }` + Idempotency-Key.
- [ ] Coverage check **with FX** (not raw number compare); return `422 mix_undercovered` when short.
- [ ] Stream `payment.settled` with `breakdown`; `GET /payments/:id`.

### Tasks — activity

- [ ] Persist ledger events; `GET /activity` cursor pagination `{ items[], nextCursor }`.
- [ ] `kind`: convert | transfer | payment | receive; statuses pending/settled/failed.
- [ ] App: Activity screen or Home recent list wired to query key `['activity']` (stream already invalidates).

**Done when:** Convert lock→confirm settles on stream; Send ACH path accepts and settles (or clear pending); Pay mix rejects undercoverage with FX; activity lists txs.

---

## P5 · Wallets & receive

**Why:** XMR hybrid + Receive addresses. Local BIP derive for BTC/ETH stays on device; server wallets for managed/unmanaged/watch.

| Method | Path | App | Priority |
|--------|------|-----|----------|
| GET | `/wallets?symbol=` | Hook unused by screens | P5 |
| GET | `/wallets/:id` | Unused | P5 |
| POST | `/wallets` | Wired (XMR add) | P5 |
| POST | `/wallets/:id/refresh` | Unused | P5 |
| GET | `/receive/:asset` | Wired | P5 |
| POST | `/receive/request` | Mock-only | P5 optional |
| — | `POST /wallets/:id/transfer` | Documented future | Later |

### Tasks — wallets

- [ ] Modes: `managed` | `unmanaged` (address + viewKey once) | `watch` (address).
- [ ] Store view-key fingerprint only after register; never log viewKey.
- [ ] Sync status object on wallet; `POST .../refresh` kicks indexer / MoneroRPC adapter.
- [ ] List/filter by `symbol`; detail by id.
- [ ] Wire CipherBank-src Monero unmanaged sync registration.
- [ ] Threat model doc for managed custody (§0).
- [ ] Future: spend transfer endpoint for managed only.

### Tasks — receive

- [ ] `GET /receive/:asset` returns `{ handle, address, uri, qr }` for **server** wallets; for local-only assets return 404 or documented “client-derived” so app keeps using local address.
- [ ] Cover XMR + any server assets; stop relying on incomplete fixture map.
- [ ] Optional `POST /receive/request` amount URIs if product wants server invoices (§0).

**Done when:** unmanaged XMR register + refresh shows sync; Receive prefers real addresses; portfolio includes server wallet balances.

---

## P6 · Vault (binaries & cards)

**Why:** Profile vault UI + POS card selection. Processor tokens only.

| Method | Path | App | Priority |
|--------|------|-----|----------|
| GET/POST | `/vault/binaries` | Wired | P6 |
| GET/POST | `/vault/cards` | Wired | P6 |
| POST | `/vault/cards/:id/delete` | Wired | P6 |

### Tasks

- [ ] Binaries: metadata refs only (`id`, `label`, `kind`, `status`, `createdAt`) — no key material.
- [ ] Cards: store `brand`, `last4`, `exp*`, `processorToken`, `hardwareTest?`, `label` — reject PAN/CVV.
- [ ] Create card = tokenize via issuer sandbox or mock tokenizer with same response shape.
- [ ] Delete soft-delete + revoke processor token.
- [ ] NFC bench card id support (`EXPO_PUBLIC_HARDWARE_CARD_ID` / `POS_REQUIRE_TEST_CARD`).
- [ ] Idempotency on POSTs.

**Done when:** Profile can list/add/remove cards against staging; POS authorize accepts tokenized `cardId`.

---

## P7 · POS / NFC presentment

**Why:** Tap-to-pay lab → production path. NFC payload is device-local; HTTP owns session lifecycle.

| Method | Path | App | Priority |
|--------|------|-----|----------|
| POST | `/pos/sessions` | Wired | P7 |
| POST | `/pos/authorize` | Wired | P7 |
| POST | `/pos/confirm` | Wired | P7 |
| GET | `/pos/sessions/:id` | Unused | P7 |

### Tasks

- [ ] Session lifecycle: create → `pending_auth` → authorize → `ready_to_present` / presentment TTL → settle.
- [ ] Enforce session expiry (~120s) and presentment TTL (~60s); errors `pos_expired`, `wallet_locked`, `card_not_found`, `test_card_required`, `insufficient_funds`, `mix_undercovered`.
- [ ] Validate `deviceAttestation` (real CDCVM when ready).
- [ ] FX-correct funding check (§0); optional `fundingQuoteId`.
- [ ] Emit **`pos.settled`** `{ sessionId, receiptId, amount, currency }` **and/or** keep `payment.settled` — implement chosen canonical; update app socket.
- [ ] `GET /pos/sessions/:id` for poll/reconnect.
- [ ] Keep NFC NDEF/HCE payload as `{ v:1, sessionId, tokenRef, merchantId? }` only — never PAN.
- [ ] Production follow-ons (separate epic): HCE APDUs, VTS/MDES, iOS Tap to Pay decision.

**Done when:** MockPosScreen against staging completes authorize → simulate/NFC → settle event → portfolio/activity update.

---

## P8 · Banks link & securities (later)

| Method | Path | Notes |
|--------|------|-------|
| POST | `/banks/link` | Plaid-like; returns `{ linked, bankId, last4 }` |
| Securities in `/assets` | Enable when custody/trading partner ready | Pay-with-stock |

### Tasks

- [ ] Bank link provider integration; webhook → refresh linked account metadata.
- [ ] Never store full bank password; tokenize institution connection.
- [ ] Securities catalog + trading APIs — new contract section when greenlit.

---

## Device-local (no API work — enforce boundaries)

These stay on-device; backend must refuse if sent:

| Data | Store |
|------|--------|
| BIP39 ciphertext, device secret, PIN hash | SecureStore |
| Unlocked mnemonic session | Process memory TTL |
| Monero spend key / seed | Device |
| Full ACH `account_number` | SQLite `ach_recipients` |
| Local BIP84/44 drafts | SQLite public meta only |

- [ ] Add automated contract tests that POST forbidden fields → `custody_local_only`.
- [ ] Security review checklist in PR template for any new vault/wallet endpoint.

---

## App cutover checklist (after each phase)

- [ ] Staging URL in `.env.example` (`EXPO_PUBLIC_API_BASE`, `EXPO_PUBLIC_WSS`).
- [ ] Bearer + refresh working.
- [ ] Golden fixture tests still pass against live OpenAPI (or recorded fixtures updated).
- [ ] Smoke: Welcome → Create → empty Home; returning → bootstrap; Convert settle; Send; Receive QR; Profile prefs; POS lab.
- [ ] `pm clear` clean install still works with live empty portfolio.
- [ ] Update `NEXT_PHASE.md` / this file checkboxes.

---

## Suggested team sequencing (calendar view)

| Sprint focus | Domains | Unlocks |
|--------------|---------|---------|
| 1 | C1–C5 + P0 Session | Authenticated staging |
| 2 | P1 Portfolio + P2 Rates/History | Home + Convert display |
| 3 | P3 Prefs + Bootstrap | Clean install product complete |
| 4 | P4 Quotes/Convert/Transfers/Pay/Activity | Core money UX live |
| 5 | P5 Wallets + Receive | XMR + real receive |
| 6 | P6 Vault + P7 POS | Tap-to-pay staging |
| 7+ | P8 Banks/securities + HCE/VTS | Production payments |

---

## Reference index

| Doc | Role |
|-----|------|
| `src/mocks/API_CONTRACT.md` | Canonical HTTP shapes |
| `src/mocks/POS_API.md` | POS session detail |
| `src/mocks/handlers.ts` | Current mock behavior |
| `src/lib/apiClient.ts` / `socket.ts` | Client cutover points |
| `docs/CUSTODY.md` / `PERSISTENCE.md` | Local vs server boundary |
| `docs/MONERO_LINK.md` | CipherBank-src mapping |
| `docs/NEXT_PHASE.md` | Product phase companion |
| `ARCHITECTURE.md` | UI↔backend interaction rules |

---

## Endpoint master checklist

### Auth
- [ ] POST `/session`
- [ ] POST `/session/refresh`

### Data
- [ ] GET `/portfolio`
- [ ] GET `/assets` (or deprecate)
- [ ] GET `/rates`
- [ ] GET `/history`
- [ ] GET `/activity`

### Profile / sync
- [ ] GET `/prefs`
- [ ] PUT `/prefs`
- [ ] GET `/account/bootstrap`
- [ ] GET `/recipients` (optional)
- [ ] POST `/recipients` (optional)
- [ ] POST `/banks/link` (later)

### Money
- [ ] POST `/quotes`
- [ ] POST `/convert` + GET `/convert/:id`
- [ ] POST `/transfers` + GET `/transfers/:id`
- [ ] POST `/payments` + GET `/payments/:id`

### Wallets / receive
- [ ] GET `/wallets`
- [ ] GET `/wallets/:id`
- [ ] POST `/wallets`
- [ ] POST `/wallets/:id/refresh`
- [ ] GET `/receive/:asset`
- [ ] POST `/receive/request` (optional)

### Vault
- [ ] GET/POST `/vault/binaries`
- [ ] GET/POST `/vault/cards`
- [ ] POST `/vault/cards/:id/delete`

### POS
- [ ] POST `/pos/sessions`
- [ ] POST `/pos/authorize`
- [ ] POST `/pos/confirm`
- [ ] GET `/pos/sessions/:id`

### Stream
- [ ] `balance.update`
- [ ] `rate.tick`
- [ ] `convert.settled`
- [ ] `transfer.settled`
- [ ] `payment.settled`
- [ ] `pos.settled`
