# Placing scoped AGENTS.md files

The installer creates a root `AGENTS.md` only when one does not already exist. It never guesses which existing project is Core, Application, Infrastructure, or Host. Copy and adapt templates after reviewing project responsibilities.

## Recommended hierarchy

```text
repository/
  AGENTS.md
  src/
    AGENTS.md
    Product.Core/AGENTS.md
    Product.Application/AGENTS.md
    Product.Infrastructure/AGENTS.md
    Product.Api/AGENTS.md
    Product.UI/AGENTS.md
  tests/
    AGENTS.md
```

## Mapping templates

| Observed responsibility | Template |
|---|---|
| Domain values, calculations, invariants, ports | `AGENTS.core.md` |
| Use-case orchestration and transaction intent | `AGENTS.application.md` |
| EF, HTTP, files, queues, telemetry, device adapters | `AGENTS.infrastructure.md` |
| API/worker startup and composition (or an executable head with no separate UI project) | `AGENTS.host.md` |
| Views, view models/components, bindings, commands, dispatcher/UI-thread boundaries | `AGENTS.ui.md` |
| SonarQube profile/gate ownership and analyzer suppression governance | `AGENTS.sonar.md` |
| Profiled numeric hot paths using masked selection, SIMD, or branch-removal candidates | `AGENTS.branchless.md` |
| Unit, integration, contract, or E2E projects | `AGENTS.tests.md` |

When a desktop/mobile project carries both the composition root and the views/view models — common in small apps — apply both `AGENTS.host.md` and `AGENTS.ui.md` to it; one project is playing two roles.

Apply `AGENTS.branchless.md` only to the narrow performance-critical subtree or benchmark project. It is not a general Core/application style contract. Keep `BRANCHLESS-PERFORMANCE-RECORD.md` with the change evidence or link it from the pull request/ADR system.

Memory-pooling and GPU-kernel code do not get a dedicated template of their own; they follow the same layer rules as everything else. A vectorized or memory-pooled routine that stays a pure, deterministic function of its inputs is Core (`AGENTS.core.md`); a GPU compute kernel is a device adapter and belongs in Infrastructure (`AGENTS.infrastructure.md`) behind a Core-defined port, even when the invoking library is small. See `SIMD-AND-VECTORIZATION.md`, `MEMORY-COMPUTE.md`, and `GPU-COMPUTE.md`.

## Merge rules

1. Preserve repository-specific build, security, licensing, and deployment requirements.
2. Add the overlay contract only where it strengthens or clarifies responsibility.
3. Resolve contradictions explicitly; do not leave two incompatible commands or ownership rules.
4. Keep root rules universal and local rules specific.
5. Verify every named path and command after placement.

Do not assign a layer based only on a project name. Inspect actual dependencies and behavior first.
