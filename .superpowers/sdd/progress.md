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
- Task 11 review follow-up (2026-07-22): cold-start fixed (Splash FadeTo pulse removed); **Other assets** expand OK; **Send** delete recipient E2E OK; **Profile** vault remove through final confirm OK; see `.superpowers/sdd/task-11-report.md` follow-up

## Task 11 review follow-up (2026-07-22)

Cold-start hang fixed by removing Splash `FadeTo` pulse (UI-thread spin). Re-smoke all three click-throughs OK: **Other assets (1)** expand (ETH); **Send** delete recipient E2E (`Rent — 4th St LLC`); **Profile** vault remove through step-up PIN + final confirm (`Demo card •••• 0001`). Persistence-first scorecard numbers unchanged. Details: `.superpowers/sdd/task-11-report.md` follow-up section.
- Whole-branch review fixes (2026-07-22): BackupFileService deletes temp recovery export after share; progress header dedupe; AppShell splash comment refreshed.

## MAUI Appium Wave 0–1 (2026-07-25)

Plan: `docs/superpowers/plans/2026-07-25-maui-appium-wave0-account.md`  
Spec: `docs/superpowers/specs/2026-07-25-maui-appium-story-completion-design.md`  
Branch: `feat/cora-redesign-maui`  
Base at plan start (spec commit): `2b0fa46`

Task 1: complete (commits 2b0fa46..df81511, review clean)
Task 2: complete (commits df81511..d365718, review clean; minors: case vs dict parse_args, log() timestamp comment)
Task 3: complete (commits d365718..54f57f3, review clean)
Task 4: complete (commits 54f57f3..f87f4a6, review clean; note: page-object AGENTS comments deferred to Task 7)
Task 5: complete (commits f87f4a6..2adadc4, review clean; minors: SkippableFact test methods lack Use/Scope docs; StoryGuard wrong-screen paths unexercised until Task 7 emulator run)
Task 6: complete (commits 2adadc4..2b59bc4, review clean; minors: StoryProcedures inert until Task 7, dictionary insertion-order relied on for step order)
Task 7: complete (commits 2b59bc4..f351479, review clean — CB-ACCOUNT-001 green on CipherBank_API34, 1 passed; minors: journal step labels submit/backup fire slightly out of step with the concrete flow; Selenium.WebDriver pinned back 4.27.0→4.21.0 for Appium.WebDriver 5.0.0 compat — needs forward re-pin follow-up; Node.js/npx fix is dev-box-local, not scripted in repo)
Task 8: complete (commits f351479..9ab8090, review clean after Important fix: IsVisible+non-empty text on BackupQuiz/SetPin error labels; US-ONB-03/04 green on device)
Task 9: complete (commits 9ab8090..f7014cc, review fix-up 10e66e7; CB-ACCOUNT-PIN-CHANGE green on CipherBank_API34; Important fixes: PinChangeCoordinator moved onto ICustodyService.ChangePinAsync to refuse legacy-blob PIN changes, wrong-current-PIN assertion made real (visible+non-empty ChangePinErrorLabel); full unit suite 262/262)
Task 10: complete (commits 10e66e7..19d4466, review clean; CB-ACCOUNT-002 green on CipherBank_API34 — real BackupFileService export to Downloads/MediaStore replacing the cache-and-delete stub, restore via Android document picker, wrong-password rejection, same-custody proof by comparing revealed mnemonic pre/post wipe and by independently decrypting the pulled `.cbr.json`; full Fresh-device regression 5/5; unit suite 262/262)
Task 11: complete (commit `3c5fc3a`) — docs closeout — polished `docs/tests/README.md` (harness pointer), `docs/README.md` (STORY_ID_MAP link), Expo mirror docs (`design_handoff_cipherbank/starter/docs/{PLAYWRIGHT_PLAN,STORY_ID_MAP}.md` — fixed stale `E2E_FRESH` env var to `E2E_RUN`, added CB-ACCOUNT-002/PIN-CHANGE/US-ONB-03 mirror rows), confirmed `artifacts/e2e-journal|e2e-recovery|e2e-diagnostics` stay gitignored; see `.superpowers/sdd/task-11-report.md` for harness run outcome

### Task 11 review fix (2026-07-25)

Review of Task 11 (base `3c5fc3a`) found `--wave account` in `scripts/e2e-android.sh` only matched
`CB_ACCOUNT_*`-named test methods, silently excluding the `US_ONB_03`/`US_ONB_04` negative Facts from
`AccountStories.cs` despite them being Wave 0–1 account/onboarding stories. Fixed `WAVE_STORY_PREFIXES`
so the `account` wave's filter is `FullyQualifiedName~CB_ACCOUNT|FullyQualifiedName~US_ONB_03|FullyQualifiedName~US_ONB_04`,
covering all five Facts: CB-ACCOUNT-001, US-ONB-03, US-ONB-04, CB-ACCOUNT-PIN-CHANGE, CB-ACCOUNT-002.
Updated `docs/tests/README.md`, `docs/tests/e2e-tests.md`, `docs/tests/STORY_ID_MAP.md` to describe the
expanded filter, and `docs/tests/README.md`'s test-pyramid inventory to name `AccountStories` as the
primary executable story suite (previously only `CoraShellSmokeTests + StoryBacklog`). Verified the
filter expansion with a `bash -x` trace of `resolve_test_filter` (no emulator/dotnet available in this
sandbox for a full harness re-run); see `.superpowers/sdd/task-11-report.md` follow-up section.
Fix commit range: `3c5fc3a..40805b3`.
