# Cora ↔ MAUI feature scorecard (F6.3)

**Branch:** `feat/cora-maui-port`  
**Re-scored:** 2026-07-19  
**Live canvas:** `cora-maui-feature-compare.canvas.tsx` (Cursor canvases)

| Status | Count | Share |
|--------|------:|------:|
| Full parity | 68 | 79% |
| Partial in MAUI | 9 | 10% |
| Cora-only | 7 | 8% |
| MAUI-only | 2 | 2% |
| **Total scored** | **86** | |

Baseline before Phase F was ~53% full parity (46/86).

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
13. [x] Cora FAB chrome
14. [x] XMR managed path (`POST v1/wallets`, no spend key on device)

## Remaining gaps (not must-close / out of scope)

- Activity tab UI (Expo stub; deferred)
- Securities teaser / AAPL pay (out of scope)
- HCE / VTS / MDES (deferred both sides)
- Demo seed PIN flags, setup prompt, currency visibility toggles
- SQLite rates cache, JobQueue P0–P3, post-create QR in add-wallet
- Splash polish, header bell, merchant amount POS UI, base-currency formatting depth

## Verification

- [x] `dotnet test CipherBank-app.Tests` — 212 passed (2026-07-19)
- [ ] Emulator manual: onboard → Home → Convert → Send ACH → Receive → Pay step-up → PosLab → Profile reveal
- [x] E2E AutomationIds + `CoraShellSmokeTests` parity smoke expanded (F6.2)
- [x] No HCE / cloud-seed scope creep
