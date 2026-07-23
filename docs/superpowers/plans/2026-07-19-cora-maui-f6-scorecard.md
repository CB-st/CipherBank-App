# Cora ↔ MAUI feature scorecard (F6.3)

**Branch:** `feat/cora-redesign-maui` (PR #16)  
**Re-scored:** 2026-07-22 (Wave 5 persistence-first closeout)  
**Live canvas:** `cora-maui-feature-compare.canvas.tsx` (Cursor canvases)

| Status | Count | Share |
|--------|------:|------:|
| Full parity | 75 | 86% |
| Partial in MAUI | 5 | 6% |
| Cora-only | 5 | 6% |
| MAUI-only | 2 | 2% |
| **Total scored** | **87** | |

**Persistence-first:** **100%** (77/77) — denominator excludes long-term rows (Activity, securities teaser, demo seed, setup prompt, header bell, Pay/POS polish, base-currency formatting depth, optional post-create QR). maui-only rows count as satisfied for this metric.

Baseline before Phase F was ~53% full parity (46/86). After F6 (2026-07-19) was 79% (68/86). Wave 1 (2026-07-20): 83% (71/86). Wave 5 adds mnemonic backup row (+1) and flips rates cache, vault CRUD, JobQueue P1/P2 to parity.

## Must-close list (F0.1) — all checked

1. [x] Real OS biometrics (device-secret unlock)
2. [x] Step-up auth before pay / convert / POS / reveal
3. [x] Backup quiz = 3 random words
4. [x] Home section visibility + order applied
5. [x] Values hidden on launch + eye toggle
6. [x] Chart ranges 1D / 1W / 1M / 1Y
7. [x] Stream `RATE.TICK` / settle refresh Home + Convert
8. [x] Convert asset pickers + fee / privacy / settlement rows
9. [x] Full ACH recipient fields
10. [x] Receive asset chips + derivation path
11. [x] Prefs GET/PUT sync when not mocking
12. [x] Account bootstrap after seal
13. [x] Cora FAB + CoraBar chrome
14. [x] XMR managed path (`POST v1/wallets`, no spend key on device)
15. [x] SplashPage + MinSplashDuration boot gate
16. [x] Public `/iquote` via `IPublicQuoteService`
17. [x] Profile currency visibility toggles (`EnabledCurrencies`)

## Persistence-first queue (Waves 2–5) — closed

From `docs/superpowers/specs/2026-07-20-persist-systems-design.md` — plan: `docs/superpowers/plans/2026-07-20-persist-systems-and-mnemonic-backup.md`.

**Wave 2 — Local market persist** ✓

- Home: filter holdings by `EnabledCurrencies`; expandable **Other assets (N)** for hidden
- `LocalDb`: `rates_snapshot`, `sync_meta`; OHLC write-through
- `IRatesCache` / `IMarketRepository`; P2 cold hydrate; P1 chart persist

**Wave 3 — Sync queue (P1/P2 only)** ✓

- `ISyncJobQueue`: P1 chart / P2 cold bootstrap; concurrency 2; P3 deferred

**Wave 4 — Management UIs + mnemonic backup** ✓

- Vault card add/remove on Profile + step-up
- Wallet delete + recipient delete (Send)
- `IMnemonicBackupService`: ciphered recovery file export/restore

**Wave 5 — Emulator verify + re-score** ✓

- Android Debug install (`EmbedAssembliesIntoApk=true`) + manual smoke (onboard → money tabs → vault → backup export → clear data → restore → Home)
- Canvas / scorecard persistence-first **100%**

## Long-term goals (not blocking persistence-first)

- Activity tab UI (Expo stub; deferred)
- Securities teaser / AAPL pay (out of scope)
- Demo seed PIN `000000` flags, setup prompt (pull/ACH/skip)
- HCE / VTS / MDES (deferred both sides — already scored parity)
- Full Expo P3 JobQueue (idle + charging)
- Header bell, POS merchant amount UI, base-currency formatting depth
- Pay hardcoded recipient label, undercovered mix polish
- Optional post-create QR in add-wallet

## Verification

- [x] `dotnet test CipherBank-app.Tests -p:CollectCoverage=false` — 248 passed (2026-07-22)
- [x] Emulator smoke: Welcome → Keys → Quiz → SetPin → Home → Convert → Send (delete payee UI) → Profile vault add/remove → Backup export → clear app data → restore → SetPin → Home
- [x] E2E AutomationIds + `CoraShellSmokeTests` parity smoke expanded (F6.2)
- [x] No HCE / cloud-seed scope creep

## Task 11 review follow-up (2026-07-22)

Cold-start hang fixed by removing Splash `FadeTo` pulse (UI-thread spin). Re-smoke all three click-throughs OK: **Other assets (1)** expand (ETH); **Send** delete recipient E2E (`Rent — 4th St LLC`); **Profile** vault remove through step-up PIN + final confirm (`Demo card •••• 0001`). Persistence-first scorecard numbers unchanged (77/77). Details: `.superpowers/sdd/task-11-report.md` follow-up section.
