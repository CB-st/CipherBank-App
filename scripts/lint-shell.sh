#!/usr/bin/env bash
# Lint shell scripts with shellcheck (org-ready local gate).
# Use: Medium (pre-push). Scope: scripts/**/*.sh and root *.sh
#
# Usage: ./scripts/lint-shell.sh

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=lint/lib.sh
source "$ROOT/scripts/lint/lib.sh"
cb_lint_ensure_path

mapfile -t files < <(cb_lint_find "$ROOT" '*.sh')
if [[ "${#files[@]}" -eq 0 ]]; then
  echo "skip (shell): no sources"
  exit 0
fi

if ! command -v shellcheck >/dev/null 2>&1; then
  echo "shellcheck not found. Run: ./scripts/lint/install-tools.sh" >&2
  exit 1
fi

echo "==> shellcheck (${#files[@]} files)"
# SC1090/SC1091: shared lib paths are resolved at runtime via ROOT.
shellcheck -x -e SC1090,SC1091 "${files[@]}"
echo "Lint OK (shell)"
