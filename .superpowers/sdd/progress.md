# SDD Progress Ledger — Cora MAUI Parity

Branch: `feat/cora-maui-port`
Auth plans:
- `docs/superpowers/plans/2026-07-19-challenge-pass-module-and-f6.md`
- `docs/superpowers/plans/2026-07-19-hybrid-pq-channel.md`
Spec: `docs/superpowers/specs/2026-07-18-challenge-pass-auth-design.md`

## Complete

- F0–F5
- Challenge/pass module (3 slots) + A1 suite
- **A2 hybrid PQ channel** (`a2-hybrid-pq-channel-v1`):
  - ML-KEM-768 + X25519 key-share → 32-byte channel key
  - ChaCha20-Poly1305 challenge/pass on that key
- Lab remains default session opener

## Next

- Custody `IAccountKeySource` + HTTP key-share/challenge clients + SessionProofMode
- F6.1 XMR managed · F6.2 E2E · F6.3 PR checklist
