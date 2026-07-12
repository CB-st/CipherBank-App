# Next phase — real wallets, storage security, and backend cutover

Companion to the CoraDesignOverhaul PR. Phase 1 (this PR) ships a **mock-first Expo app** that matches the design handoff and `/v1` contract. Phase 2 hardens custody, derives real addresses, and swaps mocks for live APIs.

## Phase 1 (done) — what we ship

| Area | Status |
|------|--------|
| Design tokens, assets, HTML designs | In `design_handoff_cipherbank/` |
| Expo app (Home, Convert, Send, Pay, Receive, Profile, Onboarding) | Mock-first UI |
| Dark-default theme + light toggle | Prefs `appearance` |
| Multi-wallet-per-crypto + local draft slots | Portfolio + AsyncStorage |
| Hybrid vault UI (local mnemonic flag + server binaries/cards) | SecureStore stub + mock APIs |
| POS / NFC lab + EMV-shaped simulate exchange | Mock POS + Android NDEF |
| Android emulator setup + APK scripts | `android:setup`, `android:apk` |
| `/v1` + POS JSON contracts | `API_CONTRACT.md`, `POS_API.md` |

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
Sprint A  SecureStore + real BIP39 + PIN/biometrics gate + backup quiz
Sprint B  BTC/ETH derivation + receive/send by walletId + portfolio read adapter
Sprint C  Staging API cutover (session, portfolio, prefs) + stream rates
Sprint D  POS: HCE spike on Android device + processor sandbox token
```

## Doc map (keep updated)

| Doc | Role |
|-----|------|
| `ARCHITECTURE.md` | Shell-first / async UI↔backend principles |
| `starter/src/mocks/API_CONTRACT.md` | Full `/v1` JSON shapes |
| `starter/src/mocks/POS_API.md` | Tap-to-pay authorize/present |
| `starter/docs/DIGITAL_CARDS_NFC.md` | Visa VTS / MDES mapping |
| `starter/docs/ANDROID_SETUP.md` | Emulator + APK scripts |
| `starter/docs/TESTING.md` | Web / Android / EAS matrix |
| This file | Phase roadmap + storage security |
