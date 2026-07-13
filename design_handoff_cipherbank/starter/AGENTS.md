# AGENTS.md — Cipherbank App Buildout Guide

Guidance for an AI agent (or developer) building the Cipherbank consumer app from this handoff. Read this first, then `ARCHITECTURE.md`, then the live contract in `src/mocks/API_CONTRACT.md` (supersedes the thin `API.md` sketch for shapes).

## What this repo is
A **starter scaffold** for the Cipherbank mobile app: Expo + React Native + TypeScript, React Query for async/server state, React Navigation for the shell. The `starter/` folder is real, runnable-shaped code — providers, navigation, theme, primitives, feature hooks, and screen stubs are wired; the visual detail of each screen is intentionally left to be filled in from the designs.

- **Design source of truth:** `../designs/*.dc.html` (open in a browser) + `../README.md` (per-screen specs) + `../tokens/` + `../assets/`.
- **Interaction contract:** `../ARCHITECTURE.md` (how UI talks to the backend).
- **Server contract:** `src/mocks/API_CONTRACT.md` + `src/mocks/POS_API.md` (canonical `/v1` shapes). Custody / XMR link: `docs/CUSTODY.md`, `docs/MONERO_LINK.md`.
- **Backend build task list:** `docs/API_BUILD_PLAN.md` — phased P0–P8 checklist (session → portfolio → market → prefs/bootstrap → money → wallets → vault → POS).
- **Prototype inventory / persistence:** `docs/PROTOTYPE_MAP.md` · `docs/PERSISTENCE.md` · `docs/USER_CONFIG.md` (base currency, enabled currencies, stale UX).
- **Upstream C++ backend:** CipherBank-src (`MoneroRPC_ExternalAPIAdapter`, PriceCache, HTTP `/quote` · `/iquote` · `/currencies`). The app does **not** call wallet-rpc directly.

### Precedence loading (RAM discipline)

| Level | Trigger | In RAM |
|-------|---------|--------|
| **P0** | NFC / POS / spend unlock | Active wallet meta + custody unlock session |
| **P1** | Chart / Convert / Receive | Requested OHLC window; on-screen rate pair |
| **P2** | Home / cold start | Prefs; wallet **index**; rates for **held** symbols only |
| **P3** | Idle **and** charging | SQLite writer path only (chunked; concurrency ≤2; one job per symbol) |

Secrets stay in SecureStore. Public meta / market snapshots live in `expo-sqlite` (`features/persist`). Bootstrap: `features/bootstrap`.

## Golden rules
1. **Shell renders synchronously; data streams in; actions are optimistic.** Never block a tap on the network. (Full rationale in `ARCHITECTURE.md §1`.)
2. **Tokens only — no magic values.** Colors, spacing, radii, type come from `src/theme` (generated from `../tokens/tokens.json`). If you need a value that isn't a token, add it to the token source, don't inline a hex.
3. **Layered boundaries:**
   - `components/**` are pure/stateless — props in, JSX out, styled from theme. **They never fetch.**
   - `features/*/use*.ts` own **all** async (queries, mutations, streams, optimistic logic).
   - `lib/apiClient.ts` + `lib/queryClient.ts` are the only places that know HTTP/cache mechanics.
4. **Self-custody:** keys are generated **on-device** (`features/vault`, `features/session`) and never sent to the server (except XMR **view key** once for unmanaged sync registration — never spend key / seed / mnemonic).
5. **Idempotency:** every money-moving mutation sends an `Idempotency-Key` so retries are safe.
6. **Modular wallets:** treat each currency’s light-wallet stack as a **plugin**, not a monorepo of one derive path. Users enable currencies locally; the app loads only the modules needed.

## Where to start interfacing with the API
Everything funnels through **`src/lib/apiClient.ts`** (HTTP) and **`src/lib/socket.ts`** (real-time).

**Mock-first (default):** set `EXPO_PUBLIC_USE_MOCK=true` in `.env`. Requests hit `src/mocks/` fixtures/handlers — see `src/mocks/README.md` and `API_CONTRACT.md`. This is the contract lab: shape the JSON here, then make the real API compliant.

**Clean OOTB vs lab seed:** `EXPO_PUBLIC_SEED_DEMO=false` (default) → Welcome create/returning, empty portfolio, no ACH seed. `EXPO_PUBLIC_SEED_DEMO=true` (or legacy `MOCK_HAS_WALLET=true`) → demo custody + rich portfolio + seeded payees. Wipe device data with `adb shell pm clear com.cipherbank.app`.

### Connection map (app `/v1` → expectations)

| App surface | Client entry | Mock / contract | Live CipherBank-src today | As light wallets land |
|-------------|--------------|-----------------|---------------------------|------------------------|
| Portfolio balances | `features/portfolio` | `GET /portfolio` | Not yet (fixtures) | Per-wallet balances from chain / wallet-rpc via gateway |
| Live prices (Convert) | `features/market/ratesCache` | `GET /rates` (+ `ttlMs`) | PriceCache → `/quote` · `/iquote` | Same; alias `XMR`↔`MONERO` |
| Locked quote | `features/quotes` | `POST /quotes` | PriceCache quotes | Keep short TTL; prefer rates cache for display |
| Bulk history / charts | `features/history` | `GET /history?range&granularity&symbols&from&to` | **Absent** (needs OHLC feeder) | Bulk blocks only — not tick-by-tick |
| BTC/ETH local derive | `features/wallets/derive` | local SecureStore + SQLite wallet index | N/A (client-side) | Extend module registry; server may later index public addresses |
| XMR hybrid wallets | `features/wallets/xmr.*` | `GET/POST /wallets`, `…/refresh` | Adapter exists **internal only** | Gateway wraps `create_wallet` / `generate_from_keys` / `get_balance` |
| Receive | `features/receive` | `GET /receive/:asset` | — | Prefer wallet `address` from drafts / `/wallets` over fixtures |
| Vault (non-seed) | `features/vault` | `/vault/binaries`, `/vault/cards` | Server vault | Still never seed/PAN |
| POS | `features/pos` | `/pos/*` | — | See `POS_API.md` |
| Stream | `lib/socket` | `rate.tick`, `*.settled` | — | Feed live rates + settlement |

To go from mocked to live:

1. **Set env:** `EXPO_PUBLIC_USE_MOCK=false`, point `EXPO_PUBLIC_API_BASE` / `EXPO_PUBLIC_WSS` at your server.
2. **Wire auth:** fill the `Authorization` header in `apiClient.ts` from `features/session` (token after wallet unlock).
3. **First real call — Portfolio** (loading pattern), then **Quotes + Convert**, then wallets / receive / send.

Each feature is independent — build vertically (one endpoint → its hook → its screen) rather than all endpoints at once.

## Modular light wallets (per currency)

**Idea:** the device does not ship one mega-wallet. It ships a **wallet module registry**. When the user adds a currency locally (Add wallet / onboarding), the app activates that module’s derive, sync, and UI paths.

```
features/wallets/
  registry.ts          # symbol → WalletModule
  derive.ts            # BTC BIP84, ETH BIP44 (active)
  xmr.api.ts / xmr.types.ts   # XMR product API (active)
  localWallets.ts      # public metadata only (SQLite via persist/)
  <coin>/…             # future: LTC, DOGE, etc.
```

### `WalletModule` contract (extend as currencies are added)

| Capability | BTC | ETH | XMR | Future (LTC/DOGE/…) |
|------------|-----|-----|-----|---------------------|
| Seed / key model | BIP39 shared custody blob | same | Separate Monero seed / spend on device for unmanaged | Coin-specific or shared BIP44 coin type |
| Local derive | BIP84 `m/84'/0'/0'/0/i` | BIP44 `m/44'/60'/0'/0/i` | Not BIP; address + keys | BIP44/84 coin path or native |
| Add-wallet UI modes | Derive next · Watch | Derive next · Watch | **Managed · Unmanaged (view key) · Watch** | Start Watch-only; add derive when module lands |
| Server sync | Optional indexer later | Optional indexer | `/wallets` → wallet-rpc view or managed | Same pattern once gateway exposes coin |
| What leaves device | Public address + path only | same | View key (unmanaged register) **or** nothing (managed) | Never spend key / seed |
| Spend / sign | Out of scope until later | same | Out of scope (client sign unmanaged later) | Per-module |

**Expectations when adding a currency type:**

1. Register a module in `features/wallets/registry.ts` (`symbol`, `modes`, `canDerive`, `sourceFor`, `usesServerWallets`).
2. Extend Add-wallet UI via mode chips from the module (prefer `getWalletModule(symbol).addModes` over hardcoding coins).
3. Persist only **public** metadata in SQLite (`address`, `path`, `mode`, `source`, sync fingerprint) via `features/persist`.
4. Mock `/wallets` or derive fixtures first; document link map in `docs/` (pattern: `MONERO_LINK.md`).
5. Prefer module addresses over portfolio fixtures when merging (`mergeLocalWallets`).
6. Convert/Home rates: ensure symbol exists in `GET /rates` + `assetConfig`.

**Do not** fold Monero into BIP39 BTC derivation. **Do not** call `crypto.sandbox…/monero/json_rpc` from the mobile app — always CipherBank `/v1`.

## Build order (matches README §Suggested Build Order)
1. Foundations — fonts, `theme`, `assets`, primitives (`Button`, `Card`, `Skeleton`, `Toast`).
2. **Home** — `usePortfolio` + `BalanceHero` + AssetList (build `AssetList` + `AssetGlyph` + its `.Skeleton`).
3. **Convert** — `useQuoteLock` + `useConvert` + `useRatesCache` + amount cards + `RateLockStrip`.
4. **Send** + **Receive**.
5. **Pay-with-a-mix** — `usePayMix` + `FundingMixBar` (coverage must reach 100% before confirm).
6. **Onboarding** — custody (`features/vault`: BIP39, PIN, quiz) + session.
7. **Light wallets** — BTC/ETH derive; XMR hybrid modules; registry for the next coin the user enables.
8. **Securities** — extend `AssetType` and the asset model; unlock "pay/invest with anything."

## Filling in a screen (repeatable recipe)
1. Open the matching `designs/Cipherbank App.dc.html` screen + its `README.md` §Screens entry for exact layout, colors, copy, and Cora line.
2. Compose existing `components/**`; build any missing presentational component in the right subfolder (`money/`, `chrome/`, …) — pure, token-styled, with a `.Skeleton` if it shows async data.
3. Get data/actions from the feature hook (`use*`), never fetch in the screen.
4. Cover every state from `ARCHITECTURE.md §7`: loading · empty · error · offline · success (+ screen-specific: rate-expired, insufficient, under-funded).

## Conventions
- **Money math:** amounts are strings end-to-end (bignumber-safe); format only at the edge via `lib/money.ts`. Never do float math on balances.
- **Cora:** one line per screen, dry and short. Her artwork is a PNG slot on `CoraBar` — the client supplies transparent cutouts (not in repo).
- **Icons/logos:** SVGs in `../assets/` (`react-native-svg`). Swap the placeholder squares in `TabBar` for `ui-*.svg`.
- **Do not** port anything from `designs/*.dc.html` runtime (`support.js`, `ios-frame.jsx`, etc.) — those render the *references* only.
- **Typecheck** before done: `npm run typecheck`.

## Definition of done for a feature
- [ ] Endpoint implemented per `API_CONTRACT.md` (request/response shapes match).
- [ ] Feature hook: query or optimistic mutation with rollback + idempotency key.
- [ ] Screen composes pure components, styled from tokens, pixel-close to the design.
- [ ] All async states handled (skeleton/empty/error/offline/success).
- [ ] Real-time reconcile wired where relevant (`socket.ts`).
- [ ] If a new currency: wallet module registered + mock/link doc + Add-wallet modes.
- [ ] `npm run typecheck` clean.
