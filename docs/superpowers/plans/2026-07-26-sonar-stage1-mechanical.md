# Sonar Stage 1 (mechanical + CRITICAL) — historical record

**Status:** Spent implementation record for audit. Operational truth lives in [`docs/BUILD_LOG.md`](../../BUILD_LOG.md) and root [`AGENTS.md`](../../../AGENTS.md). This file retains only descriptive context; it is not an execution checklist.

**Goal (completed on the stacked MAUI PRs):** Clear Stage 1 Sonar clusters—CRITICAL/HIGH csharp (S1a), explicit types (S1b), ChallengePass file headers (S1c), and member ordering (S1d)—landing fixes on the earliest owning branch and merging upward.

**Architecture (as landed):** Core on `prototype/maui-m1a` / M1b, ChallengePass on `prototype/maui-m2`, Shell on `prototype/maui-m3`, E2E on `prototype/maui-m4`. Mechanical batches used Sonar `issues.json` line maps and targeted edits. Stage 1 excluded SA1402/SA1649 file splits and `IClock` / S6354 redesigns.

**Tech stack:** .NET 10, StyleCop Analyzers, Sonar csharp + external_roslyn, xUnit (`CipherBank-app.Tests`), stacked branches `prototype/maui-m{1a,1b,2,3,4}`.

**Related design:** `docs/superpowers/specs/2026-07-26-sonar-stage1-mechanical-design.md`

## Constraints that governed Stage 1

- Fixes landed on the earliest owning layer, then merged up (shared Core smells were not parked only on M4).
- IDE0008 preferred over IDE0007 for the mechanical pass.
- Expo `design_handoff_cipherbank/` stayed out of the MAUI merge path.
- Cross-PR duplication comments only when a later PR re-touched an earlier fix site.

## PR map (formal stack)

| Layer | Branch | PR |
|-------|--------|----|
| M1a Core / Sonar gate | `prototype/maui-m1a` | [#25](https://github.com/CB-st/CipherBank-App/pull/25) |
| M1b docs / harness scaffold | `prototype/maui-m1b` | [#26](https://github.com/CB-st/CipherBank-App/pull/26) |
| M2 ChallengePass | `prototype/maui-m2` | [#21](https://github.com/CB-st/CipherBank-App/pull/21) |
| M3 Shell | `prototype/maui-m3` | [#22](https://github.com/CB-st/CipherBank-App/pull/22) |
| M4 E2E | `prototype/maui-m4` | [#23](https://github.com/CB-st/CipherBank-App/pull/23) |

Stage 2 structural StyleCop (SA1402 / SA1649) was deferred to `docs/SONAR_STRUCTURAL_PLAN.md`.
