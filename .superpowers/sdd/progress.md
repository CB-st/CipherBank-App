# SDD Progress Ledger — Cora MAUI Parity

Branch: `feat/cora-maui-port`
Plan: `docs/superpowers/plans/2026-07-18-cora-maui-parity.md`
Auth module plan: `docs/superpowers/plans/2026-07-19-challenge-pass-module-and-f6.md`
Spec: `docs/superpowers/specs/2026-07-18-challenge-pass-auth-design.md`

## Complete

- F0–F5
- **Challenge/pass module** `CipherBank-app.ChallengePass` with slot-in/out:
  - Algorithm (`ISealAlgorithm`)
  - Template (`IChallengeTemplate`)
  - Structure (`IChallengePassStructure`)
  - Catalog + A1 suite (`a1-x25519-chacha-v1`)
- Lab remains default `ISessionProofBuilder`

## Next

- Custody `IAccountKeySource` + HTTP challenge client + SessionProofMode flag
- F6.1 XMR managed · F6.2 E2E · F6.3 PR checklist
