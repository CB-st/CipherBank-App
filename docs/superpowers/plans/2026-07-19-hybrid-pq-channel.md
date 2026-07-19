# Hybrid PQ Key-Share + Channel Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add suite **A2** that first runs a hybrid ML-KEM-768 + X25519 key share to establish a PQ-derived 32-byte symmetric channel key, then runs challenge/pass using ChaCha20-Poly1305 with that key only.

**Architecture:** Keep A1 (asymmetric X25519 sealed box) unchanged. Slot in A2 as a new suite: `IHybridKeyAgreement` establishes the channel key; `IPqChannel` seals/opens with that key; structure ensures key-share before challenge/pass. Prefer `System.Security.Cryptography.MLKem` when `IsSupported`; otherwise portable **BouncyCastle** ML-KEM-768.

**Tech Stack:** .NET 10, NSec (X25519), BouncyCastle.Cryptography (ML-KEM), existing ChallengePass catalog.

## Global Constraints

- Mnemonic / seed / PIN never on the wire.
- A1 remains installed; Lab remains default `ISessionProofBuilder`.
- Channel key never logged; wipe copies after use where practical.
- Wire algorithm ids are stable versioned strings.

## File map

| File | Responsibility |
|------|----------------|
| `Hybrid/IHybridKeyAgreement.cs` | Hybrid identity + key-share request/response |
| `Hybrid/MlKemProvider.cs` | Portable ML-KEM-768 encaps/decaps (BC + optional BCL) |
| `Hybrid/HybridMlKemX25519Agreement.cs` | Combine ML-KEM SS \|\| X25519 SS → HKDF channel key |
| `Hybrid/IPqChannel.cs` + `PqSymmetricChannel.cs` | Store channel key; Seal/Open ChaCha20-Poly1305 |
| `Structures/PqChannelChallengePassStructure.cs` | Ensure share → challenge → pass via channel |
| `InMemoryPqKeyShareClient.cs` | Mock server key-share for tests |
| DI + suite id `a2-hybrid-pq-channel-v1` | Catalog registration |

## Protocol

### Phase 0 — Device hybrid identity (from custody entropy)

```
seed64 = HKDF(entropy, salt="CipherBank", info="account/hybrid/v1", L=64)
x25519 = DeriveKeyPair(seed64[0..32])
mlkem  = ML-KEM-768 keygen from seed64[32..64]  (deterministic CSPRNG)
Publish: { X25519_PUBLIC_KEY, MLKEM_PUBLIC_KEY, ALGORITHM: "hybrid-mlkem768-x25519-v1" }
```

### Phase 1 — Initial key sharing (once per device/account binding)

```
Server:
  (ct, ss_kem) = ML-KEM-Encaps(device.MLKEM_PUBLIC_KEY)
  ephemeral X25519; ss_x = X25519(eph_sk, device.X25519_PUBLIC_KEY)
  channel_key = HKDF(ss_kem || ss_x, salt="CipherBank-pq-channel", info="pq-channel/v1", L=32)
  → { KEY_SHARE_ID, MLKEM_CIPHERTEXT, SERVER_X25519_PUBLIC_KEY }

Device:
  ss_kem = ML-KEM-Decaps(ct)
  ss_x = X25519(device_sk, SERVER_X25519_PUBLIC_KEY)
  channel_key = same HKDF
  Persist channel_key in memory / SecureStorage (not mnemonic)
```

### Phase 2 — Challenge / pass (full channel AEAD)

```
ALGORITHM wire: "pq-channel-chacha20poly1305-v1"
Challenge CIPHERTEXT = ChaCha20-Poly1305 seal(P, channel_key)
Pass PASS_CIPHERTEXT = ChaCha20-Poly1305 seal(SHA256(P), channel_key)
Same template framing as A1 (CHALLENGE_ID || 0x00 || NONCE)
```

---

### Task 1: ML-KEM provider + hybrid agreement

**Files:** create Hybrid/* as above  
**Test:** encaps/decaps match; hybrid share device↔server yields identical 32-byte keys

- [x] Step 1: Failing test `Hybrid_key_share_produces_matching_channel_keys`
- [ ] Step 2: Implement `MlKem768Provider` + `HybridMlKemX25519Agreement`
- [ ] Step 3: Tests pass
- [ ] Step 4: Commit `feat: hybrid ML-KEM+X25519 key agreement`

### Task 2: PQ symmetric channel + structure

**Files:** `PqSymmetricChannel`, `PqChannelChallengePassStructure`, in-memory key-share + challenge client  
**Test:** full challenge→pass round-trip; body has no seed fields

- [ ] Step 1–4: TDD + commit `feat: PQ channel challenge/pass after key share`

### Task 3: Register suite A2 in DI + docs

- [ ] `AddChallengePassModule` registers A2 alongside A1
- [ ] Update design spec + progress ledger
- [ ] Commit `feat: register a2-hybrid-pq-channel-v1 suite`

## Success criteria

- [ ] A2 suite installed; catalog can `SetActive("a2-hybrid-pq-channel-v1")`
- [ ] Key-share then challenge/pass works in unit tests without OS ML-KEM
- [ ] A1 + Lab unchanged;  unit suite green
