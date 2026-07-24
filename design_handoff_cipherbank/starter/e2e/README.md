# Playwright E2E (Expo web)

- Plan: [`../docs/PLAYWRIGHT_PLAN.md`](../docs/PLAYWRIGHT_PLAN.md)
- CB-* stories: [`../docs/USER_STORIES.md`](../docs/USER_STORIES.md)
- ID map: [`../docs/STORY_ID_MAP.md`](../docs/STORY_ID_MAP.md)
- Configurables: [`../docs/E2E_CONFIGURABLES.md`](../docs/E2E_CONFIGURABLES.md)

```bash
cd design_handoff_cipherbank/starter
npm install
npx playwright install chromium   # once
npm run test:e2e                  # headless
npm run test:e2e:ui               # interactive
```

Default project **clean**: `SEED_DEMO=false`, `USE_MOCK=true`. Starts Expo web on port 8081 unless already running.

Executable today: `US-SMOKE-01`, `CB-ACCOUNT-001 / US-ONB-01`, `US-ONB-04`. Remaining CB-* stories are `test.fixme` in `stories/cb-backlog.spec.ts`.
