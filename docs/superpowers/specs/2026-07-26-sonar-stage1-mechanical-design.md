# Sonar Stage 1–3 remediation design — historical record

**Status:** Spent design record for audit. Operational truth lives in [`docs/BUILD_LOG.md`](../../BUILD_LOG.md), root [`AGENTS.md`](../../../AGENTS.md), and [`docs/SONAR_GATE.md`](../../SONAR_GATE.md). This file retains only descriptive context; it is not an execution checklist and does not prescribe agent bash verification steps.

**Date:** 2026-07-26 (authored); updated as the formal stack landed on `prototype/maui-m{1a,1b,2,3,4}`.

**Related plan (historical):** `docs/superpowers/plans/2026-07-26-sonar-stage1-mechanical.md`

## Goal (as designed)

Clear Sonar **new-code** gate noise on the MAUI stack in three stages: mechanical + CRITICAL first, planned structural file splits second, then medium/minor/info. Prefer fixing once on the owning layer and merging upward.

## Out of scope (this program)

- Softening server gate thresholds (documented in `docs/SONAR_GATE.md`).
- Introducing `IClock` / S6354 across Core+Shell (deferred unless Stage 3 explicitly picks it up).
- Committing `design_handoff_cipherbank/` or Expo paths.
- TreatWarningsAsErrors wholesale disable; permanent WNAE parking for correctness rules.

## Landing strategy (as applied)

**Fix on the earliest owning branch, then merge up.**

| Concern | Land on | Merge through |
|---------|---------|---------------|
| Core / shared Tests | `prototype/maui-m1a` / M1b ([#25](https://github.com/CB-st/CipherBank-App/pull/25)–[#26](https://github.com/CB-st/CipherBank-App/pull/26)) | m2 → m3 → m4 |
| ChallengePass (+ its tests) | `prototype/maui-m2` ([#21](https://github.com/CB-st/CipherBank-App/pull/21)) | m3 → m4 |
| Shell-only | `prototype/maui-m3` ([#22](https://github.com/CB-st/CipherBank-App/pull/22)) | m4 |
| E2E-only | `prototype/maui-m4` ([#23](https://github.com/CB-st/CipherBank-App/pull/23)) | — |

Shared Core smells were not parked only on M4 tip.

## Cross-PR duplication annotation (policy that governed the stack)

When a Sonar finding originated on an earlier layer but the **canonical fix** landed later, duplicate or leftover edit sites received:

```csharp
// Sonar: issue resolved in M{N} PR (https://github.com/CB-st/CipherBank-App/pull/{N}/…), edit here is duplication
```

Routine mechanical edits that only merged upward without re-editing were not annotated.

## Stage 1 — mechanical style + CRITICAL (completed pattern)

Hand-fix clusters that Stage 1 targeted (descriptive, not a work queue):

| Cluster | Typical rules | Pattern used |
|---------|---------------|--------------|
| S1a CRITICAL / HIGH csharp | S2339, S2360, S1541, S1067, S2302, S2365, S131 | Named properties / overloads / helper extracts — no optional params on abstract interface slots |
| S1b IDE0008 | explicit types on reported lines | Prefer IDE0008 over IDE0007 for that pass |
| S1c SA1636 | file headers | Align with each project `stylecop.json` |
| S1d SA1201 / SA1202 / SA1204 | member order | Reorder within files; no type splits in Stage 1 |

Stage 1 explicitly deferred SA1402 / SA1649, bulk MEDIUM/LOW/INFO, and S6354.

Verification happened through the stack’s usual gates (`dotnet test` on Core tests; Shell Android build when public surface moved). Re-scans used CI `sonar-context` artifacts — this document does not restate shell snippets as runnable checklists.

## Stage 2 — structural rebuild (plan-gated)

SA1402 / SA1649 moves required `docs/SONAR_STRUCTURAL_PLAN.md` (callers across Shell, Tests, E2E) before code splits. Prefer split-only, same namespace; filename matches primary type.

## Stage 3 — medium / minor / info

After Stage 2 (or only for pure mechanical non-structural items if Stage 2 was review-blocked): S109 protocol/crypto literals, culture/ternary signal, specific catches — with soften rows documented in `SONAR_GATE.md` rather than fake-fixed.

## Success criteria (how “done” was judged)

| Stage | Done when |
|-------|-----------|
| 1 | M1–M3 HIGH/CRITICAL csharp clusters cleared or justified; mechanical volume materially down on re-scan; tests green; duplication comments only where later PRs superseded earlier sites |
| 2 | Structural plan approved; splits merged; SA1402/SA1649 cleared or explicitly waived |
| 3 | Remaining MEDIUM/LOW/INFO fixed or mapped to soften rows; coverage tracked separately |

## Risks that shaped the work

- Member reordering churned blame across stacked review — batches kept focused.
- S2339 on public const could change attribute/`const` usage — preferred read-only properties with same literals.
- Stack rebase/merge conflicts: resolve toward earliest owning tip; restore duplication comments if markers drop them.
