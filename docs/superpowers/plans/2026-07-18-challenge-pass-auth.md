// <copyright file="2026-07-18-challenge-pass-auth.md" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

# Challenge / pass session auth (direction)

**Status:** Scaffolded (`ISessionProofBuilder` + `LabSessionProofBuilder`). Live path still posts `{ DEVICE_ATTESTATION: "lab" }` then stores Bearer tokens.

**Product intent:** Move from long-lived session tokens as the *proof of identity* to a **challenge / pass** gate bound to the account key registered at open.

## Today (MAUI)

```
Unlock custody (PIN / biometrics)
  → POST /v1/session { DEVICE_ATTESTATION: "lab" }
  → ACCESS_TOKEN + REFRESH_TOKEN in SecureStorage
  → AuthHeaderHandler adds Authorization: Bearer …
```

Custody mnemonic stays on-device. Session creation is **not** cryptographically bound to the account key yet — only gated by “local unlock succeeded.”

## Target

1. **Account open:** client registers **account public key** (derived from custody; never send seed/mnemonic/PIN).
2. **Session open (or continuous gate):**
   - API issues an encrypted **challenge** (`SessionChallengeDto`) that only the matching **private key** can unscramble.
   - Device decrypts, solves the test, seals the **pass** to the **API public key** (`SessionPassDto`).
   - API verifies pass → issues short-lived access (or accepts pass as the auth artifact).
3. **Bearer tokens** become optional cache of a successful pass (short TTL + refresh), or are retired in favor of pass-bound tickets — product decision at cutover.

## Plug-in points (already identified)

| Layer | File / type | Change |
|-------|-------------|--------|
| Proof body | `ISessionProofBuilder` | Replace `LabSessionProofBuilder` with challenge decrypt + pass seal |
| HTTP | `HttpProductApi.CreateSessionAsync` | Already consumes builder; add `GET/POST challenge` if two-step |
| Unlock | `AppSession.CompleteUnlockAsync` | Custody unlocked → proof builder can use in-memory key material |
| Headers | `AuthHeaderHandler` | Keep Bearer until tokens retired; then swap to pass ticket header |
| Onboarding | SetPin / FinishCustodySetup | Register account public key once |

## Non-negotiables

- Mnemonic / seed / PIN never on the wire.
- Challenge ciphertext and pass ciphertext only — no plaintext private material.
- Lab stub remains for mocks and local Android until API challenge endpoints ship.

## Suggested cutover

1. API: challenge issue + verify + account pubkey registry.
2. MAUI: `ChallengePassSessionProofBuilder` (custody-derived key, X25519/ChaCha or agreed suite).
3. Feature flag / settings: `SessionProofMode = Lab | ChallengePass`.
4. Retire `DEVICE_ATTESTATION=lab` on staging, then production.
