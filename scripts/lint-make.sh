#!/usr/bin/env bash
# Lint Makefiles with checkmake (build-system hygiene for org repos).
# Use: Medium (pre-push when Makefile present). Scope: Makefile / *.mk
#
# Usage: ./scripts/lint-make.sh

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=lint/lib.sh
source "$ROOT/scripts/lint/lib.sh"
cb_lint_ensure_path

mapfile -t files < <(cb_lint_find "$ROOT" 'Makefile' 'makefile' 'GNUmakefile' '*.mk')
# also plain Makefile at root if find missed case
if [[ -f "$ROOT/Makefile" ]]; then
  files+=("$ROOT/Makefile")
fi

# dedupe
if [[ "${#files[@]}" -gt 0 ]]; then
  mapfile -t files < <(printf '%s\n' "${files[@]}" | awk 'NF && !seen[$0]++')
fi

if [[ "${#files[@]}" -eq 0 ]]; then
  echo "skip (make): no sources"
  exit 0
fi

if ! command -v checkmake >/dev/null 2>&1; then
  echo "checkmake not found (Makefiles present). Run: ./scripts/lint/install-tools.sh" >&2
  exit 1
fi

config="$ROOT/scripts/lint/configs/checkmake.ini"
echo "==> checkmake (${#files[@]} files)"
fail=0
for f in "${files[@]}"; do
  if ! checkmake --config "$config" "$f"; then
    fail=1
  fi
done

if [[ "$fail" -ne 0 ]]; then
  exit 1
fi
echo "Lint OK (make)"
