import { defineConfig, devices } from '@playwright/test';

/**
 * Expo web user-story suite — see docs/PLAYWRIGHT_PLAN.md
 * Run: npm run test:e2e
 */
const PORT = Number(process.env.E2E_PORT ?? 8081);
const BASE = process.env.E2E_BASE_URL ?? `http://127.0.0.1:${PORT}`;

export default defineConfig({
  testDir: './e2e/stories',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  timeout: 120_000,
  expect: { timeout: 15_000 },
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'e2e-report' }]],
  use: {
    baseURL: BASE,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
  projects: [
    {
      name: 'clean',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: `npx expo start --web --port ${PORT}`,
    url: BASE,
    reuseExistingServer: !process.env.CI,
    timeout: 180_000,
    env: {
      ...process.env,
      EXPO_PUBLIC_USE_MOCK: 'true',
      EXPO_PUBLIC_SEED_DEMO: 'false',
      EXPO_PUBLIC_MOCK_HAS_WALLET: 'false',
      CI: '1',
    },
  },
});
