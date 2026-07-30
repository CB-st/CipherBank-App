#!/usr/bin/env bash
# Local C# lint aligned with SonarQube (csharpsquid) + existing StyleCop/NetAnalyzers.
#
# Usage:
#   ./scripts/lint-csharp.sh              # Core + ChallengePass + Tests
#   ./scripts/lint-csharp.sh --core-only  # skip ChallengePass if absent (M1)
#   ./scripts/lint-csharp.sh --strict     # also fail on Sonar warnings (not just errors)
#
# Opt-in: does not change default `dotnet build` (EnableSonarAnalyzers defaults off).
# Policy: docs/SONAR_GATE.md · Connected Mode: docs/LOCAL_SONAR_LINT.md

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=lib/android-env.sh
source "$ROOT/scripts/lib/android-env.sh"

core_only=0
strict=0
for arg in "$@"; do
  case "$arg" in
    --core-only) core_only=1 ;;
    --strict) strict=1 ;;
    -h|--help)
      sed -n '2,12p' "$0"
      exit 0
      ;;
    *)
      echo "Unknown arg: $arg" >&2
      exit 2
      ;;
  esac
done

projects=(
  CipherBank-app.Core/CipherBank-app.Core.csproj
  CipherBank-app.Tests/CipherBank-app.Tests.csproj
)
if [[ "$core_only" -eq 0 && -f CipherBank-app.ChallengePass/CipherBank-app.ChallengePass.csproj ]]; then
  projects=(
    CipherBank-app.Core/CipherBank-app.Core.csproj
    CipherBank-app.ChallengePass/CipherBank-app.ChallengePass.csproj
    CipherBank-app.Tests/CipherBank-app.Tests.csproj
  )
fi

echo "==> Sonar-aligned local lint (EnableSonarAnalyzers=true)"
echo "    Projects: ${projects[*]}"
echo "    Policy: docs/SONAR_GATE.md"
echo

fail=0
log="$(mktemp)"
trap 'rm -f "$log"' EXIT

for project in "${projects[@]}"; do
  if [[ ! -f "$project" ]]; then
    echo "skip missing $project"
    continue
  fi
  echo "---- build $project ----"
  set +e
  dotnet build "$project" -c Release --nologo \
    -p:EnableSonarAnalyzers=true \
    -p:RunAnalyzersDuringBuild=true \
    -p:EnforceCodeStyleInBuild=true \
    >"$log" 2>&1
  rc=$?
  set -e

  # Surface Sonar (S####) and StyleCop (SA####) / CA lines; keep build noise short.
  errors="$(grep -E 'error (S[0-9]+|SA[0-9]+|CA[0-9]+)' "$log" | sed 's/^[[:space:]]*//' | sort -u || true)"
  warns="$(grep -E 'warning (S[0-9]+|SA[0-9]+|CA[0-9]+|IDE[0-9]+)' "$log" | sed 's/^[[:space:]]*//' | sort -u || true)"

  if [[ -n "$warns" ]]; then
    echo "$warns" | head -40
    warn_count="$(echo "$warns" | wc -l)"
    if [[ "$warn_count" -gt 40 ]]; then
      echo "... ($((warn_count - 40)) more warnings)"
    fi
  fi

  if [[ -n "$errors" ]]; then
    echo "$errors" >&2
    echo "BUILD FAILED: $project (Sonar/analyzer errors)" >&2
    fail=1
    continue
  fi

  if [[ "$rc" -ne 0 ]]; then
    echo "BUILD FAILED: $project (exit $rc)" >&2
    grep -E 'error ' "$log" | tail -40 >&2 || true
    fail=1
    continue
  fi

  if [[ "$strict" -eq 1 && -n "$warns" ]]; then
    echo "STRICT: Sonar/StyleCop warnings present in $project" >&2
    fail=1
  fi
done

if [[ "$fail" -ne 0 ]]; then
  echo
  echo "Lint failed. Fix P0/P1 Sonar errors (see .editorconfig) or run without --strict." >&2
  echo "Deferred rules (S6354/S4055/S4004/S3956/S4136) are silenced per SONAR_GATE.md." >&2
  exit 1
fi

echo
echo "Lint OK — no analyzer errors under Sonar-aligned severities."
