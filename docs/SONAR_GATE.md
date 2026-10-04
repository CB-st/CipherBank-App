# SonarQube gate policy (MAUI Core / prototype stack)

**Project:** `CB-st_CipherBank-App_59d7f589-fd7d-4064-9687-e720f9b3443c`  
**Host:** https://sonar.cipherbank.money  
**Applies to:** M1+ stacked PRs (`prototype/maui-m*`)

Sonar does not always annotate the GitHub PR; use the `sonar-context-<sha>` workflow artifact (`issues.json`, `quality-gate.json`) or the PR dashboard.

## What we fix in code (must reduce)

| Priority | Category | Examples | Policy |
|----------|----------|----------|--------|
| P0 | Reliability / bugs | S1244, S3923, S6966 | Fix before merge |
| P1 | HIGH maintainability | S2360 optional params, S1541/S3776 complexity, S2339 public const, S3218 shadowing, S1067 | Fix or overload |
| P2 | MAJOR maintainability (signal) | S109 magic numbers, nested ternaries S3358, Uri string props S399x | Name constants / extract helpers / use `Uri` |
| P3 | Coverage | new_coverage / new_line_coverage | Coverlet → OpenCover (`reports/coverage.opencover.xml`) for `sonar.cs.opencover.reportsPaths`; CI fails closed if the artifact is missing. When the project quality gate includes `new_coverage`, treat **≥80%** as the target (confirm via `quality-gate.json` — do not assume the condition from docs alone). |
| P0 | NuGet constraint / restore | NU1608, NU1605, NU1107 | **Errors** via `Directory.Build.props` `WarningsAsErrors`; never demote to WNAE |

## Remaining exceptions (not deferred smells — structural/product constraints)

| Rule / cluster | Why it remains | Follow-up |
|----------------|----------------|-----------|
| **CA1707** RootNamespace `CipherBank_app` | Underscored root namespace matches the shipping assembly / historical package id; renaming is a cross-repo break. Suppressed via `NoWarn` on Core/ChallengePass. | Coordinated rename |
| **Record / property initializers** (e.g. `PricePoint` optional volume) | Not method optional parameters (S2360); changing record shape is API churn without maintainability win. | Leave |
| **InMemoryProductClient fixture literals** | One-off demo balances / timestamps are story fixtures; named where reused. | Keep naming new reused literals |

## Burned clusters (2026-07 full burndown)

Previously deferred rows cleared **in code** (no new `sonar.yml` ignores):

- **S6354** — `TimeProvider` injected across Core + Shell (pattern from `RateLimiter`)
- **S4055** — `CipherBank-app.Core/Resources/Strings.resx` + `Resources/Strings` ResourceManager for ACH / PIN messages
- **S4004 / S3956** — get-only `Collection<T>` / dictionaries with `JsonObjectCreationHandling.Populate` on wire DTOs (`PrefsWireDto` / `PortfolioDto`). **`UserPrefs` keeps setters** so `PrefsStore`’s default STJ options can replace `HomeOrder` / `EnabledCurrencies` on load (get-only + Populate would append onto defaults)
- **S4136** — DIM overload pairs kept contiguous on Core/ChallengePass interfaces (CPD exclusions unchanged)
- **SA1402 Shell leftovers** — nested VMs/controls extracted (`HomeSectionToggle`, `AssetRowVm`, `MixSource`, …)
- **IDE0007/IDE0008** — convention locked in `.editorconfig` (`var` when apparent; IDE0008 silenced)

## Local verify

```bash
export PATH="$HOME/.local/dotnet:$PATH"
source scripts/lib/android-env.sh
./scripts/lint.sh                  # csharp + shell (+ other langs when present)
./scripts/lint-csharp.sh --strict  # C# only; also fail on remaining warnings
mkdir -p reports
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release \
  -p:CollectCoverage=true -p:CoverletOutputFormat=\"cobertura,opencover\" \
  -p:CoverletOutput="$PWD/reports/coverage" -p:Threshold=0
```

**Pre-push lint:** [LOCAL_LINT.md](LOCAL_LINT.md) · C# Connected Mode: [LOCAL_SONAR_LINT.md](LOCAL_SONAR_LINT.md).
