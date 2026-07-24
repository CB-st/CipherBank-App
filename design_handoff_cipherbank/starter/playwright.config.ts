import { defineConfig, devices } from '@playwright/test';
import { e2eEnv } from './e2e/config/env';

/**
 * Expo web user-story suite — see docs/PLAYWRIGHT_PLAN.md
 * Run: npm run test:e2e
 */
const PORT = e2eEnv.port;
const BASE = e2eEnv.baseURL;

export default defineConfig({
  testDir: './e2e/stories',
  testMatch: /.*\.spec\.ts/,
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  timeout: 120_000,
  expect: { timeout: 15_000 },
  reporter: [['list'], ['html', { open: 'never', outputFolder: 'e2e-report' }]],
  use: {
    baseURL: BASE,
    trace: e2eEnv.trace,
    screenshot: 'only-on-failure',
    video: e2eEnv.video,
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
