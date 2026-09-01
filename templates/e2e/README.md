# Appium E2E templates

Use these scaffolds together when promoting a story from the catalog to executable coverage. The page object owns selectors and interactions; the story test owns intent, device-profile setup, and assertions.

| File | Purpose |
| --- | --- |
| `PageObject.cs.template` | A focused Appium screen boundary with stable automation IDs |
| `StoryTest.cs.template` | A `Story`-traited device fact wrapped by `StoryRunner` |
| `TEMPLATE.md` | Copy procedure and definition of done |

Replace every `__TOKEN__`, add the story to `StoryIds`, `StoryCatalog`, `WaveStories`, and `docs/tests/STORY_ID_MAP.md`, then run trait discovery before a device run.
