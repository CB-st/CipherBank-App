# On-device custody — threat model

CipherBank Digital Teller keeps **self-custody keys on the device**. Server vault APIs hold binaries and card processor tokens only — never seed material, PIN, or raw card PAN/CVV.

## What is stored where

| Data | Location | Notes |
|------|----------|--------|
| BIP39 mnemonic (ciphertext) | SecureStore `cb_custody_v2` | AES-256-GCM; `{ ciphertext, iv, salt, version, kdf }` |
| Device secret (AES key material) | SecureStore `cb_device_secret_v2` | Random 32-byte secret; PBKDF2 → AES key with blob salt |
| PIN | SecureStore `cb_pin_meta_v1` | Salted SHA-256 hash only; never plaintext |
| Unlock session mnemonic | Process memory only | Cleared on **background**, explicit lock, or ~5 min TTL. `inactive` (biometric sheets) does not clear. |
| Wallet metadata (label, path, address, source, mode) | AsyncStorage `cb_local_wallets_v1` | Public data only — safe to back up / inspect |
| Server binaries / card tokens | CipherBank API | Hybrid vault; unrelated to seed |

### Emulator / mock notes

- Hermes needs `crypto.getRandomValues` + `TextDecoder` polyfills (`index.js`).
- Mock builds seal with a lighter PBKDF2 cost and store secrets in an AsyncStorage mirror (SecureStore round-trips on AVDs were unreliable).
- Demo CipherBank PIN after heal: `000000` (fallback only — prefer Android fingerprint / device PIN).

**Web fallback:** if SecureStore is unavailable, secrets may land in AsyncStorage under `cb_secure_web:*`. That path is **not production-grade** — treat web as demo only.

## Unlock model

1. **App shell lock** — after onboarding, `RootNavigator` shows `UnlockScreen` until `session.unlocked`. Idle timeout (default 60s, Profile-configurable) and **`background` AppState** lock the shell and clear the mnemonic session. `inactive` is ignored (biometric / device-PIN sheets would otherwise re-lock mid-auth). An `authInProgress` counter also skips custody clear during OS prompts.
2. **OS unlock first** — `LocalAuthentication.authenticateAsync` with device-credential fallback. Fingerprint/face when enrolled; otherwise Android’s built-in PIN/pattern/password keypad. CipherBank’s in-app 6-digit PIN is last-resort (web / no device lock / explicit fallback).
3. Gate success → decrypt blob with device-secret-derived key → short-lived `sessionMnemonic` (~5 min TTL, cleared on lock).
4. **Step-up (`requireAuth`)** — always re-prompt for:
   - Payment / convert confirm
   - POS authorize
   - POS presentment (token handoff — anti-skimming)
   - Reveal / export recovery phrase
5. Lockout: 5 failed CipherBank PIN attempts → exponential backoff stored with PIN meta.

See also `features/vault/requireAuth.ts`.

## Derivation (Sprint B)

- BTC: `m/84'/0'/0'/0/{i}` → native segwit `bc1…`
- ETH: `m/44'/60'/0'/0/{i}` → EIP-55 `0x…`
- Primary accounts (`i=0`) are ensured after seal / mock bootstrap.
- “Derive next” increments `accountIndex` for that symbol; watch-only wallets are paste-only (no spend).

## Monero (hybrid light wallets)

Monero is **not** BIP84/BIP44. Modes (see [`MONERO_LINK.md`](./MONERO_LINK.md)):

| Mode | Spend | Server |
|------|-------|--------|
| managed | Server wallet-rpc | Full custody account |
| unmanaged | Device only | View-key sync / balance |
| watch | None | Address metadata |

Server may receive **viewKey + address** for unmanaged sync registration — never spend key or seed.

## Explicit non-goals (this phase)

- Cloud seed backup
- Spending / transaction signing (incl. XMR transfer)
- Live chain balance indexers (mock `/portfolio` until Sprint C)
- Android HCE / VTS-MDES (Sprint D)
- Embedding monero-wallet-rpc / LWS in the mobile binary

## Invariants

1. Mnemonic never in logs, toasts, analytics, or API bodies.
2. PIN never stored plaintext.
3. Server never receives seed, PIN, or Monero spend key.
4. Mock demo may bootstrap with PIN `000000` when `EXPO_PUBLIC_MOCK_HAS_WALLET=true` — not for real funds.
