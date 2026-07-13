# Device persistence & precedence loading

Device-first SQLite for public user environment. Secrets stay in SecureStore. Nothing bulk-loads into RAM at cold start.

See also [`PROTOTYPE_MAP.md`](./PROTOTYPE_MAP.md).

## Stores

| Store | Contents |
|-------|----------|
| SecureStore | BIP39 ciphertext, device secret, PIN hash, wallet-present flag |
| expo-sqlite `cipherbank.db` | wallets, prefs, rates_snapshot, market_ohlc, sync_meta, **ach_recipients** |
| React Query | Short-lived network cache; hydrated from SQLite for P2/P1 only |
| Process memory | Unlocked mnemonic (TTL / background clear) |

**Never** put seed, spend key, or PIN plaintext in SQLite.

## Schema (logical)

- `wallets(id, symbol, label, address, derivation_path, account_index, source, mode, sync_json, view_key_fp, created_at)`
- `prefs(key PRIMARY, value_json)`
- `rates_snapshot(symbol PRIMARY, usd, change24h, updated_at)`
- `market_ohlc(symbol, granularity, t, o, h, l, c, v, PRIMARY KEY(symbol, granularity, t))`
- `sync_meta(key PRIMARY, value, updated_at)`
- `ach_recipients(...)` — on-device ACH payees for Send
- `sync_meta` also holds setup flags: `setup_path` (`new`|`returning`), `setup_complete` (`0`|`1`), `account_bootstrap_at`

## Clean install vs lab seed

| Flag | Effect |
|------|--------|
| `EXPO_PUBLIC_SEED_DEMO=true` (or legacy `MOCK_HAS_WALLET=true`) | Demo custody PIN `000000`, seeded ACH payees, rich `portfolio.demo.json` |
| Both false (default OOTB) | Welcome → create/returning; empty portfolio; no ACH seed until setup or `GET /account/bootstrap` |

Wipe a device lab install: `adb shell pm clear com.cipherbank.app` then relaunch with Metro.

## Precedence (what may enter RAM)

| Level | Trigger | Load | Do not load |
|-------|---------|------|-------------|
| **P0** | NFC / POS / spend unlock | Active wallet meta; custody unlock; active card id | OHLC; all-coin rates |
| **P1** | Chart / Convert / Receive | Requested OHLC window; on-screen rate pair | Other symbols’ history |
| **P2** | Home / tabs (cold start) | Prefs; wallet **index**; rates for **held** symbols | Full OHLC dump |
| **P3** | Idle **and** charging | Write into SQLite only (chunked) | UI-forcing large arrays |

**Concurrency:** one in-flight job per `symbol`; global cap **2**. Never refresh every coin wallet in parallel.

## Activation contexts

`shell` | `chart` | `convert` | `nfc_pos` | `background`

Implemented in `src/features/bootstrap/activation.ts`. Screens call `setActivation(...)` so the job queue can prioritize.

## Background (P3)

- Watch `AppState` + `expo-battery` charge state.
- Quiet period: no P0/P1 for ~3 minutes → eligible for P3 if charging.
- Jobs: refresh rates snapshot; fill missing OHLC for held symbols one-at-a-time.
- **Limits:** best-effort while the process is alive. Not a true OS daemon; future `expo-task-manager` optional. Pause P3 when user becomes active or unplugged.

## Bootstrap sequence (cold start)

1. Open SQLite; migrate schema; one-time AsyncStorage → SQLite for wallets/prefs.
2. P2: load wallet index + prefs + held rates into RQ `initialData` (small).
3. Start background watcher (does not run P3 until idle+charging).
4. Chart interaction triggers P1 history for held symbols only.
