#!/usr/bin/env bash
# Lint C/C++ with clang-tidy (Sonar C-family stand-in for backend repos).
# Use: Medium (pre-push when C++ sources present). Scope: *.c/cc/cpp/h/hpp + CMake
#
# Usage: ./scripts/lint-cpp.sh

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=lint/lib.sh
source "$ROOT/scripts/lint/lib.sh"
cb_lint_ensure_path

has_cmake=0
if [[ -f "$ROOT/CMakeLists.txt" ]]; then
  has_cmake=1
fi

mapfile -t files < <(cb_lint_find "$ROOT" '*.c' '*.cc' '*.cpp' '*.cxx' '*.h' '*.hpp' '*.hxx')
if [[ "${#files[@]}" -eq 0 && "$has_cmake" -eq 0 ]]; then
  echo "skip (cpp): no sources"
  exit 0
fi

if ! command -v clang-tidy >/dev/null 2>&1; then
  echo "clang-tidy not found (sources present). Install llvm/clang-tidy, then re-run." >&2
  exit 1
fi

tidy_config="$ROOT/.clang-tidy"
if [[ ! -f "$tidy_config" ]]; then
  tidy_config="$ROOT/scripts/lint/configs/.clang-tidy"
fi

echo "==> clang-tidy (${#files[@]} translation units)"
if [[ "${#files[@]}" -eq 0 ]]; then
  echo "CMakeLists.txt present but no C/C++ sources found under tree — nothing to tidy"
  exit 0
fi

fail=0
for f in "${files[@]}"; do
  case "$f" in
    *.h|*.hpp|*.hxx) continue ;; # headers via TU includes; skip lone headers
  esac
  if ! clang-tidy -quiet -config-file="$tidy_config" "$f" -- -std=c++17; then
    fail=1
  fi
done

if [[ "$fail" -ne 0 ]]; then
  exit 1
fi

if command -v clang-format >/dev/null 2>&1; then
  echo "==> clang-format --dry-run (non-fatal style)"
  clang-format --dry-run --Werror "${files[@]}" 2>/dev/null || echo "clang-format style diffs present (non-fatal)"
fi

echo "Lint OK (cpp)"
