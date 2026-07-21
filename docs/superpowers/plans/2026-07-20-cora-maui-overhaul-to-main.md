# Cora overhaul → main (MAUI first)

**Repo:** CB-st/CipherBank-App only  
**Branch:** `feat/cora-redesign-maui`  
**PR:** [#16](https://github.com/CB-st/CipherBank-App/pull/16) → `CoraDesignOverhaul`  
**Date:** 2026-07-20  

## Rules

- Do **not** cross-commit with CipherBank-src. Backend work is planned from App docs at most; never shared commits.
- **MAUI Cora Shell is the product.** Expo `design_handoff_cipherbank/starter` is the behavioral/visual **spec**, not a second production app.
- All new work lands on `feat/cora-redesign-maui` until #16 merges.

## Branch consolidation (done)

| Branch | Disposition |
|--------|-------------|
| `feat/cora-onto-cip19-main` | Contained in redesign tip |
| `feat/cora-maui-port` | Contained; PR #15 → `main` superseded by #16 |
| `feat/cora-redesign-maui` | **Integration tip** — keep pushing here |
| `CoraDesignOverhaul` | Merge target for #16, then refresh draft PR #2 → `main` |

## Product path to main

1. Execute **persistence-first waves** on redesign tip (see persist plan below) — closes remaining in-scope scorecard gaps.
2. Close remaining **partial** chrome gaps (header bell, POS merchant UI) after persistence waves.
3. Emulator smoke + `dotnet test` green.
4. Merge PR #16 into `CoraDesignOverhaul`.
5. Update draft PR #2 (`CoraDesignOverhaul` → `main`) as the overhaul cutover.
6. Close PR #15; freeze Expo as runtime (keep as spec + contract docs).
7. Park or remove legacy non-Shell pages (Login / Dashboard / Wallet) from the product story; Settings may remain as lab/debug.

## Persistence-first plan (active)

**Spec:** `docs/superpowers/specs/2026-07-20-persist-systems-design.md`  
**Implementation:** `docs/superpowers/plans/2026-07-20-persist-systems-and-mnemonic-backup.md`

Wave 1 (docs rescore) is done at **83%** (71/86 parity). Waves 2–5 deliver local market persist, P1/P2 sync queue, vault/wallet/recipient management, mnemonic recovery file, then emulator verify and re-score toward persistence-first 100%.

## Expo surfaces → MAUI (summary)

Full row inventory: Cursor canvas `cora-maui-overhaul-roadmap.canvas.tsx` and F6.3 scorecard `docs/superpowers/plans/2026-07-19-cora-maui-f6-scorecard.md`.

| MAUI status | Meaning | Action |
|-------------|---------|--------|
| Parity | Matches Expo behavior | Maintain |
| Partial | Present but thinner / wrong wire | Recreate to match Expo |
| MAUI-only | No Expo counterpart | Keep if product (ChallengePass); park if legacy |
| Deferred | Stub / non-goal both sides | Do not block main |

### Must recreate / finish in MAUI (from Expo checklist)

| Priority | Gap | Expo reference | MAUI touch | Status |
|----------|-----|----------------|------------|--------|
| P0 | Convert live indicative quotes via public `/iquote` | `publicMarket.api.ts`, `useQuoteLock` | `ConvertViewModel` → `IPublicQuoteService` | **Done** (2026-07-20) |
| P0 | Home chart symbols from holdings ∩ enabled currencies | Home rates/held symbols | `HomeViewModel.BuildChartSymbols` | **Done** |
| P0 | Emulator manual smoke + keep unit/E2E green | — | Device | Open |
| P1 | Profile prefs depth (appearance, enabled currencies, send speed) | `ProfileScreen`, `prefs/*` | `ProfileViewModel` / `ProfilePage` | **Done** |
| P1 | Cora chrome fidelity (FAB vs Expo bar) | `CoraBar.tsx` + `CoraAssistant.tsx` | `Controls/CoraBar.cs` + `CoraFab` | **Done** (inline bar on Convert/Pay/Receive/Keys + FAB) |
| P2 | Splash polish | `SplashScreen.tsx` | `SplashPage` + AppShell bootstrap | **Done** |
| P2 | Legacy screen retirement from product docs/DI story | — | Login/Dashboard/Wallet(/Purchase?) | Open |

### Explicitly deferred (do not block overhaul)

- Activity tab UI  
- Bank link / Fund onboarding steps  
- Securities-as-payment  
- Production HCE / VTS / MDES  

## Verification gates before main

- [ ] `dotnet test CipherBank-app.Tests` green  
- [ ] Android Debug install (`net10.0-android`)  
- [ ] Manual: onboard → Home → Convert → Send → Receive → Pay step-up → PosLab → Profile reveal  
- [ ] Appium `CoraShellSmokeTests` when APK + `E2E_TEST_PIN` available  
- [ ] PR #15 closed; #16 merged; #2 retargeted/updated for main cutover  

## Draft PR #2 (`CoraDesignOverhaul` → `main`)

Draft [#2](https://github.com/CB-st/CipherBank-App/pull/2) is the **main cutover vehicle**. It currently describes the Expo handoff only.

### To close #2 as the overhaul rebuild

1. Merge [#16](https://github.com/CB-st/CipherBank-App/pull/16) into `CoraDesignOverhaul` (brings MAUI Shell + parity).
2. Retitle/rewrite #2 as **MAUI-first Cora overhaul** (Expo = spec under `design_handoff_cipherbank/`).
3. Finish remaining gates: emulator smoke, splash polish, park legacy Login/Dashboard/Wallet.
4. Mark #2 ready for review → merge to `main`.

### Already absorbed into #16 tip (no separate PR work)

- CIP-19 radial wallet restyle ancestry (`feat/cora-onto-cip19-main`)
- MAUI port Phases A–F + challenge/pass (`feat/cora-maui-port` / closed #15)
- Public quote Part A + Convert/Home/Profile/Cora chrome parity commits

## Related docs

- `docs/superpowers/plans/2026-07-18-cora-maui-parity.md` — Phase F parity plan  
- `docs/superpowers/plans/2026-07-19-cora-maui-f6-scorecard.md` — 83% scorecard (Wave 1 rescore)
- `docs/superpowers/specs/2026-07-20-persist-systems-design.md` — persistence-first + mnemonic backup design
- `docs/superpowers/plans/2026-07-20-persist-systems-and-mnemonic-backup.md` — Waves 2–5 implementation plan  
- `design_handoff_cipherbank/starter/docs/PROTOTYPE_MAP.md` — Expo inventory  
- `design_handoff_cipherbank/starter/docs/NEXT_PHASE.md` — Expo next-phase (spec only)
