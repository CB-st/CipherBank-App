# Story ID map — CB-* ↔ US-*

Canonical bridge between the Draw.io Playwright scaffold (`UserStories/cipherbank-playwright-scaffold`) and Expo Cora stories (`US-*` in [`PLAYWRIGHT_PLAN.md`](./PLAYWRIGHT_PLAN.md)).

**Source of truth for procedures:** scaffold `docs/USER_STORIES.md` + Expo mirror until Shell parity.  
**At Expo parity:** MAUI Appium (`CipherBank-app.E2ETests`) owns design-spec E2E — see repo `docs/tests/STORY_ID_MAP.md`.

| Scaffold `CB-*` | Expo `US-*` | Expo surface | Status |
|-----------------|-------------|--------------|--------|
| CB-ACCOUNT-001 | US-ONB-01 | Welcome → Keys → Quiz → PIN → Home | Executable (`e2e/stories/onboarding.spec.ts`); MAUI Appium executable (`E2E_RUN=1`) |
| CB-ACCOUNT-002 | US-ONB-02 | Welcome → Set up this device → Keys…PIN | Backlog Expo (`test.fixme`); MAUI Appium executable (recover account, `E2E_RUN=1`) |
| CB-WALLET-001 | US-WLT-01 | User-controlled / local vault wallet | Backlog |
| CB-WALLET-002 | US-WLT-02 | CipherBank checking / hybrid | Backlog |
| CB-FUND-001 | US-RCV-01 + fund | Receive / deposit into user wallet | Backlog (MAUI Receive smoke partial) |
| CB-FUND-002 | US-RCV-01 + fund | Receive into checking | Backlog |
| CB-CARD-001 | US-VLT-01 | Prepaid from account | Backlog |
| CB-CARD-002 | — | Guest prepaid | Backlog (no Expo guest surface yet) |
| CB-PAY-001 | US-PAY-01 | Pay from user wallet | Backlog |
| CB-PAY-002 | US-PAY-02 | Pay from CB checking | Backlog |
| CB-PAY-003 | US-POS-01 | POS / prepaid presentment | Backlog Expo; MAUI PosLab partial |
| CB-MARKET-001 | US-HOM-05 / US-CNV-01 | Home chart + Convert iquote | Backlog Expo; MAUI chart chips executable |
| CB-PREPAID-PLACEHOLDER | — | Blank drawio | Skipped |

## Title convention

Executable specs use both IDs:

`CB-ACCOUNT-001 / US-ONB-01 — Create an account`

## Negative backlog

Scaffold negatives stay in `e2e/stories/cb-backlog.spec.ts` as `test.fixme()` until failure injection exists. Expo-local negatives already implemented:

| ID | Spec |
|----|------|
| US-ONB-04 | PIN mismatch — `onboarding.spec.ts`; MAUI Appium executable (`E2E_RUN=1`) |

MAUI-only executables with no Expo mirror yet: US-ONB-03 (wrong backup-quiz words block advance) and
CB-ACCOUNT-PIN-CHANGE (Shell has a Change PIN surface Expo does not). See repo `docs/tests/STORY_ID_MAP.md`.

## Related

- [`USER_STORIES.md`](./USER_STORIES.md) — procedures + Expo notes
- [`E2E_CONFIGURABLES.md`](./E2E_CONFIGURABLES.md) — env / selectors / routes
- Scaffold: `docs/ADAPTATION_CHECKLIST.md`, `docs/CONFIGURABLES.md`
