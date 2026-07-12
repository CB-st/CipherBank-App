# On-device custody — threat model

CipherBank Digital Teller keeps **self-custody keys on the device**. Server vault APIs hold binaries and card processor tokens only — never seed material, PIN, or raw card PAN/CVV.

## What is stored where

| Data | Location | Notes |
|------|----------|--------|
| BIP39 mnemonic (ciphertext) | SecureStore `cb_custody_v2` | AES-256-GCM; `{ ciphertext, iv, salt, version, kdf }` |
| Device secret (AES key material) | SecureStore `cb_device_secret_v2` | Random 32-byte secret; PBKDF2 → AES key with blob salt |
| PIN | SecureStore `cb_pin_meta_v1` | Salted SHA-256 hash only; never plaintext |
| Unlock session mnemonic | Process memory only | Cleared on background, explicit lock, or ~5 min TTL |
| Wallet metadata (label, path, address, source, mode) | AsyncStorage `cb_local_wallets_v1` | Public data only — safe to back up / inspect |
| Server binaries / card tokens | CipherBank API | Hybrid vault; unrelated to seed |

**Web fallback:** if SecureStore is unavailable, secrets may land in AsyncStorage under `cb_secure_web:*`. That path is **not production-grade** — treat web as demo only.

## Unlock model

1. Biometrics (when hardware + enrollment exist), else PIN.
2. Gate success → decrypt blob with device-secret-derived key → short-lived `sessionMnemonic`.
3. Derivation (BIP84 BTC / BIP44 ETH) runs only while session is live.
4. Lockout: 5 failed PIN attempts → exponential backoff stored with PIN meta.

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
