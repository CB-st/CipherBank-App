# E2E configurables (Expo Cora)

Companion to the UserStories Playwright scaffold `docs/CONFIGURABLES.md` (source of truth for CB_* knobs). This tree runs Playwright against **Expo web** with in-process mocks.

---

## Expo / Metro (webServer)

Set via `playwright.config.ts` `webServer.env` (defaults below). Override in the shell when needed.

| Variable | Default in e2e | Purpose |
|----------|----------------|---------|
| `E2E_PORT` | `8081` | Metro / Expo web port |
| `E2E_BASE_URL` | `http://127.0.0.1:${E2E_PORT}` | Playwright `baseURL` |
| `EXPO_PUBLIC_USE_MOCK` | `true` | In-process `src/mocks/handlers` — no live `/v1` |
| `EXPO_PUBLIC_SEED_DEMO` | `false` | Clean OOTB (no demo custody / rich portfolio) |
| `EXPO_PUBLIC_MOCK_HAS_WALLET` | `false` | Force Welcome / onboarding |
| `CI` | `1` (in webServer) | Quieter Expo; also disables `reuseExistingServer` when set for the test process |

---

## Scaffold-compatible diagnostics (`CB_*`)

Mirrored in `e2e/config/env.ts` and wired into `playwright.config.ts` / `story-runner`.

| Variable | Default | Purpose |
|----------|---------|---------|
| `CB_TEST_MODE` | `mocked` | Expo default is in-app mock; `live` reserved for future `@live` project |
| `CB_SCREENSHOT_EACH_STEP` | `false` | Attach PNG after each `runStoryStep` — **keep off** near Keys/Quiz |
| `CB_PAUSE_AFTER_STEP` | _(empty)_ | e.g. `CB-ACCOUNT-001.backup` |
| `CB_STEP_DELAY_MS` | `0` | Delay after each step |
| `CB_TRACE` | `on-first-retry` | Playwright trace mode |
| `CB_VIDEO` | `retain-on-failure` | Video retention |
| `CB_FIXTURE_API_URL` / `CB_FIXTURE_API_TOKEN` | _(empty)_ | Future fixture service — see [`FIXTURE_API_CONTRACT.md`](./FIXTURE_API_CONTRACT.md) |

---

## Selectors

Central map: [`e2e/config/selectors.ts`](../e2e/config/selectors.ts).

| `tid` key | Expo `testID` today |
|-----------|---------------------|
| `tid.account.createSubmit` / open | `welcome-create` |
| `tid.account.recover` entry | `welcome-returning` |
| `tid.account.recoveryMaterial` | `keys-screen` / `keys-word-text-*` |
| `tid.account.createSuccess` destination | `home-screen` |
| setup card | `home-setup-prompt` |
| quiz / pin | `quiz-*`, `pin-input`, `pin-confirm`, `pin-finish` |

Unmapped scaffold IDs (wallets, cards, payments, market) stay as **target** strings until UI ships `testID`s — do not invent CSS selectors.

---

## Routes

[`e2e/config/routes.ts`](../e2e/config/routes.ts) holds Playwright intercept globs for a future live mode.

**Mock mode (default):** the app does **not** hit those HTTP paths — `apiClient` routes through `src/mocks/handlers`. Do not rely on `page.route()` for happy-path onboarding.

**Live mode (later):** map globs to CipherBank `/v1` + public `api.cipherbank.money` patterns from [`PUBLIC_API.md`](./PUBLIC_API.md) / [`API_BUILD_PLAN.md`](./API_BUILD_PLAN.md).

---

## Security

- Never attach recovery phrases or PIN values in screenshots/traces.
- `readMnemonicWords` is for quiz input only — do not log or `testInfo.attach` the phrase.
- Synthetic PIN in tests (e.g. `246810`); never production credentials.

---

## Related

- [`STORY_ID_MAP.md`](./STORY_ID_MAP.md)
- [`USER_STORIES.md`](./USER_STORIES.md)
- [`PLAYWRIGHT_PLAN.md`](./PLAYWRIGHT_PLAN.md)
- [`TESTING.md`](./TESTING.md)
