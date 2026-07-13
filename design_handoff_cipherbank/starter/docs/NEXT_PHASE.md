# Next phase — real wallets, storage security, and backend cutover

Companion to the CoraDesignOverhaul PR. Phase 1 (this PR) ships a **mock-first Expo app** that matches the design handoff and `/v1` contract. Phase 2 hardens custody, derives real addresses, and swaps mocks for live APIs.

## Phase 1 (done) — what we ship

| Area | Status |
|------|--------|
| Design tokens, assets, HTML designs | In `design_handoff_cipherbank/` |
| Expo app (Home, Convert, Send, Pay, Receive, Profile, Onboarding) | Mock-first UI |
| Dark-default theme + light toggle | Prefs `appearance` |
| Multi-wallet-per-crypto + local draft slots | Portfolio + SQLite (`features/persist`) |
| Hybrid vault UI (local mnemonic flag + server binaries/cards) | SecureStore stub + mock APIs |
| POS / NFC lab + EMV-shaped simulate exchange | Mock POS + Android NDEF |
| Android emulator setup + APK scripts | `android:setup`, `android:apk` |
| `/v1` + POS JSON contracts | `API_CONTRACT.md`, `POS_API.md` |

## Backend API workstream

Full endpoint-by-endpoint task lists (session, portfolio, market, prefs/bootstrap, money movement, wallets, vault, POS): [`API_BUILD_PLAN.md`](./API_BUILD_PLAN.md).

## Phase 2 — storage security & wallet setup (priority)

### 2.1 Local custody (device)

1. **Replace mock BIP39** with real entropy (`expo-crypto` / audited BIP39 lib); never log the phrase.
2. **SecureStore hardening** — `REQUIRE_AUTHENTICATION` / Keystore-backed keys on Android; Keychain accessibility on iOS; wipe on uninstall policy documented.
3. **PIN / passcode fallback** when biometrics unavailable; lockout + attempt limits.
4. **Encrypted mnemonic blob** at rest (AES-GCM with key in TEE/StrongBox when available); app memory zeroization after unlock window.
5. **Backup UX** — forced write-down / verify quiz before funding; optional encrypted cloud backup (separate product decision — off by default).
6. **Device binding** — rotate `cb_device_signing_key`; attest unlock for POS (`deviceAttestation` → real CDCVM).

### 2.2 Derivation & multi-wallet (read path)

1. BIP84/BIP44 (and coin-specific) derivation from mnemonic → addresses for BTC/ETH/LTC/DOGE/XMR (Monero may need separate stack).
2. Fill empty “Add wallet” slots with derived addresses + paths; persist only **public** metadata locally.
3. **Watch-only** import: validate address checksum; no spend path.
4. **Balance read** — chain indexers / CipherBank `/portfolio` once backend returns per-`walletId` balances.
5. Receive screen already selects wallet; wire QR/URI to derived address (drop fixture-only addresses).

### 2.3 Server hybrid vault (never sees seed)

1. Keep rule: **mnemonic/PAN/CVV never leave device**.
2. Server holds wallet **binary refs** / shards only if product requires hybrid recovery — document threat model in `ARCHITECTURE.md`.
3. Card vault: processor tokens only; integrate VTS / MDES push-provisioning when issuer partnership lands (`DIGITAL_CARDS_NFC.md`).

### 2.4 Session & API cutover

1. Live `POST /session` + refresh; drop mock tokens.
2. `EXPO_PUBLIC_USE_MOCK=false` against staging that implements `API_CONTRACT.md`.
3. WebSocket `/stream` for rates + settlement; keep optimistic UI.
4. Idempotency keys on convert/transfer/pay/pos in production clients.

## Phase 3 — payments & NFC production

1. Android **HCE** `HostApduService` (replace NDEF lab payload).
2. Visa VTS / Mastercard MDES token requestor or issuer connector.
3. Ephemeral cryptogram TTL enforcement; POS settle reconciliation.
4. iOS path decision: Apple Tap to Pay / Wallet vs Android-first only.

## Phase 4 — product polish

1. Real bank link (`POST /banks/link`).
2. Securities / “pay with stock” behind feature flag.
3. EAS production signing, store listings, crash/analytics (privacy-preserving).
4. Threat model review + pen-test before mainnet funds.

## Suggested sequencing (next 2–4 sprints)

```
Sprint A  SecureStore + real BIP39 + PIN/biometrics gate + backup quiz   ✅ in progress / this PR
Sprint B  BTC/ETH derivation + receive/send by walletId + portfolio read adapter  ✅ address derive (balances still mock)
Sprint C  Staging API cutover (session, portfolio, prefs) + stream rates
Sprint D  POS: HCE spike on Android device + processor sandbox token
```

### Sprint A/B checklist (Phase 2 slice)

- [x] Real BIP39 12-word generate (`@scure/bip39` + `expo-crypto` entropy)
- [x] SecureStore hardening (`WHEN_UNLOCKED_THIS_DEVICE_ONLY`) + AES-GCM blob `cb_custody_v2`
- [x] PIN hash + lockout; biometrics gate; in-memory unlock session
- [x] Backup write-down + 3-word verify quiz → Set PIN
- [x] BTC BIP84 + ETH BIP44 derive; `ensureDerivedWallets`; Add-wallet “Derive next”
- [x] Receive / Home prefer derived addresses; `docs/CUSTODY.md`
- [x] XMR hybrid wallets contract + mocks (managed / unmanaged / watch); `docs/MONERO_LINK.md`
- [x] Bulk `/history` (granularity + symbols + from/to); live `/rates` cache for Convert
- [x] Device SQLite + P0–P3 bootstrap (`docs/PERSISTENCE.md`); prototype map (`docs/PROTOTYPE_MAP.md`)
- [x] User config: base currency (locale-aware USD/BTC/EUR/JPY), enabled currencies, Other assets — `docs/USER_CONFIG.md`
- [ ] Sprint C: `EXPO_PUBLIC_USE_MOCK=false` staging cutover
- [ ] Sprint D: HCE / VTS-MDES
- [ ] CipherBank-src: expose `/v1/wallets*` over HTTP + history OHLC feeder
- [ ] Optional: OS background fetch via `expo-task-manager` (beyond in-process idle+charging)

### Local DB / bootstrap checklist

- [x] `expo-sqlite` schema: wallets, prefs, rates_snapshot, market_ohlc, sync_meta
- [x] Migrate `cb_local_wallets_v1` / `cb_user_prefs_v1` from AsyncStorage → SQLite
- [x] Cold start runs **P2 only** (wallet index + prefs + held rates)
- [x] Chart / Convert declare P1; POS declares P0
- [x] JobQueue: single-flight per symbol; global concurrency 2
- [x] P3 only when idle + charging; pause when active/unplugged

## Doc map (keep updated)

| Doc | Role |
|-----|------|
| `ARCHITECTURE.md` | Shell-first / async UI↔backend principles |
| `starter/src/mocks/API_CONTRACT.md` | Full `/v1` JSON shapes |
| `starter/src/mocks/POS_API.md` | Tap-to-pay authorize/present |
| `starter/docs/DIGITAL_CARDS_NFC.md` | Visa VTS / MDES mapping |
| `starter/docs/ANDROID_SETUP.md` | Emulator + APK scripts |
| `starter/docs/TESTING.md` | Web / Android / EAS matrix |
| `starter/docs/CUSTODY.md` | On-device custody threat model |
| `starter/docs/MONERO_LINK.md` | XMR hybrid wallets ↔ MoneroRPC / PriceCache |
| `starter/docs/PROTOTYPE_MAP.md` | Prototype inventory + placement + build-out |
| `starter/docs/PERSISTENCE.md` | SQLite schema + precedence + charging policy |
| `starter/docs/USER_CONFIG.md` | Base currency, enabled currencies, wallet manifest |
| This file | Phase roadmap + storage security |
