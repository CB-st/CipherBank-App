# Playwright user-story testing plan

Companion to [`API_BUILD_PLAN.md`](./API_BUILD_PLAN.md) and [`TESTING.md`](./TESTING.md).

**Goal:** Grow an executable Playwright suite that walks Cora **user stories** as we add functionality — mock-first on Expo web today, reusable story IDs for MAUI Appium later.

**Procedural CB-* catalog:** Draw.io scaffold stories live in [`USER_STORIES.md`](./USER_STORIES.md); bridge to Expo `US-*` IDs in [`STORY_ID_MAP.md`](./STORY_ID_MAP.md). Env/selectors: [`E2E_CONFIGURABLES.md`](./E2E_CONFIGURABLES.md). Fixture contract (future): [`FIXTURE_API_CONTRACT.md`](./FIXTURE_API_CONTRACT.md).

**Primary target (this plan):** Expo web (`npm run web`) with `EXPO_PUBLIC_USE_MOCK=true`.  
**Native:** MAUI already has Appium smoke (`CipherBank-app.E2ETests` on `feat/cora-redesign-maui`). Map the same story IDs; do not duplicate drivers in one framework until Shell parity is locked.

---

## Branch context (Jul 2026)

| Branch / PR | Role |
|-------------|------|
| [`feat/cora-redesign-maui`](https://github.com/CB-st/CipherBank-App/tree/feat/cora-redesign-maui) · [PR #16](https://github.com/CB-st/CipherBank-App/pull/16) | **Most advanced product track** — MAUI Shell + Expo handoff docs |
| [`CoraDesignOverhaul`](https://github.com/CB-st/CipherBank-App/tree/CoraDesignOverhaul) · [PR #2](https://github.com/CB-st/CipherBank-App/pull/2) | Expo mock-first + API contract / FullAPIRef |
| `main` | CIP-19 MAUI wallet; no `design_handoff` tree |

Playwright lives under `design_handoff_cipherbank/starter/e2e/` and guards the **Expo contract lab**. MAUI Appium guards the **shipping Shell**.

---

## Principles

1. **Story IDs are stable** — Expo `US-*` and scaffold `CB-*` both appear in titles when mapped (`CB-ACCOUNT-001 / US-ONB-01`). Same IDs in Playwright, Appium facts, and GitHub issues.
2. **Mock-first** — suite runs with `USE_MOCK=true` without staging. Add `@live` tag later for `USE_MOCK=false`.
3. **One story → one (or few) tests** — assert user-visible outcomes, not implementation details. Prefer `runStoryStep()` for CB-* procedures.
4. **Seed modes** — `SEED_DEMO=false` for clean OOTB; optional project for lab seed.
5. **No secrets in repo** — demo PIN `000000` only under `SEED_DEMO`; clean path generates PIN in-test.
6. **Accessibility hooks** — prefer `getByRole` / `getByTestId`; add `testID` / `accessibilityLabel` as stories land. Central map: `e2e/config/selectors.ts`.

---

## Suite phases (grow with the plan)

| Phase | When | Stories unlocked |
|-------|------|------------------|
| **PW0 Scaffold** | Now | Config, web server, smoke shell |
| **PW1 Onboarding** | Clean install done | Welcome → Keys → Quiz → PIN → Home |
| **PW2 Shell** | Tabs stable | Home empty, Convert quote, Send ACH, Receive QR, Profile |
| **PW3 Setup / bootstrap** | Returning path | Pull CipherBank, setup card, skip |
| **PW4 Money** | After mock money hardened | Convert settle toast, Send, Pay mix undercoverage |
| **PW5 Vault / POS lab** | Lab screens | Cards list, POS simulate exchange |
| **PW6 Live API** | Staging cutover (API P0+) | `@live` session + prefs + portfolio |

---

## User stories → tests

### Onboarding & custody

| ID | Story | Given / When / Then | Priority |
|----|-------|---------------------|----------|
| **US-ONB-01** | New user creates account | Clean storage → Welcome → Create → Keys shown → Quiz pass → Set PIN → lands unlocked Home | P0 |
| **US-ONB-02** | Returning user sets up device | Welcome → Set up this device → Keys…PIN → bootstrap toast / contacts | P0 |
| **US-ONB-03** | Backup quiz rejects wrong words | Wrong quiz answers → cannot advance | P1 |
| **US-ONB-04** | PIN mismatch blocks seal | Confirm ≠ PIN → error, stay on Set PIN | P1 |
| **US-LCK-01** | Unlock with CipherBank PIN | Locked app → enter PIN → Home | P0 |
| **US-LCK-02** | Wrong PIN shows error / lockout | Bad PIN → error; after N fails, cooldown message | P1 |

### Home & setup

| ID | Story | Assert | Priority |
|----|-------|--------|----------|
| **US-HOM-01** | Clean Home is empty | After ONB-01, total ~$0, no Maya/Jordan until setup | P0 |
| **US-HOM-02** | Setup card until complete | `setup_complete=0` → Cora setup CTAs visible | P0 |
| **US-HOM-03** | Skip setup dismisses card | “I will do this later” → card gone | P1 |
| **US-HOM-04** | Pull from CipherBank | Pull → toast + recipients available in Send | P1 |
| **US-HOM-05** | Charts render | History series paints without crash | P2 |

### Convert / Send / Pay / Receive

| ID | Story | Assert | Priority |
|----|-------|--------|----------|
| **US-CNV-01** | Quote lock from iquote | Enter amount → rate/out updates → Convert enabled | P0 |
| **US-CNV-02** | Convert accepts | Confirm → pending/settled toast; no freeze | P1 |
| **US-SND-01** | Pick ACH contact | Open Send → picker → select/add payee | P0 |
| **US-SND-02** | Send ACH | Amount + speed → accepted toast | P1 |
| **US-PAY-01** | Mix undercoverage | Sources &lt; total → error `mix_undercovered` / clear message | P1 |
| **US-RCV-01** | Receive shows address/QR | Asset selected → address non-empty, QR visible | P0 |

### Profile / vault / POS

| ID | Story | Assert | Priority |
|----|-------|--------|----------|
| **US-PRF-01** | Prefs round-trip | Toggle Cora / appearance → survives reload (local or mock PUT) | P1 |
| **US-VLT-01** | Vault cards list | Profile vault → cards from mock fixture | P2 |
| **US-POS-01** | Simulate exchange | Tap to pay lab → authorize → Simulate → settle stages | P1 |

### Lab seed (optional project)

| ID | Story | Assert | Priority |
|----|-------|--------|----------|
| **US-LAB-01** | SEED_DEMO unlock | `SEED_DEMO=true` → unlock with `000000` → rich/demo path | P2 |

### Live API (future `@live`)

| ID | Story | Depends on API plan | Priority |
|----|-------|---------------------|----------|
| **US-LIVE-01** | Session + prefs | P0 + P3 staging | Later |
| **US-LIVE-02** | Portfolio non-empty funded user | P1 staging | Later |
| **US-LIVE-03** | Public `/iquote` against `api.cipherbank.money` | Public host up | Later |

---

## Mapping to API_BUILD_PLAN

| Playwright phase | Unblocks / guards |
|------------------|-------------------|
| PW1–PW2 | App Phase 1 (mock UI) regressions |
| PW3 | Clean install + bootstrap (app done; backend P3 later) |
| PW4 | Client money rails before backend P4 |
| PW5 | Vault/POS lab before backend P6–P7 |
| PW6 `@live` | After C1–C2 + P0–P3 staging |

Do **not** wait for backend P0 to start PW0–PW2 — mock stories are the safety net while server work is open (#3–#14).

---

## Suggested folder layout

```
design_handoff_cipherbank/starter/
  playwright.config.ts
  e2e/
    config/
      env.ts
      selectors.ts
      routes.ts
    stories/catalog.ts       # CB-* StoryDefinition[]
    support/story-runner.ts
    fixtures/onboarding.ts
    stories/
      smoke.spec.ts
      onboarding.spec.ts     # CB-ACCOUNT-001 / US-ONB-*, US-ONB-04
      cb-backlog.spec.ts     # remaining CB-* fixme + negatives
  docs/
    PLAYWRIGHT_PLAN.md
    USER_STORIES.md
    STORY_ID_MAP.md
    E2E_CONFIGURABLES.md
    FIXTURE_API_CONTRACT.md
```

### Config sketch

- `webServer`: `npm run web` (or `npx expo start --web --port 8081`)
- `baseURL`: `http://127.0.0.1:8081`
- Projects: `clean` (default), `lab` (`SEED_DEMO=true`), `live` (opt-in)
- Retries: 1 on CI; trace on failure
- Timeout: generous for Metro first boot (60–120s)

### Storage reset (clean install)

Before US-ONB-*: clear site data / IndexedDB / localStorage for the origin (web SecureStore/AsyncStorage mirrors). Document exact keys once identified (`cb_custody_v2`, SQLite OPFS if used).

---

## Implementation checklist

### PW0 — Scaffold (do first)

- [x] Add `@playwright/test` + `playwright.config.ts`
- [x] `npm run test:e2e` / `test:e2e:ui` scripts
- [x] Smoke: load app → see Welcome **or** Unlock (branch on custody)
- [x] Document in `TESTING.md`
- [ ] CI job (optional): web e2e on PR touching `starter/`

### PW1 — Onboarding

- [x] Add `testID`s: `welcome-create`, `welcome-returning`, `keys-continue`, `quiz-*`, `pin-input`, `pin-confirm`
- [x] Implement CB-ACCOUNT-001 / US-ONB-01 via `runStoryStep`, US-ONB-04
- [x] Storage reset helper
- [x] CB-* backlog + negatives as `test.fixme` (`cb-backlog.spec.ts`)
- [ ] US-ONB-02 returning path
- [ ] US-ONB-03 wrong quiz words

### PW2 — Shell

- [ ] Tab `testID`s: `tab-home`, `tab-convert`, …
- [ ] US-HOM-01, US-CNV-01, US-RCV-01, US-SND-01

### PW3+ 

- [ ] Setup / money / POS stories as features harden
- [ ] `@live` project when staging exists

---

## Relationship to MAUI Appium

Existing smoke (`CoraShellSmokeTests`): Unlock → Home → Convert → Receive (and related).

| Story ID | Playwright (Expo web) | Appium (MAUI) |
|----------|----------------------|---------------|
| US-LCK-01 | Yes | Yes (exists) |
| US-CNV-01 | Yes | Yes (exists) |
| US-RCV-01 | Yes | Yes (exists) |
| US-ONB-01 | Yes (web clean) | Add when onboarding parity lands |
| US-POS-01 | Simulate on web | Device / lab build |

Keep **one story catalog**; two runners.

---

## Definition of done for a story

1. Automated test with story ID in title: `US-CNV-01 quote lock enables convert`
2. Passes locally against mock web
3. Linked from feature PR / issue when the story ships
4. Failure produces Playwright trace artifact

---

## Out of scope (for now)

- Full visual regression / Percy
- Real NFC RF in Playwright (use Simulate exchange)
- Load / performance soak
- Replacing MAUI Appium with Playwright mobile (revisit after Shell freeze)
