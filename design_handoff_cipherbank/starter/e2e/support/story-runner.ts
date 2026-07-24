import { test, type Page, type TestInfo } from '@playwright/test';
import { e2eEnv } from '../config/env';
import type { StoryDefinition, StoryStep } from '../domain/types';

export async function runStoryStep(
  page: Page,
  testInfo: TestInfo,
  storyDef: StoryDefinition,
  stepId: string,
  body: (step: StoryStep) => Promise<void>,
): Promise<void> {
  const step = storyDef.steps.find((candidate) => candidate.id === stepId);
  if (!step) throw new Error(`Story ${storyDef.id} has no step named ${stepId}`);

  await test.step(`${storyDef.id}.${step.id} — ${step.action}`, async () => {
    await body(step);
    if (e2eEnv.screenshotEachStep) {
      const safeName = `${storyDef.id}-${step.id}`.replace(/[^a-zA-Z0-9-_]/g, '-');
      await testInfo.attach(safeName, {
        body: await page.screenshot({ fullPage: true }),
        contentType: 'image/png',
      });
    }

    if (e2eEnv.stepDelayMs > 0) {
      await page.waitForTimeout(e2eEnv.stepDelayMs);
    }

    if (e2eEnv.pauseAfterStep === `${storyDef.id}.${step.id}`) {
      await page.pause();
    }
  });
}

export function annotateStory(testInfo: TestInfo, storyDef: StoryDefinition): void {
  testInfo.annotations.push(
    { type: 'story', description: `${storyDef.id}: ${storyDef.title}` },
    { type: 'source-diagram', description: storyDef.sourceDiagram },
    { type: 'actor', description: storyDef.actor },
  );
}
