# Agentic workflow configuration

`dispatch.json` routes a requested change to the smallest CipherBank skill, the repository contracts it must read, the scaffolds it may copy, and the gates it must run.

This configuration is repository tooling only. It is not embedded in a product assembly and must never contain credentials, customer data, private endpoints, keys, PINs, recovery phrases, or environment-specific secrets.

## Editing rules

- Keep workflow IDs stable because dispatch packets may cite them.
- A workflow names one primary skill. Use `followUps` for independently required UI, data, E2E, or validation work.
- Every referenced path must exist in the repository.
- List the narrowest gate first and the full structural gate last.
- Update `docs/agentic/README.md`, the affected templates, and architecture tests when a workflow changes meaning.
