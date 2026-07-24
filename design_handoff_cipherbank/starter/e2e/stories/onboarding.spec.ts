import { test, expect } from '@playwright/test';
import {
  completeBackupQuiz,
  createNewAccount,
  pressTestId,
  readMnemonicWords,
  resetAppStorage,
} from '../fixtures/onboarding';

test.describe('PW1 onboarding', () => {
  test('US-ONB-01 new user creates account and lands on Home', async ({ page }) => {
    await createNewAccount(page);
    await expect(page.getByTestId('home-screen')).toBeVisible();
    // Clean new-user path should surface first-run setup (sync_meta setup_complete=0).
    await expect(page.getByTestId('home-setup-prompt')).toBeVisible({ timeout: 20_000 });
  });

  test('US-ONB-04 PIN mismatch stays on Set PIN', async ({ page }) => {
    await resetAppStorage(page);
    await page.getByTestId('welcome-screen').waitFor({ timeout: 90_000 });
    await pressTestId(page, 'welcome-create');
    const words = await readMnemonicWords(page);
    await pressTestId(page, 'keys-continue');
    await completeBackupQuiz(page, words);
    await page.getByTestId('set-pin-screen').waitFor({ timeout: 30_000 });
    await page.getByTestId('pin-input').fill('246810');
    await page.getByTestId('pin-confirm').fill('000000');
    await pressTestId(page, 'pin-finish');
    await expect(page.getByTestId('set-pin-screen')).toBeVisible();
    await expect(page.getByText(/PINs do not match/i)).toBeVisible({ timeout: 10_000 });
  });
});
