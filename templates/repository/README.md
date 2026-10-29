# Repository-layer templates

This package is for adding a new bounded layer or feature subtree while preserving CipherBank's documentation and agent contracts.

- Copy `AGENTS.md.template` to the new subtree as `AGENTS.md` and replace its boundary rules.
- Copy `README.md.template` to the subtree as `README.md` and link it from `docs/README.md`.
- Complete `TEMPLATE.md` before implementation and keep it with the design/review notes when the feature lands.

Subtree contracts may tighten the root rules; they may not weaken security, package, persistence, configuration, or verification gates.
