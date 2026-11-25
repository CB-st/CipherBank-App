# Documentation Conventions

CipherBank-App documentation is organized from architecture to areas to focused component references. Keep existing guides useful while adding component pages incrementally; this convention does not require a generator.

## Page levels and paths

- **L1 — Architecture:** system boundaries and major flows live in [`architecture.md`](architecture.md). Keep diagrams in Mermaid when they clarify the design.
- **L2 — Area indexes:** each area `README.md` summarizes its scope and links to its topic guides and available component references.
- **L3 — Type or component pages:** add a page when a stable component has a contract useful to maintainers or callers. Mirror the conceptual source area at `docs/<area>/<Type>.md`; for example, a future Core model reference could be `docs/core/Wallet.md`.
- **L4 — Member contracts:** create separate pages only when a non-trivial public method, input, or behavior needs its own navigation target. Trivial accessors do not need pages.
- **L5 — Examples and validation recipes:** keep concrete examples, commands, or representative tests linked from the relevant component page. Existing test guides and operational guides remain in their area.

Existing topic pages such as `docs/core/models.md` and `docs/app/services.md` remain area-level guides. New type pages should be linked from their L2 index; add a link only when the page exists so offline link checks remain meaningful. Area indexes may identify future references as planned paths in text.

## Component page template

Use the sections below for new type or component pages. Mark a section as not applicable when that makes a contract clearer; do not invent behavior to fill a heading.

```markdown
# <Type or component>

## Purpose
<What this component is for.>

## Source
- `<path to source>`

## Contract
<Responsibilities, public behavior, invariants, or externally visible effects.>

## Dependencies/Inputs
<Collaborators, configuration, arguments, or external inputs.>

## Failure behavior
<Errors, cancellation, fallback, or partial effects; state when none are relevant.>

## Tests/Validation
- `<test path, validation command, or CI check>`

## Related docs
- [<related page>](<relative link>)
```

Use relative Markdown links and verify their targets exist. Link to the test project, test class, CI check, or offline validation command that covers non-trivial behavior. Keep contracts concise and specific to the component.
