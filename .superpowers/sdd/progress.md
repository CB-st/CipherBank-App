# SDD Progress Ledger — Cora MAUI Parity

Branch: `feat/cora-maui-port`  
PR: https://github.com/CB-st/CipherBank-App/pull/15  
Auth plans:
- `docs/superpowers/plans/2026-07-19-challenge-pass-module-and-f6.md`
- `docs/superpowers/plans/2026-07-19-hybrid-pq-channel.md`
Spec: `docs/superpowers/specs/2026-07-18-challenge-pass-auth-design.md`  
Scorecard: `docs/superpowers/plans/2026-07-19-cora-maui-f6-scorecard.md`

## Complete

- F0–F6 (behavioral parity wave)
- Challenge/pass module (3 slots) + A1 suite
- **A2 hybrid PQ channel** (`a2-hybrid-pq-channel-v1`)
- Cutover wiring: custody keys, `SessionProofMode`, HTTP/mock clients (Lab default)
- F6.1 Managed XMR · F6.2 E2E parity smoke · F6.3 scorecard + PR checklist
- Comparison canvas re-scored: **79% full parity** (68/86); must-close list checked

## Next (post-parity)

- Emulator manual smoke (onboard → money tabs → PosLab → Profile reveal)
- Live staging: `UseMockServices=false` against challenge / key-share / wallets APIs
- Optional: remaining Cora-only polish (bell, splash, JobQueue, rates cache)
- Challenge/pass cutover when live endpoints are ready (`SessionProofMode` → A1/A2)
