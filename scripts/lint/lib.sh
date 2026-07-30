#!/usr/bin/env bash
# Shared helpers for scripts/lint*.sh and scripts/lint/install-tools.sh.
# Policy: docs/LOCAL_LINT.md

# shellcheck disable=SC2034
CB_LINT_LIB_LOADED=1

# Resolves the repository root from this library's path.
# Use: High (every lint / install invocation). Scope: process cwd resolution.
cb_lint_repo_root() {
  local here
  here="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
  printf '%s' "$here"
}

# Returns the install prefix for pinned lint binaries ($CB_LINT_HOME or ~/.local/cb-lint).
# Use: High (install + PATH setup). Scope: local developer / agent machine.
cb_lint_install_root() {
  printf '%s' "${CB_LINT_HOME:-$HOME/.local/cb-lint}"
}

# Ensures the cb-lint bin directory exists and is first on PATH.
# Use: High (before any tool invocation). Scope: current shell process.
cb_lint_ensure_path() {
  local bin
  bin="$(cb_lint_install_root)/bin"
  mkdir -p "$bin"
  case ":$PATH:" in
    *":$bin:"*) ;;
    *) export PATH="$bin:$PATH" ;;
  esac
}

# Finds files matching name patterns under ROOT, excluding build/vcs dirs.
# Use: Medium (language auto-detect). Scope: repo tree walk.
# Args: root -- find -name patterns...
cb_lint_find() {
  local root="$1"
  shift
  if [[ "$#" -eq 0 ]]; then
    return 0
  fi
  local args=()
  local first=1
  local pat
  for pat in "$@"; do
    if [[ "$first" -eq 1 ]]; then
      args+=( -name "$pat" )
      first=0
    else
      args+=( -o -name "$pat" )
    fi
  done
  find "$root" \
    \( -path '*/.git/*' -o -path '*/bin/*' -o -path '*/obj/*' -o -path '*/.venv/*' -o -path '*/node_modules/*' -o -path '*/artifacts/*' \) -prune \
    -o \( "${args[@]}" \) -print 2>/dev/null
}

# Returns 0 when at least one matching file exists under root.
# Use: Medium (language auto-detect). Scope: repo tree presence check.
cb_lint_has_any() {
  local root="$1"
  shift
  local f
  while IFS= read -r f; do
    if [[ -n "$f" ]]; then
      return 0
    fi
  done < <(cb_lint_find "$root" "$@")
  return 1
}

# Sources scripts/lint/tool-versions.env into the current shell (version + digest pins).
# Use: High (install-tools). Scope: current shell process env.
cb_lint_load_versions() {
  local root versions
  root="$(cb_lint_repo_root)"
  versions="$root/scripts/lint/tool-versions.env"
  if [[ -f "$versions" ]]; then
    # shellcheck disable=SC1090
    set -a
    # shellcheck disable=SC1091
    source "$versions"
    set +a
  fi
}

# Verifies a downloaded file's SHA-256 against an expected digest (fail closed).
# Use: High (every curl install path). Scope: single asset on disk.
cb_lint_verify_sha256() {
  local file="$1"
  local expected="$2"
  local actual
  if [[ -z "$expected" || "$expected" == "unset" ]]; then
    echo "error: missing SHA-256 pin for $(basename "$file")" >&2
    return 1
  fi
  actual="$(sha256sum "$file" | awk '{print $1}')"
  if [[ "$actual" != "$expected" ]]; then
    echo "error: SHA-256 mismatch for $(basename "$file")" >&2
    echo "  expected: $expected" >&2
    echo "  actual:   $actual" >&2
    return 1
  fi
}
