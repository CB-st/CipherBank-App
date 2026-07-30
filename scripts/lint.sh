#!/usr/bin/env bash
# Org-ready local lint dispatcher (C# / shell / Python / C++ / Make).
# Auto-detects languages with sources; skips the rest.
#
# Usage:
#   ./scripts/lint.sh                 # auto-detect
#   ./scripts/lint.sh csharp shell    # subset
#   ./scripts/lint.sh --install       # install pinned tools only
#   ./scripts/lint.sh --strict        # C# fail on warnings
#   ./scripts/lint.sh --core-only     # C# Core+Tests only (M1)
#
# Spec: docs/superpowers/specs/2026-07-30-local-multi-lang-lint-design.md
# Docs: docs/LOCAL_LINT.md

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=lint/lib.sh
source "$ROOT/scripts/lint/lib.sh"
cb_lint_ensure_path

install_only=0
strict=0
core_only=0
langs=()

for arg in "$@"; do
  case "$arg" in
    --install) install_only=1 ;;
    --strict) strict=1 ;;
    --core-only) core_only=1 ;;
    -h|--help)
      sed -n '2,16p' "$0"
      exit 0
      ;;
    csharp|shell|python|cpp|make)
      langs+=("$arg")
      ;;
    *)
      echo "Unknown arg: $arg" >&2
      exit 2
      ;;
  esac
done

if [[ "$install_only" -eq 1 ]]; then
  exec "$ROOT/scripts/lint/install-tools.sh"
fi

detect_langs() {
  local out=()
  if cb_lint_has_any "$ROOT" '*.csproj' || cb_lint_has_any "$ROOT" '*.cs'; then
    out+=(csharp)
  fi
  if cb_lint_has_any "$ROOT" '*.sh'; then
    out+=(shell)
  fi
  if cb_lint_has_any "$ROOT" '*.py'; then
    out+=(python)
  fi
  if cb_lint_has_any "$ROOT" '*.c' '*.cc' '*.cpp' '*.cxx' '*.h' '*.hpp' '*.hxx' \
    || [[ -f "$ROOT/CMakeLists.txt" ]]; then
    out+=(cpp)
  fi
  if cb_lint_has_any "$ROOT" 'Makefile' 'makefile' 'GNUmakefile' '*.mk' \
    || [[ -f "$ROOT/Makefile" ]]; then
    out+=(make)
  fi
  printf '%s\n' "${out[@]}"
}

if [[ "${#langs[@]}" -eq 0 ]]; then
  mapfile -t langs < <(detect_langs)
fi

if [[ "${#langs[@]}" -eq 0 ]]; then
  echo "No lintable languages detected."
  exit 0
fi

echo "==> local lint: ${langs[*]}"
echo "    Docs: docs/LOCAL_LINT.md"
echo

fail=0
for lang in "${langs[@]}"; do
  case "$lang" in
    csharp)
      args=()
      [[ "$strict" -eq 1 ]] && args+=(--strict)
      [[ "$core_only" -eq 1 ]] && args+=(--core-only)
      if ! "$ROOT/scripts/lint-csharp.sh" "${args[@]+"${args[@]}"}"; then
        fail=1
      fi
      ;;
    shell)
      if ! "$ROOT/scripts/lint-shell.sh"; then fail=1; fi
      ;;
    python)
      if ! "$ROOT/scripts/lint-python.sh"; then fail=1; fi
      ;;
    cpp)
      if ! "$ROOT/scripts/lint-cpp.sh"; then fail=1; fi
      ;;
    make)
      if ! "$ROOT/scripts/lint-make.sh"; then fail=1; fi
      ;;
  esac
  echo
done

if [[ "$fail" -ne 0 ]]; then
  echo "Lint failed." >&2
  exit 1
fi
echo "All requested language lints OK."
