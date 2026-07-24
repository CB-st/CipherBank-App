# Playwright E2E (Expo web)

See story catalog: [`../docs/PLAYWRIGHT_PLAN.md`](../docs/PLAYWRIGHT_PLAN.md).

```bash
cd design_handoff_cipherbank/starter
npm install
npx playwright install chromium   # once
npm run test:e2e                  # headless
npm run test:e2e:ui               # interactive
```

Default project **clean**: `SEED_DEMO=false`, `USE_MOCK=true`. Starts Expo web on port 8081 unless already running.
