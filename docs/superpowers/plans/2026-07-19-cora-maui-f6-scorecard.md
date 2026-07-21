# Cora ↔ MAUI feature scorecard (F6.3)

**Branch:** `feat/cora-redesign-maui` (PR #16)  
**Re-scored:** 2026-07-20 (Wave 1 rescore)  
**Live canvas:** `cora-maui-feature-compare.canvas.tsx` (Cursor canvases)

| Status | Count | Share |
|--------|------:|------:|
| Full parity | 71 | 83% |
| Partial in MAUI | 7 | 8% |
| Cora-only | 6 | 7% |
| MAUI-only | 2 | 2% |
| **Total scored** | **86** | |

Baseline before Phase F was ~53% full parity (46/86). After F6 (2026-07-19) was 79% (68/86). Wave 1 rescore (+3 parity): splash, public `/iquote`, Profile `EnabledCurrencies`, CoraBar note.

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

## Persistence-first queue (Waves 2–4)

From `docs/superpowers/specs/2026-07-20-persist-systems-design.md` — implementation plan: `docs/superpowers/plans/2026-07-20-persist-systems-and-mnemonic-backup.md`.

**Wave 2 — Local market persist**

- Home: filter holdings by `EnabledCurrencies`; expandable **Other assets (N)** for hidden
- `LocalDb`: `rates_snapshot`, `sync_meta`; extend OHLC write-through
- `IRatesCache` / `IMarketRepository`; P2 cold hydrate; P1 chart persist from `IPublicQuoteService`

**Wave 3 — Sync queue (P1/P2 only)**

- `ISyncJobQueue`: P1 chart / P2 cold bootstrap; concurrency 2; defer P3 (idle + charging)

**Wave 4 — Management UIs + mnemonic backup**

- Vault card add/remove on `IProductApi` + Profile UI
- Wallet delete (Home / AddWallet) + recipient delete (Send)
- Optional: post-create QR in add-wallet
- `IMnemonicBackupService`: ciphered recovery file export/restore (recovery password)

**Wave 5 — Emulator verify + re-score**

- Manual smoke + update canvas to persistence-first 100% (denominator excludes long-term rows)

## Long-term goals (not blocking persistence-first)

- Activity tab UI (Expo stub; deferred)
- Securities teaser / AAPL pay (out of scope)
- Demo seed PIN `000000` flags, setup prompt (pull/ACH/skip)
- HCE / VTS / MDES (deferred both sides — already scored parity)
- Full Expo P3 JobQueue (idle + charging)
- Header bell, POS merchant amount UI, base-currency formatting depth
- Pay hardcoded recipient label, undercovered mix polish

## Verification

- [x] `dotnet test CipherBank-app.Tests` — 212 passed (2026-07-19)
- [ ] Emulator manual: onboard → Home → Convert → Send ACH → Receive → Pay step-up → PosLab → Profile reveal
- [x] E2E AutomationIds + `CoraShellSmokeTests` parity smoke expanded (F6.2)
- [x] No HCE / cloud-seed scope creep
