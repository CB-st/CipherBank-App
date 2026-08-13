# CipherBank agent dispatch

CipherBank work is routed as a bounded dispatch rather than a broad instruction to edit the entire stack. The dispatcher identifies ownership first, loads only the contracts for the affected layers, selects copy-ready templates, and records the verification evidence required for handoff.

## Dispatch lifecycle

1. Classify the request with `config/agentic/dispatch.json`.
2. Read the root `AGENTS.md`, then only the nearest owning subtree contracts.
3. Record scope, invariants, dependencies, resource reuse, and gates with `templates/dispatch/`.
4. Apply the focused build skill and its templates.
5. Register implementations only at the owning composition root.
6. Run the narrow gate, the architecture gate, then any platform/device gate.
7. Report changed ownership boundaries, resource additions, and verification gaps.

Create a deterministic routing packet without editing product source:

```bash
python3 scripts/create-dispatch.py \
  --workflow feature-slice \
  --feature MerchantSettlement \
  --summary "Add merchant settlement status and retry handling"
```

The command prints JSON by default. Pass `--output artifacts/dispatches/merchant-settlement.json` to retain it locally; existing files are never overwritten unless `--force` is explicit.

Use `cipherbank-dispatch` when the request crosses layers or ownership is unclear. Directly use a focused skill when the boundary is already explicit:

| Skill | Primary responsibility |
| --- | --- |
| `cipherbank-build-feature` | Core service contracts, implementations, adapters, configuration, and explicit DI modules |
| `cipherbank-build-ui` | MAUI pages/ViewModels, semantic typography/color, shared or feature-local resources |
| `cipherbank-build-data` | EF Core entities/repositories/migrations and isolated compatibility SQL |
| `cipherbank-build-e2e` | Appium page objects, stable story traits, device state, diagnostics, and gap evidence |
| `cipherbank-validate-stack` | Structural, unit, build, Sonar, platform, and E2E verification |

## Dispatch boundaries

- A dispatcher may select several focused workflows but each implementation step has one owning layer.
- A work packet is evidence, not product configuration. Keep generated packets under gitignored `artifacts/dispatches/` unless a long-lived architectural decision belongs in `docs/`.
- Do not bundle secrets, customer data, device credentials, recovery phrases, or private endpoints into a dispatch.
- If a requested shortcut violates custody, ChallengePass, persistence, or E2E invariants, stop and surface the conflict instead of weakening the contract.

See [MODULE_COMPOSITION.md](MODULE_COMPOSITION.md) for DI and feature registration. See [RESOURCE_OWNERSHIP.md](RESOURCE_OWNERSHIP.md) for placement and access rules.
