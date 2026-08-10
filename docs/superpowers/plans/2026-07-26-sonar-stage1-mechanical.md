# Sonar Stage 1 (mechanical + CRITICAL) Implementation Plan

**Goal:** Clear Stage 1 Sonar clusters on the MAUI stack—CRITICAL/HIGH csharp (S1a), explicit types (S1b), ChallengePass file headers (S1c), and member ordering (S1d)—landing fixes on the earliest owning branch and merging upward.

**Architecture:** Fix Core on `prototype/maui-m1`, ChallengePass on `prototype/maui-m2`, then merge m1→m2→m3→m4. Mechanical batches use Sonar `issues.json` line maps and `dotnet format` / targeted edits. Do not split files (SA1402/SA1649). Cross-PR duplication comments only when a later PR re-touches or supersedes an M1 finding.

**Tech Stack:** .NET 10, StyleCop Analyzers, Sonar csharp + external_roslyn, xUnit (`CipherBank-app.Tests`), stacked git branches `prototype/maui-m{1..4}`.

**Spec:** `docs/superpowers/specs/2026-07-26-sonar-stage1-mechanical-design.md`

## Global Constraints

- Landing: earliest owning branch, then merge up (never Stage-1-only on M4 tip).
- IDE0008 wins over IDE0007 for this pass.
- No SA1402 / SA1649 file splits in Stage 1.
- No `IClock` / S6354 in Stage 1.
- Duplication comment form when required: `// Sonar: issue resolved in M{N} PR (https://github.com/CB-st/CipherBank-App/pull/{N}/…), edit here is duplication` with `{N}` in {2,3,4}.
- Do not commit `design_handoff_cipherbank/` or push workflow files without `workflow` PAT scope.
- `PATH` must include `$HOME/.local/dotnet` for CLI.

**Issue artifacts (local):** `/tmp/sonar-stack/{m1,m2,m3}/issues.json`

**PR links:** [#20](https://github.com/CB-st/CipherBank-App/pull/20) M1 · [#21](https://github.com/CB-st/CipherBank-App/pull/21) M2 · [#22](https://github.com/CB-st/CipherBank-App/pull/22) M3 · [#23](https://github.com/CB-st/CipherBank-App/pull/23) M4

---

### Task 1: Branch hygiene + stash non-Stage-1 noise

**Files:**
- None committed (workspace only)

- [ ] **Step 1: Note current branch and dirty paths**

```bash
cd /home/skyrailmaxima/Desktop/CipherBank/App_BuildSpace/CipherBank-App
git status -sb
git branch --show-current
```

Expected: likely `prototype/maui-m4` with possible `M .github/workflows/sonar.yml` and `?? design_handoff_cipherbank/`.

- [ ] **Step 2: Stash or leave alone workflow + Expo; do not commit them**

```bash
git stash push -m "wip sonar.yml coverlet" -- .github/workflows/sonar.yml || true
```

Leave `design_handoff_cipherbank/` untracked.

- [ ] **Step 3: Checkout M1 tip tracking remote**

```bash
git fetch origin
git checkout prototype/maui-m1
git pull --ff-only origin prototype/maui-m1
git log -1 --oneline
```

Expected: on `prototype/maui-m1`, clean enough to edit Core/Tests.

---

### Task 2: S1a Core CRITICAL on M1 — `AchRecipientValidation` S2339

**Files:**
- Modify: `CipherBank-app.Core/Persist/AchRecipientValidation.cs`
- Test: existing Persist/ACH tests via full Core test filter

**Interfaces:**
- Produces: `RoutingNumberDigitCount`, `AccountNumberMinDigits`, `MaskVisibleTrailingDigits`, `MemoMaxLength` as `public static int` get-only properties (same literal values 9, 4, 4, 140). Call sites using `AchRecipientValidation.RoutingNumberDigitCount` etc. keep compiling (const→static property is source-compatible for reads; not usable as default attribute args—verify no attribute usage).

- [ ] **Step 1: Confirm consts on M1 tip**

```bash
sed -n '1,40p' CipherBank-app.Core/Persist/AchRecipientValidation.cs
```

Expected: four `public const int` fields after the class opening brace.

- [ ] **Step 2: Replace consts with static read-only properties**

Replace:

```csharp
    public const int RoutingNumberDigitCount = 9;
    public const int AccountNumberMinDigits = 4;
    public const int MaskVisibleTrailingDigits = 4;
    public const int MemoMaxLength = 140;
```

With:

```csharp
    public static int RoutingNumberDigitCount => 9;

    public static int AccountNumberMinDigits => 4;

    public static int MaskVisibleTrailingDigits => 4;

    public static int MemoMaxLength => 140;
```

- [ ] **Step 3: Grep for attribute / default-arg uses of these names**

```bash
rg -n "AchRecipientValidation\.(RoutingNumberDigitCount|AccountNumberMinDigits|MaskVisibleTrailingDigits|MemoMaxLength)" -g '*.cs'
```

If any use as attribute argument, switch that site to a private `const` local to the attribute consumer or keep a `private const` + public property wrapper—prefer private const for attribute + public property exposing it only if needed.

- [ ] **Step 4: Build Core**

```bash
export PATH="$HOME/.local/dotnet:$PATH"
dotnet build CipherBank-app.Core/CipherBank-app.Core.csproj -c Release --nologo -v q
```

Expected: exit 0.

---

### Task 3: S1a Core CRITICAL — `MnemonicBackupService` S1067 + `PaymentUri` S1541

**Files:**
- Modify: `CipherBank-app.Core/Custody/MnemonicBackupService.cs` (~line 176)
- Modify: `CipherBank-app.Core/Wallets/PaymentUri.cs` (~line 75, method with complexity 12)
- Test: `CipherBank-app.Tests` custody / wallet tests

**Interfaces:**
- Produces: extracted private helpers; public method signatures unchanged.

- [ ] **Step 1: Open the S1067 expression**

```bash
sed -n '160,200p' CipherBank-app.Core/Custody/MnemonicBackupService.cs
```

- [ ] **Step 2: Split the compound condition**

Extract boolean locals or early returns so no expression has more than 3 conditional operators. Example pattern:

```csharp
bool missingA = ...;
bool missingB = ...;
bool missingC = ...;
if (missingA || missingB || missingC)
{
    ...
}
```

Keep behavior identical; do not change quiz/backup semantics.

- [ ] **Step 3: Open PaymentUri complexity hotspot**

```bash
sed -n '60,160p' CipherBank-app.Core/Wallets/PaymentUri.cs
```

- [ ] **Step 4: Extract helpers until cyclomatic complexity ≤ 10**

Move scheme/query parsing branches into `private static` helpers (e.g. `TryParseAmount`, `ApplyQueryParams`) called from the main parse method. Public API of `PaymentUri` unchanged.

- [ ] **Step 5: Run unit tests (no ChallengePass on M1 if absent)**

```bash
export PATH="$HOME/.local/dotnet:$PATH"
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release --nologo \
  --filter "FullyQualifiedName!~ChallengePass"
```

Expected: all passed.

- [ ] **Step 6: Commit M1 S1a Core**

```bash
git add CipherBank-app.Core/Persist/AchRecipientValidation.cs \
  CipherBank-app.Core/Custody/MnemonicBackupService.cs \
  CipherBank-app.Core/Wallets/PaymentUri.cs
git commit -m "$(cat <<'EOF'
fix(core): clear Sonar CRITICAL S2339/S1067/S1541 on M1

EOF
)"
```

---

### Task 4: S1b Core/Tests IDE0008 on M1 (`var` → explicit type)

**Files:** (from `/tmp/sonar-stack/m1/issues.json` rule `external_roslyn:IDE0008`)

Core (representative high-count):
- `CipherBank-app.Core/Wallets/AddressDerive.cs` (13)
- `CipherBank-app.Core/Persist/RecipientRepository.cs` (11)
- `CipherBank-app.Core/Persist/WalletRepository.cs` (7)
- `CipherBank-app.Core/Persist/MarketRepository.cs` (5)
- `CipherBank-app.Core/Persist/RatesCache.cs` (5)
- `CipherBank-app.Core/Pos/NfcPresentment.cs` (5)
- Plus remaining Core/Test files listed in the m1 IDE0008 map (35 files, 153 issues total)

**Interfaces:**
- Produces: same locals with explicit types; no API changes.

- [ ] **Step 1: Export line map**

```bash
python3 - <<'PY'
import json
from collections import defaultdict
from pathlib import Path
issues=json.loads(Path('/tmp/sonar-stack/m1/issues.json').read_text())
by=defaultdict(list)
for i in issues:
    if i['rule']!='external_roslyn:IDE0008': continue
    by[i['component'].split(':')[-1]].append(i.get('line'))
for f, lines in sorted(by.items()):
    print(f'{f}: {sorted({l for l in lines if l})}')
PY
```

- [ ] **Step 2: Prefer analyzer-driven format when available**

```bash
export PATH="$HOME/.local/dotnet:$PATH"
dotnet format CipherBank-app.Core/CipherBank-app.Core.csproj --diagnostics IDE0008 --severity info --verbosity diagnostic 2>&1 | tail -40
dotnet format CipherBank-app.Tests/CipherBank-app.Tests.csproj --diagnostics IDE0008 --severity info --verbosity diagnostic 2>&1 | tail -40
```

If `dotnet format` does not rewrite IDE0008 (common when style is IDE0007-prefer-var), fall back to Step 3.

- [ ] **Step 3: Manual/scripted per-file replacement at reported lines**

For each `(file, line)`: replace `var name =` with `TypeName name =` using the compile-time type (hover / build error after temporary wrong type). Skip `var` over anonymous types / `new { }` / `ValueTuple` with names if the explicit type would be ugly—only fix Sonar-reported lines.

Do **not** mass-replace every `var` in the repo.

- [ ] **Step 4: Build + test**

```bash
export PATH="$HOME/.local/dotnet:$PATH"
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release --nologo \
  --filter "FullyQualifiedName!~ChallengePass"
```

Expected: pass.

- [ ] **Step 5: Commit**

```bash
git add CipherBank-app.Core CipherBank-app.Tests
git commit -m "$(cat <<'EOF'
style(core): IDE0008 explicit types for Sonar Stage 1b on M1

EOF
)"
```

---

### Task 5: S1d Core/Tests member order (SA1201/1202/1204) on M1

**Files:** 23 files from m1 SA120* map, including:
- `CipherBank-app.Core/Custody/CustodyService.cs`, `PinService.cs`, `PinChange.cs`
- `CipherBank-app.Core/Persist/{PrefsStore,RecipientRepository,WalletRepository,LocalDb,SyncJobQueue,IRatesCache}.cs`
- `CipherBank-app.Core/Session/AppSession.cs`
- `CipherBank-app.Core/V1/{MockProductApi,StreamService,PrefsWire,SessionChallenge}.cs`
- `CipherBank-app.Core/Pos/NfcPresentment.cs`, `Charts/ChartMath.cs`, `Wallets/WalletRegistry.cs`
- Matching test files under `CipherBank-app.Tests/Custody|Session|V1`

**Interfaces:**
- Produces: same members, reordered only.

- [ ] **Step 1: For each listed file, reorder members**

Order within type (StyleCop):
1. Fields / constants
2. Constructors
3. Properties / indexers
4. Methods
5. Nested types  
Within each group: `public` before `private`; `static` before instance (SA1204).

Use the Sonar line as the “offender” (e.g. property following a method → move property block above methods).

- [ ] **Step 2: Build + test after each ~5-file batch** (avoid one giant untested reorder)

```bash
export PATH="$HOME/.local/dotnet:$PATH"
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release --nologo \
  --filter "FullyQualifiedName!~ChallengePass"
```

- [ ] **Step 3: Commit**

```bash
git add CipherBank-app.Core CipherBank-app.Tests
git commit -m "$(cat <<'EOF'
style(core): SA1201/1202/1204 member ordering for Sonar Stage 1d on M1

EOF
)"
```

- [ ] **Step 4: Push M1**

```bash
git push origin prototype/maui-m1
```

---

### Task 6: Merge M1 → M2; S1a ChallengePass CRITICAL

**Files:**
- Modify (ChallengePass):
  - `AccountKeyDerivation.cs` (S2339×2)
  - `Algorithms/X25519ChaChaSealAlgorithm.cs` (S2339)
  - `ChallengePassCatalog.cs` (S2365)
  - `ChallengePassSlots.cs` (S2360)
  - `DependencyInjection/ChallengePassServiceCollectionExtensions.cs` (S2339×2, S2360)
  - `Hybrid/HybridKeyShareModels.cs` (S2360)
  - `Hybrid/HybridMlKemX25519Agreement.cs` (S2339×2)
  - `ISessionChallengeClient.cs` (S2360)
  - `InMemorySessionChallengeClient.cs` (S2360×2)
  - `StaticAccountKeySource.cs` (S2360)
  - `Structures/PqChannelChallengePassStructure.cs` (S2339, S2360×2)
  - `Structures/TwoStepChallengePassStructure.cs` (S2339)
  - `Templates/ChallengeIdNonceSha256Template.cs` (S2339, S2302×2)
  - `WireEncoding.cs` (S131)
- Test: `CipherBank-app.Tests/ChallengePass/**`

**Interfaces:**
- S2339: `public const string X = "..."` → `public static string X => "...";` (or get-only property). Update any `const` consumers in attributes.
- S2360: split optional-parameter methods into required + overload that supplies defaults.
- S2365: `AvailableSuiteIds` property → `GetAvailableSuiteIds()` method returning a new collection; update call sites.
- S2302: `nameof(plaintext)` instead of `"plaintext"`.
- S131: `default:` branch on `WireEncoding.FromWire` switch (throw or pad consistently).

- [ ] **Step 1: Checkout M2 and merge M1**

```bash
git checkout prototype/maui-m2
git pull --ff-only origin prototype/maui-m2
git merge prototype/maui-m1 -m "$(cat <<'EOF'
merge(m1): Stage 1 Core Sonar mechanical + CRITICAL

EOF
)"
```

Resolve conflicts favoring Stage 1 Core fixes. If a conflict re-applies an M1 fix that M2 already changed differently, add duplication comment on the M2 side pointing at #21 commit once committed.

- [ ] **Step 2: S2339 on ChallengePass public consts**

Example for `AccountKeyDerivation.cs`:

```csharp
public static string HkdfSalt => "CipherBank";
public static string HkdfInfo => "account/x25519/v1";
```

Repeat for every S2339 path listed above (suite IDs, algorithm names, DI keys).

- [ ] **Step 3: S2360 overloads**

For each optional-parameter API (slots, DI `AddChallengePass`, hybrid models, session client, static key source, PQ structure):

```csharp
// Before
void Foo(string a, int b = 0);

// After
void Foo(string a) => Foo(a, 0);
void Foo(string a, int b);
```

Keep interface + implementation signatures aligned.

- [ ] **Step 4: Catalog, nameof, switch default**

```csharp
// ChallengePassCatalog — replace copying property with:
public static IReadOnlyList<string> GetAvailableSuiteIds() => /* copy or immutable list */;

// ChallengeIdNonceSha256Template — ArgumentNullException.ThrowIfNull(plaintext, nameof(plaintext));
// WireEncoding.FromWire — add default: throw new FormatException(...);
```

Update all `AvailableSuiteIds` call sites to `GetAvailableSuiteIds()`.

- [ ] **Step 5: Test ChallengePass**

```bash
export PATH="$HOME/.local/dotnet:$PATH"
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release --nologo \
  --filter "FullyQualifiedName~ChallengePass"
```

Expected: pass.

- [ ] **Step 6: Commit S1a ChallengePass**

```bash
git add CipherBank-app.ChallengePass CipherBank-app.Tests/ChallengePass
git commit -m "$(cat <<'EOF'
fix(challengepass): clear Sonar CRITICAL S2339/S2360/S2302/S2365/S131

EOF
)"
```

---

### Task 7: S1c ChallengePass SA1636 headers + stylecop.json

**Files:**
- Create: `CipherBank-app.ChallengePass/stylecop.json`
- Modify: `CipherBank-app.ChallengePass/CipherBank-app.ChallengePass.csproj` (ensure `AdditionalFiles` / StyleCop settings link if required—mirror Core csproj)
- Modify: all 23 SA1636 files only if header text still mismatches after settings (usually settings fix is enough)

**Interfaces:**
- Produces: StyleCop `copyrightText` matching existing headers.

- [ ] **Step 1: Add ChallengePass stylecop.json matching Core headers**

```json
{
  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
  "settings": {
    "documentationRules": {
      "companyName": "CipherBank",
      "copyrightText": "Copyright (c) {companyName}. All rights reserved.",
      "xmlHeader": true
    }
  }
}
```

- [ ] **Step 2: Wire stylecop.json into the ChallengePass csproj the same way Core does**

```bash
rg -n "stylecop.json" CipherBank-app.Core/CipherBank-app.Core.csproj
# Mirror AdditionalFiles Include in ChallengePass csproj
```

- [ ] **Step 3: Verify a sample file header matches**

Header template (already on files):

```csharp
// <copyright file="WireEncoding.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>
```

If any file lacks this header, add it with the correct `file=` attribute.

- [ ] **Step 4: Commit**

```bash
git add CipherBank-app.ChallengePass/stylecop.json CipherBank-app.ChallengePass/CipherBank-app.ChallengePass.csproj CipherBank-app.ChallengePass
git commit -m "$(cat <<'EOF'
style(challengepass): align StyleCop copyright settings for SA1636

EOF
)"
```

---

### Task 8: S1b/S1d ChallengePass + Tests on M2; push M2

**Files:**
- IDE0008: 9 files / 11 issues from m2 map (`PortableX25519.cs`, hybrid files, ChallengePass tests)
- SA120*: `AccountKeyDerivation.cs`, `ChallengePassSlots.cs`, `HybridKeyShareModels.cs`, `PqChannelChallengePassStructure.cs`, `CustodyAccountKeySourceTests.cs`

- [ ] **Step 1: Fix IDE0008 lines on M2 map**

Same rules as Task 4.

- [ ] **Step 2: Reorder SA120* members on listed ChallengePass files**

- [ ] **Step 3: Full test suite on M2**

```bash
export PATH="$HOME/.local/dotnet:$PATH"
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release --nologo
```

Expected: pass.

- [ ] **Step 4: Commit + push M2**

```bash
git add CipherBank-app.ChallengePass CipherBank-app.Tests
git commit -m "$(cat <<'EOF'
style(challengepass): IDE0008 + SA120* Stage 1 on M2

EOF
)"
git push origin prototype/maui-m2
```

---

### Task 9: Merge M2 → M3; residual M3 Stage 1 deltas

**Files:**
- Merge brings Core/ChallengePass Stage 1.
- M3-only IDE0008 leftovers (often Tests/Services + some Core lines already fixed on M1—if still reported after merge, fix once; if edit is duplicate of M1 fix, add duplication comment pointing at #20 commit).
- M3 SA120*: `MockProductApi.cs` SA1202, `StreamService.cs` SA1204—if still present after merge.

**Interfaces:**
- Produces: clean merge; duplication comments only when required by spec.

- [ ] **Step 1: Checkout M3 and merge M2**

```bash
git checkout prototype/maui-m3
git pull --ff-only origin prototype/maui-m3
git merge prototype/maui-m2 -m "$(cat <<'EOF'
merge(m2): Stage 1 Sonar mechanical + CRITICAL

EOF
)"
```

- [ ] **Step 2: Reconcile M3-only Sonar lines**

```bash
python3 - <<'PY'
import json
from pathlib import Path
from collections import defaultdict
issues=json.loads(Path('/tmp/sonar-stack/m3/issues.json').read_text())
want={'external_roslyn:IDE0008','external_roslyn:SA1201','external_roslyn:SA1202','external_roslyn:SA1204'}
# CRITICAL already fixed via M1 merge for AchRecipientValidation/PaymentUri
for i in issues:
    if i['rule'] in want or i.get('severity')=='CRITICAL':
        print(i['rule'].split(':')[-1], i['component'].split(':')[-1], i.get('line'))
PY
```

For each remaining hit: fix if still valid; if the line was already corrected by merge and you only re-touch for conflict, add:

```csharp
// Sonar: issue resolved in M1 PR (https://github.com/CB-st/CipherBank-App/pull/20/commits/<sha>), edit here is duplication
```

Note: spec says `{N}` is 2–4 for “resolved in”; when the **canonical** fix is M1 and the **duplicate edit** is on M3, use:

```csharp
// Sonar: issue resolved in M1 PR (https://github.com/CB-st/CipherBank-App/pull/20/commits/<sha>), edit here is duplication
```

**Amendment for this plan:** allow `M1` in the comment **only** when the duplicate site is on M2–M4 and the canonical fix is M1. Spec’s “N is 2–4” covers the case where the resolving PR is later; both directions need a pointer—use the PR that owns the canonical fix.

- [ ] **Step 3: Test + commit + push M3**

```bash
export PATH="$HOME/.local/dotnet:$PATH"
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release --nologo
git add -A
git status -sb
# stage only intentional source/docs; exclude design_handoff and workflow unless intended
git commit -m "$(cat <<'EOF'
merge(m2)+style: Stage 1 Sonar residuals on M3

EOF
)"
git push origin prototype/maui-m3
```

---

### Task 10: Merge M3 → M4; smoke tests

**Files:** merge only unless E2E compile breaks.

- [ ] **Step 1: Merge and push M4**

```bash
git checkout prototype/maui-m4
git pull --ff-only origin prototype/maui-m4
git merge prototype/maui-m3 -m "$(cat <<'EOF'
merge(m3): Stage 1 Sonar mechanical + CRITICAL

EOF
)"
export PATH="$HOME/.local/dotnet:$PATH"
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release --nologo
git push origin prototype/maui-m4
```

- [ ] **Step 2: Note CI**

After Actions finish, download new `sonar-context-*` for PRs 20–22 and confirm HIGH/CRITICAL S1a rules cleared; IDE0008/SA120\*/SA1636 counts drop. Coverage may still fail until Coverlet workflow is pushable.

---

### Task 11: Stage 2 stub doc (no splits yet)

**Files:**
- Create: `docs/SONAR_STRUCTURAL_PLAN.md` (skeleton only)

- [ ] **Step 1: Write skeleton listing SA1402/SA1649 from `/tmp/sonar-stack` without performing splits**

Include columns: layer, file, types, callers TBD, proposed path, breaks, annotation needed.

- [ ] **Step 2: Commit on M1 or M4 tip per team preference (prefer M1 so it merges up)**

```bash
git checkout prototype/maui-m1
# cherry-pick or add file, merge up — or add on m4 only if docs-only preferred on tip
```

Prefer adding the skeleton on **M1** and merging up so all PRs see the plan.

---

## Spec coverage check

| Spec item | Task |
|-----------|------|
| S1a CRITICAL csharp | 2, 3, 6 |
| S1b IDE0008 | 4, 8, 9 |
| S1c SA1636 headers | 7 |
| S1d SA120\* order | 5, 8, 9 |
| Land earliest + merge up | 1, 5–10 |
| Duplication comments | 6, 9 |
| No SA1402/SA1649 execute | 11 skeleton only |
| Tests before push | 3–10 |

## After Stage 1

Do **not** start Stage 3 medium/minor/info until Stage 2 structural plan is reviewed (per spec). Coverlet/`sonar.yml` remains a separate ops task.
