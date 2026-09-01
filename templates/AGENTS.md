# Template maintenance contract

Templates encode the current repository architecture. They are not historical examples.

- Keep placeholders obvious and language-neutral, such as `__FEATURE__` and `__NAMESPACE__`.
- A new capability template includes its interface, implementation, DI registration point, configuration ownership, and test seam.
- UI templates use design-system tokens and named styles; they never introduce literal colors or ad hoc typography.
- E2E templates keep selectors in page objects, attach stable story traits, use the shared fixture, and route failures through `StoryRunner`.
- When a repository rule changes, update the affected template and its README in the same change.
- Templates remain non-compiling scaffolds through the `.template` extension.
- Dispatch templates describe ownership, selected skills, dependencies, resources, and gates; they do not contain secrets or production data.
- Feature-module templates register interfaces to implementations explicitly and never use reflection, assembly scanning, dependency bags, or service locators.
- Resource templates consume global semantic tokens. Promote a feature-local token to the shared dictionaries only after it has a stable cross-feature role.
