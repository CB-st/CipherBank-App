# Local multi-language lint (org-ready)

Pre-push / agent lint that mirrors Sonar-style smell checking for **C#** (same Roslyn engine as SonarQube) and portable stand-ins for **Shell, Python, C++, Make** so Cursor checkouts across CipherBank (tooling Python, backend C++, Makefiles) share one workflow.

**Sonar policy:** [SONAR_GATE.md](SONAR_GATE.md) · **Server:** https://sonar.cipherbank.money

## Quick start

```bash
./scripts/lint/install-tools.sh   # once — tools under ~/.local/cb-lint/bin
./scripts/lint.sh                 # auto-detect languages with sources
./scripts/lint.sh csharp shell    # subset
./scripts/lint.sh --strict        # C#: also fail on analyzer warnings
./scripts/lint.sh --core-only     # C#: Core + Tests only (M1)
```

On this MAUI repo tip you typically get **csharp + shell**; python / cpp / make print `skip (…): no sources` until those files exist.

## Languages

| Lang | Script | Tool | When it runs |
|------|--------|------|----------------|
| C# | `lint-csharp.sh` | SonarAnalyzer.CSharp (opt-in NuGet) | `*.csproj` / `*.cs` present |
| Shell | `lint-shell.sh` | shellcheck | `*.sh` present |
| Python | `lint-python.sh` | ruff | `*.py` present |
| C++ | `lint-cpp.sh` | clang-tidy (+ optional clang-format dry-run) | C/C++ sources or `CMakeLists.txt` |
| Make | `lint-make.sh` | checkmake | `Makefile` / `*.mk` |

Pinned versions: `scripts/lint/tool-versions.env`.  
Default configs (for other repos / when no project config): `scripts/lint/configs/`.

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
