# Local Sonar-aligned C# linting

Keep PR quality-gate surprises low by running the **same Roslyn rule family** SonarQube uses (`csharpsquid:S*`) before push, plus the StyleCop / NetAnalyzers already in `Directory.Build.props`.

**Server:** https://sonar.cipherbank.money  
**Project:** `CB-st_CipherBank-App_59d7f589-fd7d-4064-9687-e720f9b3443c`  
**Policy:** [SONAR_GATE.md](SONAR_GATE.md)

## What we use (two layers)

| Layer | What | When |
|-------|------|------|
| **CLI (in-repo)** | `SonarAnalyzer.CSharp` NuGet, opt-in via `-p:EnableSonarAnalyzers=true` | Pre-push / agent loops — `./scripts/lint-csharp.sh` |
| **IDE Connected Mode** | [SonarQube for IDE](https://www.sonarsource.com/products/sonarlint/) (formerly SonarLint) bound to `sonar.cipherbank.money` | Squiggles in Cursor / VS Code / Rider / Visual Studio using the **server quality profile** |

CLI ≈ same analyzer engine as the server for C# smells. Connected Mode also picks up **server-side** profile / multicriteria ignores and is the closest match to the PR “SonarQube Code Analysis” check.

Default `dotnet build` / CI compile paths stay unchanged (`EnableSonarAnalyzers` defaults off) so deferred debt does not suddenly fail every build.

## CLI — before push

```bash
source scripts/lib/android-env.sh
./scripts/lint-csharp.sh           # Core + ChallengePass (if present) + Tests
./scripts/lint-csharp.sh --core-only   # M1 worktrees without ChallengePass
./scripts/lint-csharp.sh --strict      # also fail on remaining Sonar/StyleCop warnings
```

Severities live in `.editorconfig` under the SonarAnalyzer section and mirror [SONAR_GATE.md](SONAR_GATE.md):

- **error (default fail):** P0 reliability bugs (S1244, S3923, S6966)
- **warning (`--strict` fails):** maintainability / signal rules still under burn (S109, S1541, …)
- **suggestion:** residual MINOR (S2221, S4056, …)
- **none:** SQL `CommandText` S4055 noise in Persist repositories; IDE0008 (var/explicit lock prefers IDE0007)

Former deferred clusters (S6354 clocks, S4055 UI strings, S4004/S3956 DTO collections, S4136 DIM grouping, Shell SA1402) were burned in the 2026-07 full burndown — see [SONAR_GATE.md](SONAR_GATE.md).

## IDE — SonarQube for IDE (Connected Mode)

1. Install **SonarQube for IDE** in Cursor (VS Code marketplace) or Rider / Visual Studio.
2. Connect to **https://sonar.cipherbank.money** with a user token (SonarQube → My Account → Security).
3. Bind this workspace to project key  
   `CB-st_CipherBank-App_59d7f589-fd7d-4064-9687-e720f9b3443c`.
4. Open a stacked PR branch (`prototype/maui-m*`) so Connected Mode can use PR/new-code context when offered.

Connected Mode does **not** replace Coverlet/`new_coverage` or CPD density — those remain CI/server metrics. It does surface the same smell rules locally.

## Coverage (still CI / Coverlet)

```bash
mkdir -p reports
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release \
  -p:CollectCoverage=true -p:CoverletOutputFormat=cobertura \
  -p:CoverletOutput="$PWD/reports/coverage" -p:Threshold=0
```

Gate expects **≥80%** new coverage on Sonar; local Coverlet total line % is a useful proxy but not identical to Sonar’s new-code window.

## What this does *not* do

- Does not push `.github/workflows/sonar.yml` (needs GitHub `workflow` scope).
- Does not soft-fail the GitHub Sonar check by itself — only reduces surprises before push.
- StyleCop `SA*` structural debt remains Stage 2 / deferred unless you opt into `--strict`.
