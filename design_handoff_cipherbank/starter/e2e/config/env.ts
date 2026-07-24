export type TestMode = 'live' | 'mocked';

function bool(name: string, fallback = false): boolean {
  const value = process.env[name];
  return value === undefined ? fallback : value.toLowerCase() === 'true';
}

/** Expo web + scaffold-compatible diagnostics. See docs/E2E_CONFIGURABLES.md */
export const e2eEnv = {
  port: Number(process.env.E2E_PORT ?? 8081),
  baseURL: process.env.E2E_BASE_URL ?? `http://127.0.0.1:${process.env.E2E_PORT ?? 8081}`,
  mode: (process.env.CB_TEST_MODE ?? 'mocked') as TestMode,
  fixtureApiUrl: process.env.CB_FIXTURE_API_URL ?? '',
  fixtureApiToken: process.env.CB_FIXTURE_API_TOKEN ?? '',
  screenshotEachStep: bool('CB_SCREENSHOT_EACH_STEP'),
  pauseAfterStep: process.env.CB_PAUSE_AFTER_STEP ?? '',
  stepDelayMs: Number.parseInt(process.env.CB_STEP_DELAY_MS ?? '0', 10) || 0,
  trace: (process.env.CB_TRACE ?? 'on-first-retry') as
    | 'off'
    | 'on'
    | 'retain-on-failure'
    | 'on-first-retry',
  video: (process.env.CB_VIDEO ?? 'retain-on-failure') as
    | 'off'
    | 'on'
    | 'retain-on-failure'
    | 'on-first-retry',
} as const;
