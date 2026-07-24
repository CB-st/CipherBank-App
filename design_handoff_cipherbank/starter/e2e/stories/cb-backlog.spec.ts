import { test } from '@playwright/test';
import { storyCatalog } from './catalog';

/**
 * Visible backlog for CB-* stories not yet executable against Expo Cora.
 * Convert each fixme into a real spec when UI testIDs + mock/fixture hooks land.
 * See docs/STORY_ID_MAP.md.
 */
const executableNow = new Set(['CB-ACCOUNT-001']);

for (const userStory of storyCatalog) {
  if (executableNow.has(userStory.id)) continue;

  if (userStory.id === 'CB-PREPAID-PLACEHOLDER' || userStory.steps.length === 0) {
    test.skip(`${userStory.id} — ${userStory.title} (blank source diagram)`, async () => {
      /* Make Prepaid Purchase.drawio is empty */
    });
    continue;
  }

  test.fixme(`${userStory.id} — ${userStory.title}`, async () => {
    // Happy path: implement with runStoryStep once selectors + surfaces exist.
  });

  for (const negativeCase of userStory.negativeCases) {
    test.fixme(`${userStory.id} negative — ${negativeCase}`, async () => {
      // Failure injection after happy-path selectors are stable.
    });
  }
}

// Negatives for executable stories still awaiting injection hooks
const createAccount = storyCatalog.find((s) => s.id === 'CB-ACCOUNT-001');
if (createAccount) {
  for (const negativeCase of createAccount.negativeCases) {
    test.fixme(`${createAccount.id} negative — ${negativeCase}`, async () => {
      // US-ONB-04 covers PIN mismatch locally; remaining negatives TBD.
    });
  }
}
