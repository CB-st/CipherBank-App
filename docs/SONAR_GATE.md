# SonarQube gate policy (MAUI Core / prototype stack)

**Project:** `CB-st_CipherBank-App_6f7fd196-021a-4b20-a3f2-9094fa18ab2c`  
**Host:** https://sonar.cipherbank.money  
**Applies to:** M1+ stacked PRs (`prototype/maui-m*`)

Sonar does not always annotate the GitHub PR; use the `sonar-context-<sha>` workflow artifact (`issues.json`, `quality-gate.json`) or the PR dashboard.

## What we fix in code (must reduce)

| Priority | Category | Examples | Policy |
|----------|----------|----------|--------|
| P0 | Reliability / bugs | S1244, S3923, S6966 | Fix before merge |
| P1 | HIGH maintainability | S2360 optional params, S1541/S3776 complexity, S2339 public const, S3218 shadowing, S1067 | Fix or overload; do not leave HIGH without a row below |
| P2 | MAJOR maintainability (signal) | S109 magic numbers in protocol/crypto/DB, nested ternaries S3358 | Name constants / extract helpers |
| P3 | Coverage | new_coverage / new_line_coverage | CI must run Coverlet into `reports/coverage.cobertura.xml` before `sonarscanner end` |

## Softened / deferred (explicit reasons)

These may remain on a foundation PR after the P0–P2 pass. Softening the **quality gate thresholds** on the server is allowed only when every remaining open issue maps to a row here (or a newer dated addendum).

| Rule / cluster | Why we may leave it | Follow-up |
|----------------|---------------------|-----------|
| **external_roslyn:SA*** (StyleCop file layout: SA1402, SA1201, SA1649, …) | Ordering / one-type-per-file / header layout does not change runtime behavior; mass moves fight the multi-type wire-model files. Sonar CI builds with `RunAnalyzersDuringBuild=false` so these are not re-imported as “new issues.” | Optional later StyleCop cleanup PR |
| **csharpsquid:S6354** (`DateTimeOffset.UtcNow`) | Introducing `IClock` / time provider across Core+Shell is a cross-cutting design change, not a local smell fix. | Dedicated clock-injection PR |
| **csharpsquid:S4055** (string literals → ResourceManager) | Product is not shipping i18n resource packs yet; forcing ResourceManager adds ceremony without users. | When localization starts |
| **csharpsquid:S4004 / S3956** (mutable collection properties on DTOs) | JSON wire DTOs need settable collections for deserialize; making them read-only breaks System.Text.Json patterns used here. | Revisit if we move to required init-only + custom converters |
| **csharpsquid:S109** residual fixture literals in `MockProductApi` | One-off demo balances / timestamps in the mock API are story fixtures, not protocol constants. Named where reused. | Keep naming new reused literals |
| **Record / property initializers** (e.g. `PricePoint` optional volume, layout defaults) | Not method optional parameters (S2360); changing record shape is API churn without maintainability win. | Leave |
| **INFO / LOW cosmetic** beyond gate INFO budget | Noise after HIGH/MAJOR cleanup. | Batch only if gate still red |
| **external_roslyn:IDE0007 / IDE0008** | Analyzers disagree (var vs explicit); flip-flops on re-scan. Ignored in `sonar.yml` multicriteria. | Leave |
| **csharpsquid:S4136** (overload ordering) | Zero-token DIM overload pairs are intentional (S2360 avoidance). Ignored in `sonar.yml`; CPD-excluded on those interfaces. | Leave |

Scanner also ignores deferred S6354/S4055/S4004/S3956 and IDE0021/22/28 via `sonar.issue.ignore.multicriteria` in `.github/workflows/sonar.yml` so they do not inflate `new_violations` on stacked PRs after the P0–P2 pass.

## Suggested gate softening (server)

After P0–P2 + Coverlet, ask SRE to set **new-code** thresholds approximately to:

| Metric | Was (strict) | Softened target | Rationale |
|--------|--------------|-----------------|-----------|
| Blocker / security | 0 | 0 | Never soften |
| Reliability | 0 | 0 | Never soften |
| HIGH maintainability | 0 | 0 (after P1 pass) | Code-fixed, not threshold-waived |
| MEDIUM maintainability | 0 | ≤ 40 (or warn) | Allows residual S6354/S4004/fixture S109 with reasons above |
| LOW | ≤ 5 | ≤ 30 | Style leftovers |
| INFO | ≤ 20 | ≤ 80 | Doc/info noise |
| Coverage on new code | ≥ 40% / line ≥ 70% | Keep | Enforced via Coverlet in `sonar.yml` |

If HIGH is still &gt; 0 after the P1 pass, **do not** raise the HIGH threshold — fix or justify each remaining key in this file.

## Local verify

```bash
export PATH="$HOME/.local/dotnet:$PATH"
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release \
  -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura \
  -p:CoverletOutput=reports/coverage.cobertura.xml -p:Threshold=0
```
