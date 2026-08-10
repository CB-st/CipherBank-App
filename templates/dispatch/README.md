# Dispatch templates

Use these files to turn a request into a bounded work packet before editing a cross-layer feature. Select one primary workflow from `config/agentic/dispatch.json`, list only the affected ownership contracts, and make verification explicit.

Dispatch packets normally belong under gitignored `artifacts/dispatches/`. Promote a durable decision to `docs/` only when it changes repository architecture or policy.

Use `python3 scripts/create-dispatch.py --help` to select a configured workflow and produce the initial machine-readable packet. Complete the boundary and construction decisions with `DISPATCH.md.template` before implementation.
