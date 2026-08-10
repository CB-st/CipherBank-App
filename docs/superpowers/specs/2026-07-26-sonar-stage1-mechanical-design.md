# Sonar Stage 1–3 remediation design

**Date:** 2026-07-26  
**Stack:** `prototype/maui-m1` → `m2` → `m3` → `m4` (PRs [#20](https://github.com/CB-st/CipherBank-App/pull/20)–[#23](https://github.com/CB-st/CipherBank-App/pull/23))  
**Source inventory:** CI `sonar-context` artifacts (M1 `@8a04b58`, M2 `@d5c3cd4`, M3 `@f063be0`, M4 `@068886b`)  
**Triage canvas:** workspace `canvases/sonar-m1-m4-triage.canvas.tsx`

## Goal

Clear Sonar **new-code** gate noise on the MAUI stack in three stages: mechanical + CRITICAL first, planned structural file splits second, then medium/minor/info. Prefer fixing once on the owning layer and merging upward.

## Out of scope (this program)

- Softening server gate thresholds (documented separately in `docs/SONAR_GATE.md` when present on the branch).
- Introducing `IClock` / S6354 across Core+Shell (deferred unless Stage 3 explicitly picks it up).
- Committing `design_handoff_cipherbank/` or Expo paths.
- Pushing `.github/workflows/sonar.yml` Coverlet changes until a `workflow`-scoped token is available (coverage remains a separate blocker).

## Landing strategy

**Fix on the earliest owning branch, then merge up.**

| Concern | Land on | Merge through |
|---------|---------|---------------|
| Core / shared Tests | `prototype/maui-m1` (#20) | m2 → m3 → m4 |
| ChallengePass (+ its tests) | `prototype/maui-m2` (#21) | m3 → m4 |
| Shell-only / M3-delta Core edits already unique to m3 | `prototype/maui-m3` (#22) | m4 |
| E2E-only (M4 currently 0 Sonar new issues) | `prototype/maui-m4` (#23) | — |

Do **not** dump Stage 1-only fixes solely onto M4 tip; stacked PR new-code would stay red on M1–M3.

## Cross-PR duplication annotation (required)

When a Sonar finding originates on **M1** (or an earlier stack layer) but the **canonical fix, relocation, or removal** lands on **M2–M4**, leave a comment on any **duplicate or leftover edit site** so reviewers know not to re-fix the same smell on the earlier PR.

**Exact comment form (C#):**

```csharp
// Sonar: issue resolved in M{N} PR (https://github.com/CB-st/CipherBank-App/pull/{N}/…), edit here is duplication
```

Rules:

1. `{N}` is the PR that owns the **canonical** fix: usually **2**, **3**, or **4**. When the canonical fix is on **M1** and a **later** PR only re-touches the same site, `{N}` may be **1** and the comment lives on the later duplicate edit.
2. Deep-link the resolving change when possible:
   - Prefer commit on that PR: `https://github.com/CB-st/CipherBank-App/pull/{N}/commits/{sha}`
   - Or blob+lines on the resolving branch: `https://github.com/CB-st/CipherBank-App/blob/{sha}/path#Lstart-Lend`
   - PR URL alone is allowed temporarily; update to commit/blob once the resolving commit exists.
3. **Where to put it:**
   - On the **later** edit that re-touches or re-implements the same fix (“edit here is duplication”).
   - And/or on **leftover M1 code** that still shows the old smell because the real resolution only exists further up the stack (so M1 reviewers see the pointer).
4. Do **not** add this on the canonical first fix when that fix is applied on the owning earliest branch with no later duplicate.
5. Do **not** spam it on routine mechanical IDE0008/header/order edits that are applied once on M1 and merely merged upward without re-editing.
6. Stage 2 type splits: annotate both the **old multi-type file** (leftover) and any **follow-up PR** that only adjusts usings after the split PR, pointing at the PR that performed the move.

PR bases:

| Layer | PR |
|-------|----|
| M1 | https://github.com/CB-st/CipherBank-App/pull/20 |
| M2 | https://github.com/CB-st/CipherBank-App/pull/21 |
| M3 | https://github.com/CB-st/CipherBank-App/pull/22 |
| M4 | https://github.com/CB-st/CipherBank-App/pull/23 |

## Stage 1 — mechanical style + CRITICAL (execute first)

### S1a — CRITICAL / HIGH csharp (gate)

Hand-fix; no StyleCop file splits.

| Rule | Where | Fix |
|------|-------|-----|
| S2339 | Core `AchRecipientValidation`; ChallengePass consts | `public const` → `public static` read-only property (or equivalent API-safe shape) |
| S2360 | ChallengePass optional params | Overloads instead of optional parameters |
| S1541 | `PaymentUri` | Reduce cyclomatic complexity (extract helpers) |
| S1067 | `MnemonicBackupService` | Split compound condition |
| S2302 | ChallengePass templates | `nameof(...)` |
| S2365 | `ChallengePassCatalog.AvailableSuiteIds` | Method instead of copying property |
| S131 | `WireEncoding` | Add `default` to switch |

### S1b — IDE0008 (`var` → explicit type)

- Apply explicit types where Sonar reports IDE0008.
- Convention for this pass: **IDE0008 wins** over IDE0007 (prefer `var`); do not flip-flop.
- Prefer targeted edits on reported lines; avoid repo-wide blind replace that breaks `var` used for anonymous/tuple cases Sonar did not flag.

### S1c — SA1636 file headers

- Align file header copyright text with each project’s `stylecop.json` (`companyName`: CipherBank).
- Primary volume: ChallengePass (~23 MAJOR). Apply the same header template used by Core when adding/fixing headers.
- Headers alone do not clear HIGH; still required for Stage 1 mechanical cleanup.

### S1d — SA1201 / SA1202 / SA1204 member order

- Properties before methods; static before instance; public before private (per StyleCop ordering).
- Reorder within existing files only—**do not** split types in Stage 1.

### Stage 1 verification

```bash
export PATH="$HOME/.local/dotnet:$PATH"
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release --nologo
# On M1-only tip, ChallengePass filter may still apply if project absent:
# --filter "FullyQualifiedName!~ChallengePass"
```

Push owning branch tips after green tests. Re-fetch `sonar-context` when CI finishes to confirm HIGH drop.

### Stage 1 explicit non-goals

- SA1402 (one type per file), SA1649 (file name = type)
- Medium/minor/info bulk (S109, S4056, S2221, IDE002*, S4055, …)
- S6354 clock injection

## Stage 2 — structural rebuild (plan, then execute)

### Deliverable before any split

Create `docs/SONAR_STRUCTURAL_PLAN.md` containing:

1. Every SA1402 / SA1649 finding (file, types in file, rule, layer).
2. For each type to extract: public API surface, known callers (Core / ChallengePass / Shell / Tests / E2E).
3. Expected breaks (usings, InternalsVisibleTo, JSON DTO co-location, test helpers).
4. Proposed file paths and merge layer (usually M1 for Core types).
5. Annotation checklist: which M1 sites need the cross-PR comment when a later PR completes a move.

**No SA1402/SA1649 code moves until that plan is reviewed and approved.**

### Execute

Split/rename in dependency order; merge up; annotate per policy above; re-run tests and Sonar.

## Stage 3 — medium / minor / info

After Stage 2 (or in parallel only for pure mechanical non-structural items if Stage 2 is blocked on review):

- Priority signal: S109 (protocol/crypto/DB), S4056 CultureInfo, nested ternaries, specific catches.
- Keep soften list: S6354, S4055 i18n, S4004/S3956 DTO collections, residual mock fixture literals—document in `SONAR_GATE.md` rather than fake-fixing.

## Success criteria

| Stage | Done when |
|-------|-----------|
| 1 | M1–M3 HIGH/CRITICAL csharp clusters from S1a cleared or justified; IDE0008/SA1636/SA120\* volume materially down on re-scan; tests green; duplication comments present where later PRs superseded M1 sites |
| 2 | Structural plan approved; splits merged; SA1402/SA1649 cleared or explicitly waived with reason |
| 3 | Remaining MEDIUM/LOW/INFO either fixed or mapped to soften rows; coverage still tracked via Coverlet workflow separately |

## Risks

- Member reordering can churn blame and conflict with in-flight PR review comments—keep commits focused per batch (S1a, then S1b, …).
- S2339 on public const may be a binary/API shape change for ChallengePass suite IDs—prefer read-only properties with same literal values; update tests if they use `const` in attributes.
- Stack merge conflicts: resolve toward Stage 1 tip; re-apply duplication comments if conflict markers drop them.
