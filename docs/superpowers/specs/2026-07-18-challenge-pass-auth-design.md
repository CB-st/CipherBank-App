# Challenge / Pass Session Auth (A1) — Design

**Date:** 2026-07-18  
**Updated:** 2026-07-19 — installed as pluggable module `CipherBank-app.ChallengePass`  
**Status:** Approved; module slotted (algo / template / structure)  
**Branch:** `feat/cora-maui-port`  
**Related:** `docs/superpowers/plans/2026-07-18-challenge-pass-auth.md`, `docs/superpowers/plans/2026-07-18-cora-maui-parity.md` (F6)

---

## Problem

Today, after local custody unlock, MAUI opens a product session with `{ DEVICE_ATTESTATION: "lab" }` and stores Bearer tokens. Session identity is not cryptographically bound to the account key. We will move to a **sealed challenge / pass** gate: the API encrypts a test that only the registered account private key can open; the device seals the solved pass to the API public key.

## Goals

1. Bind session open to possession of the on-device account private key (derived from custody; never transmitted).
2. Keep mnemonic / seed / PIN off the wire at all times.
3. Preserve short-lived Bearer tokens after a successful pass (refresh path unchanged) until a later cutover retires them.
4. Ship client crypto + mock round-trip behind a feature flag; live API verify can follow without rewriting unlock orchestration.
5. **Slot-in / slot-out** algorithm, challenge template, and protocol structure independently via `CipherBank-app.ChallengePass`.
6. **A2 hybrid PQ channel:** ML-KEM-768 + X25519 key-share establishes a 32-byte channel key; challenge/pass uses ChaCha20-Poly1305 with that key.
7. Complete Phase F6 parity hardening in the same delivery wave (XMR managed, E2E, PR checklist) without blocking on live challenge endpoints.

## Non-goals

- Retiring Bearer tokens in this wave.
- Replacing POS CDCVM with full challenge/pass (optional later: attestation string parity only).
- Storing spend keys for managed XMR.
- Cloud backup of seed / HCE presentment scope creep.

## Decision summary

| Decision | Choice |
|----------|--------|
| Proof shape | **A — Sealed challenge/pass** |
| Crypto suite (default) | **A1 — X25519 + HKDF-SHA256 + ChaCha20-Poly1305** |
| Module | **`CipherBank-app.ChallengePass`** (separate assembly) |
| Account key | HKDF from BIP39 entropy → X25519; pubkey registered at account open |
| Session artifact after pass | Keep `ACCESS_TOKEN` / `REFRESH_TOKEN` (short TTL) |
| Default mode | `LabSessionProofBuilder` until API + custody `IAccountKeySource` are live |
| Library | **NSec.Cryptography** in ChallengePass project |

---

## Module architecture (slot-in / slot-out)

```
CipherBank-app.ChallengePass
├── ISealAlgorithm          ← Slot 1: crypto (seal/open + keypair from seed)
├── IChallengeTemplate      ← Slot 2: plaintext framing + pass payload
├── IChallengePassStructure ← Slot 3: HTTP/choreography (challenge → pass body)
├── ChallengePassSuite      ← named composition of the three slots
└── IChallengePassCatalog   ← registry; SetActive(suiteId) swaps suites at runtime
```

| Slot | Default A1 implementation | Id |
|------|---------------------------|-----|
| Algorithm | `X25519ChaChaSealAlgorithm` | `x25519-chacha20poly1305` |
| Template | `ChallengeIdNonceSha256Template` | `challenge-id-null-nonce-sha256-v1` |
| Structure | `TwoStepChallengePassStructure` | `two-step-challenge-pass-v1` |
| Suite | composition | `a1-x25519-chacha-v1` |
| Suite A2 | hybrid key-share → channel AEAD | `a2-hybrid-pq-channel-v1` |

**A2 flow**

1. Device identity: X25519 + ML-KEM-768 from custody entropy (`account/hybrid/v1`).
2. Key share: server encaps + ephemeral X25519 → `channel_key = HKDF(ss_kem ‖ ss_x)`.
3. Challenge/pass: ChaCha20-Poly1305 with `channel_key` only (`pq-channel-chacha20poly1305-v1`).

Portable ML-KEM via BouncyCastle (OS `MLKem.IsSupported` may be false on Android/Linux without OpenSSL 3.5).

**Swap examples**

- New AEAD/KEM → implement `ISealAlgorithm`, register `AddChallengePassSuite("a2-…", …)`.
- New framing (e.g. include `DEVICE_ID`) → implement `IChallengeTemplate`, keep same algo/structure.
- One-shot session POST → implement `IChallengePassStructure`, keep same algo/template.

DI: `services.AddChallengePassModule()` installs A1. App keeps `ISessionProofBuilder → LabSessionProofBuilder` until cutover; then bind `ChallengePassSessionProofBuilder`.

Ports (not slots, but swappable adapters):

- `ISessionChallengeClient` — fetch challenge (in-memory for lab/tests; HTTP later)
- `IAccountKeySource` — unlocked account keypair from custody (`LockedAccountKeySource` placeholder today)

---

## Protocol

### Account key derivation (device)

When custody is unlocked (mnemonic in memory):

1. Decode BIP39 mnemonic → entropy (existing NBitcoin path).
2. `AccountKeyDerivation.DeriveAccountKey(algorithm, entropy)` → HKDF salt `CipherBank`, info `account/x25519/v1`, L=32 → seal-slot `DeriveKeyPair`.
3. Persist only **public key** wire form (`WireEncoding`); never persist X25519 private key.

### Account open (once)

```
POST /v1/account/keys
Body: { ACCOUNT_PUBLIC_KEY, DEVICE_ID?, ALGORITHM }
```

### Session open (each unlock) — structure slot `two-step-challenge-pass-v1`

```
1. POST /v1/session/challenge  { ACCOUNT_PUBLIC_KEY }
   ← SessionChallengeDto (CIPHERTEXT sealed to account pubkey)

2. Open CIPHERTEXT → template parses P = UTF8(CHALLENGE_ID) || 0x00 || NONCE
3. PASS_PAYLOAD = SHA256(P); seal to API_PUBLIC_KEY
4. POST /v1/session ← SessionPassDto → SessionDto tokens
```

**Lab mode:** `{ DEVICE_ATTESTATION: "lab" }` via `LabSessionProofBuilder` (unchanged).

### Sealed-box (algorithm slot)

Ephemeral X25519 + HKDF(`CipherBank-seal-v1` / `seal/chacha20poly1305/v1`) + ChaCha20-Poly1305.  
Wire blob: `ephemeral_pk(32) || nonce(12) || ciphertext+tag`.

---

## Phase F6 (same delivery wave)

Unchanged: XMR managed API, E2E AutomationIds, PR #15 checklist.

---

## Rollout

1. Module + A1 slots + unit tests (catalog swap, seal round-trip, pass body).  
2. Custody `IAccountKeySource` + HTTP `ISessionChallengeClient`.  
3. Settings flag → bind `ChallengePassSessionProofBuilder`.  
4. API challenge issue / pass verify.  
5. Retire lab attestation.

## Success criteria

- [x] Separate `CipherBank-app.ChallengePass` assembly with three slots + catalog.  
- [x] A1 sealed round-trip + in-memory challenge→pass without seed fields.  
- [x] Custody-backed key source + HTTP challenge client.  
- [ ] F6.1–F6.3 done; PR #15 updated.  
- [x] Lab remains default opener until flag flip.  
- [x] F6.1 Managed XMR wallet create (no spend key stored).  
- [x] F6.2 E2E AutomationIds + parity smoke.  
- [ ] F6.3 PR checklist.
