import { test, expect } from '@playwright/test';
import { resetAppStorage } from '../fixtures/onboarding';

/**
 * PW0 smoke — app boots to Welcome (clean) or Unlock (existing custody).
 */
test.describe('PW0 shell smoke', () => {
  test('US-SMOKE-01 app loads Welcome or Unlock', async ({ page }) => {
    await resetAppStorage(page);

    const welcome = page.getByTestId('welcome-screen');
    const unlock = page.getByText(/Unlock|CipherBank PIN|fingerprint|Use CipherBank PIN/i);

    await expect
      .poll(async () => {
        const w = await welcome.count();
        const u = await unlock.count();
        return w + u > 0;
      }, { timeout: 90_000 })
      .toBeTruthy();
  });
});
