import type { Page } from '@playwright/test';

/**
 * Clear web storage before the app boots so expo-sqlite / AsyncStorage start clean.
 * Deleting IndexedDB while the app holds open handles leaves setup flags stale.
 */
export async function resetAppStorage(page: Page) {
  await page.goto('about:blank');
  await page.evaluate(async () => {
    try {
      localStorage.clear();
      sessionStorage.clear();
    } catch {
      /* ignore */
    }
    try {
      if (indexedDB?.databases) {
        const dbs = await indexedDB.databases();
        await Promise.all(
          dbs
            .filter((d) => d.name)
            .map(
              (d) =>
                new Promise<void>((resolve) => {
                  const req = indexedDB.deleteDatabase(d.name!);
                  req.onsuccess = () => resolve();
                  req.onerror = () => resolve();
                  req.onblocked = () => setTimeout(resolve, 750);
                }),
            ),
        );
      }
    } catch {
      /* ignore */
    }
  });
  await page.goto('/');
  await page.waitForLoadState('domcontentloaded');
}

export async function readMnemonicWords(page: Page): Promise<string[]> {
  await page.getByTestId('keys-screen').waitFor({ timeout: 60_000 });
  const words: string[] = [];
  for (let i = 0; i < 12; i++) {
    const text = (await page.getByTestId(`keys-word-text-${i}`).innerText()).trim();
    words.push(text);
  }
  return words;
}

export async function completeBackupQuiz(page: Page, words: string[]) {
  await page.getByTestId('quiz-screen').waitFor({ timeout: 30_000 });
  const prompts = page.locator('[data-testid^="quiz-prompt-"]');
  const count = await prompts.count();
  for (let i = 0; i < count; i++) {
    const prompt = prompts.nth(i);
    const testId = await prompt.getAttribute('data-testid');
    const idx = Number(testId?.replace('quiz-prompt-', ''));
    if (!Number.isFinite(idx)) continue;
    await page.getByTestId(`quiz-answer-${idx}`).fill(words[idx] ?? '');
  }
  await pressTestId(page, 'quiz-continue');
}

/** RN-web Pressable: prefer role+testID intersection. */
export async function pressTestId(page: Page, testId: string) {
  const byRole = page.getByRole('button', { name: /./ }).and(page.getByTestId(testId));
  const target = (await byRole.count()) > 0 ? byRole.first() : page.getByTestId(testId);
  await target.waitFor({ state: 'visible', timeout: 30_000 });
  await target.click({ timeout: 15_000 });
}

export async function createNewAccount(page: Page, pin = '246810') {
  await resetAppStorage(page);
  await page.getByTestId('welcome-screen').waitFor({ timeout: 90_000 });
  await pressTestId(page, 'welcome-create');
  const words = await readMnemonicWords(page);
  await pressTestId(page, 'keys-continue');
  await completeBackupQuiz(page, words);
  await page.getByTestId('set-pin-screen').waitFor({ timeout: 30_000 });
  await page.getByTestId('pin-input').fill(pin);
  await page.getByTestId('pin-confirm').fill(pin);
  await pressTestId(page, 'pin-finish');
  await page.getByTestId('home-screen').waitFor({ timeout: 60_000 });
  return { words, pin };
}
