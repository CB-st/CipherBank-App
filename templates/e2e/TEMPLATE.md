# New executable E2E story checklist

- [ ] Story ID is a stable `CB-*` or `US-*` constant and appears in the catalog/map
- [ ] Wave mapping uses the same ID and `Story=` filtering
- [ ] Test method has `[Trait("Story", StoryIds.*)]`
- [ ] `E2E_RUN` absent produces a real skipped result
- [ ] Enabled execution establishes the declared device profile and fails on missing prerequisites
- [ ] Story body runs through `StoryRunner` so failures create a gap note and rethrow
- [ ] Selectors exist only in a page object and use stable automation IDs
- [ ] Page object methods state purpose, frequency, and scope
- [ ] Blocking emulator/Appium/ADB lifecycle work remains outside the fact body
- [ ] Lab secrets come from environment or gitignored `artifacts/`, never literals
- [ ] Host contract tests and `--list-tests --filter "Story=..."` find the story
- [ ] The relevant Android wave passes from the repository root
