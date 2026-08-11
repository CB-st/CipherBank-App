#!/usr/bin/env bash
# Lint Python with ruff (Sonar-Python stand-in for tooling repos).
# Use: Medium (pre-push when *.py present). Scope: repo Python sources
#
# Usage: ./scripts/lint-python.sh

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
# shellcheck source=lint/lib.sh
source "$ROOT/scripts/lint/lib.sh"
cb_lint_ensure_path

if ! cb_lint_has_any "$ROOT" '*.py'; then
  echo "skip (python): no sources"
  exit 0
fi

if ! command -v ruff >/dev/null 2>&1; then
  echo "ruff not found. Run: ./scripts/lint/install-tools.sh" >&2
  exit 1
fi

config="$ROOT/scripts/lint/configs/ruff.toml"
if [[ -f "$ROOT/ruff.toml" ]]; then
  config="$ROOT/ruff.toml"
elif [[ -f "$ROOT/pyproject.toml" ]] && grep -q '\[tool\.ruff' "$ROOT/pyproject.toml" 2>/dev/null; then
  config=""
fi

echo "==> ruff check"
if [[ -n "$config" ]]; then
  ruff check "$ROOT" --config "$config"
else
  ruff check "$ROOT"
fi
echo "Lint OK (python)"
