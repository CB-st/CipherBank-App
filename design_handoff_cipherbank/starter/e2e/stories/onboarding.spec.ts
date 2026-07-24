import { test, expect } from '@playwright/test';
import { tid } from '../config/selectors';
import { story } from './catalog';
import { annotateStory, runStoryStep } from '../support/story-runner';
import {
  completeBackupQuiz,
  pressTestId,
  readMnemonicWords,
  resetAppStorage,
} from '../fixtures/onboarding';

const createAccount = story('CB-ACCOUNT-001');

test.describe('PW1 onboarding', () => {
  test(`${createAccount.id} / US-ONB-01 — ${createAccount.title}`, async ({ page }, testInfo) => {
    annotateStory(testInfo, createAccount);
    const pin = '246810';
    let words: string[] = [];

    await runStoryStep(page, testInfo, createAccount, 'open', async () => {
      await resetAppStorage(page);
      await expect(page.getByTestId(tid.account.welcomeScreen)).toBeVisible({ timeout: 90_000 });
    });

    await runStoryStep(page, testInfo, createAccount, 'complete-form', async () => {
      // Expo custody onboarding has no email/password form — CTAs are the required agreements surface.
      await expect(page.getByTestId(tid.account.createSubmit)).toBeVisible();
      await expect(page.getByTestId(tid.account.recoverEntry)).toBeVisible();
    });

    await runStoryStep(page, testInfo, createAccount, 'submit', async () => {
      await pressTestId(page, tid.account.createSubmit);
      await expect(page.getByTestId(tid.account.recoveryMaterial)).toBeVisible({ timeout: 60_000 });
    });

    await runStoryStep(page, testInfo, createAccount, 'backup', async () => {
      words = await readMnemonicWords(page);
      expect(words).toHaveLength(12);
      await pressTestId(page, tid.account.keysContinue);
      await completeBackupQuiz(page, words);
    });

    await runStoryStep(page, testInfo, createAccount, 'complete', async () => {
      await page.getByTestId(tid.account.setPinScreen).waitFor({ timeout: 30_000 });
      await page.getByTestId(tid.account.pinInput).fill(pin);
      await page.getByTestId(tid.account.pinConfirm).fill(pin);
      await pressTestId(page, tid.account.pinFinish);
      await expect(page.getByTestId(tid.account.createSuccess)).toBeVisible({ timeout: 60_000 });
      await expect(page.getByTestId(tid.account.setupPrompt)).toBeVisible({ timeout: 20_000 });
    });
  });

  test('US-ONB-04 PIN mismatch stays on Set PIN', async ({ page }) => {
    await resetAppStorage(page);
    await page.getByTestId(tid.account.welcomeScreen).waitFor({ timeout: 90_000 });
    await pressTestId(page, tid.account.createSubmit);
    const words = await readMnemonicWords(page);
    await pressTestId(page, tid.account.keysContinue);
    await completeBackupQuiz(page, words);
    await page.getByTestId(tid.account.setPinScreen).waitFor({ timeout: 30_000 });
    await page.getByTestId(tid.account.pinInput).fill('246810');
    await page.getByTestId(tid.account.pinConfirm).fill('000000');
    await pressTestId(page, tid.account.pinFinish);
    await expect(page.getByTestId(tid.account.setPinScreen)).toBeVisible();
    await expect(page.getByText(/PINs do not match/i)).toBeVisible({ timeout: 10_000 });
  });
});
