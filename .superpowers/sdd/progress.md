# SDD Progress Ledger — Cora MAUI Parity

Branch: `feat/cora-maui-port`
Plan: `docs/superpowers/plans/2026-07-18-cora-maui-parity.md`
Workspace: scoped under Digital Teller R2/`CipherBank-App/` (root stay put)

## Complete

- **F0.1** Plan + PR tracking
- **F1.1** Device-secret biometrics unlock (Expo parity)
- **F1.2** Step-up auth gates (Pay/Convert/POS/Reveal)
- **F1.3** Three-word backup quiz
- **F2.1** Home sections `holdings`/`localWallets` + green/gold + `AssetsLayout`
- **F2.2** Hide balances + values-hidden-on-launch
- **F2.3** Chart ranges 1D/1W/1M/1Y
- **F2.4** Stale/Updating indicator

## Notes

- Trust model: logical gate + SecureStorage device secret (same as Expo), not TEE-bound bio key release
- Tests: 187 passed; Android net10.0 build 0 errors
