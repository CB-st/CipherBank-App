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

## Persist systems + mnemonic backup (2026-07-20)

Plan: `docs/superpowers/plans/2026-07-20-persist-systems-and-mnemonic-backup.md`  
Branch: `feat/cora-redesign-maui` · PR #16  
Base at plan start: `4fbcadb`


## Persist systems + mnemonic backup (2026-07-20)

Plan: `docs/superpowers/plans/2026-07-20-persist-systems-and-mnemonic-backup.md`  
Branch: `feat/cora-redesign-maui` · PR #16  
Base at plan start: `4fbcadb`

Task 1: complete (commits 4fbcadb..e58ce9e, review clean; minors: canvas Wave 5 label, CoraBar count wording)
Task 2: complete (commits e58ce9e..2b51e4d, review clean; minor note CREATED_AT ISO vs epoch ms)
Task 3: complete (commits 2b51e4d..8ce7623, review clean)
- Task 4: complete (commits 8ce7623..8cd157b, review clean)
- Task 5: complete (commits 8cd157b..3defb6e, review clean)
- Task 6: complete (commits 3defb6e..cff3ad5, review clean)
- Task 7: complete (commits cff3ad5..93f135a, review clean)
- Task 8: complete (commits 93f135a..2a42478, review clean)
- Task 9: complete (commits 2a42478..4ed5280, DONE_WITH_CONCERNS: Android SDK missing for XAML compile; feature code + tests landed)
- Task 10: complete (commit bf1b954, DONE_WITH_CONCERNS: Android SDK missing for XAML compile; UI/DI wired onto existing Core IMnemonicBackupService, no new Core helpers so no new tests, full suite green 248/248)

- Task 11: complete (Wave 5 emulator smoke + persistence-first scorecard 100%; Android Debug EmbedAssemblies install; tests 248/248; canvas + F6.3 scorecard re-scored 2026-07-22)
- Task 11 review follow-up (2026-07-22): cold-start fixed (Splash FadeTo pulse removed); **Other assets** expand OK; **Send** delete recipient E2E OK; **Profile** vault remove final confirm still incomplete (idle lock + Unlock PIN automation); see `.superpowers/sdd/task-11-report.md` follow-up

