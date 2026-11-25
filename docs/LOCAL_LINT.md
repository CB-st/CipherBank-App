# Local lint

Pre-push / agent lint that mirrors Sonar-style smell checking for **C#** (same Roslyn engine as SonarQube) plus **shellcheck** for the repo's shell scripts. Lanes exist only for languages this repo contains; the Python/C++/Make stand-ins were removed and live in git history if those sources ever land.

**Sonar policy:** [SONAR_GATE.md](SONAR_GATE.md) · **Server:** https://sonar.cipherbank.money

## Quick start

```bash
./scripts/lint/install-tools.sh   # once — tools under ~/.local/cb-lint/bin
./scripts/lint.sh                 # auto-detect languages with sources
./scripts/lint.sh csharp shell    # subset
./scripts/lint.sh --strict        # C#: also fail on analyzer warnings
./scripts/lint.sh --core-only     # C#: Core + Tests only (M1)
```

On this MAUI repo tip you get **csharp + shell**.

## Languages

| Lang | Script | Tool | When it runs |
|------|--------|------|----------------|
| C# | `lint-csharp.sh` | SonarAnalyzer.CSharp (opt-in NuGet) | `*.csproj` / `*.cs` present |
| Shell | `lint-shell.sh` | shellcheck | `*.sh` present |

Pinned versions: `scripts/lint/tool-versions.env`.

## C# / Sonar alignment

Default `dotnet build` is unchanged. Local C# lint sets `-p:EnableSonarAnalyzers=true`. Severities live in `.editorconfig` (see [LOCAL_SONAR_LINT.md](LOCAL_SONAR_LINT.md) for Connected Mode IDE setup).

```bash
./scripts/lint-csharp.sh
./scripts/lint-csharp.sh --strict
```

## What this does *not* do

- Does **not** reproduce Sonar `new_coverage` or CPD density (use Coverlet + CI `sonar-context`).
- Does **not** replace the GitHub Sonar check.
- Empty languages are skipped — that is not a soft-pass of Sonar on C#.

## Coverage proxy

```bash
mkdir -p reports
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj -c Release \
  -p:CollectCoverage=true -p:CoverletOutputFormat=\"cobertura,opencover\" \
  -p:CoverletOutput="$PWD/reports/coverage" -p:Threshold=0
```

Aim toward **~80–90%** new coverage on stacked PRs when burning coverage debt. Confirm whether `new_coverage` is an active quality-gate condition via the CI `quality-gate.json` artifact — docs are not a substitute for the live gate.
