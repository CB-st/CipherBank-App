#!/usr/bin/env bash
# Shared helpers for scripts/lint*.sh and scripts/lint/install-tools.sh.
# Use: High (every local lint invocation). Scope: CB-APP / portable org checkout.

# shellcheck disable=SC2034
CB_LINT_LIB_LOADED=1

cb_lint_repo_root() {
  local here
  here="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
  printf '%s' "$here"
}

cb_lint_install_root() {
  printf '%s' "${CB_LINT_HOME:-$HOME/.local/cb-lint}"
}

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
