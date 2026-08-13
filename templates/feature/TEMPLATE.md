# New feature module checklist

- [ ] The feature has one owner and explicit inward dependencies
- [ ] Core contracts contain no MAUI or platform types
- [ ] Each runtime service implements a focused interface
- [ ] The module binds typed options and calls `ValidateOnStart` when configuration is required
- [ ] Lifetimes match retained state and thread-safety
- [ ] The module is called once from the MAUI composition root
- [ ] Shared resources are referenced from their canonical owner
- [ ] Feature-local resources are registered at the smallest common scope
- [ ] Unit tests cover service behavior and registration resolution
- [ ] Appium coverage exists for user-visible behavior
- [ ] Structural and target-platform gates pass
