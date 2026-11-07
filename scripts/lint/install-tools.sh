#!/usr/bin/env bash
# Installs pinned local lint tools into ~/.local/cb-lint/bin (or $CB_LINT_HOME).
# Does not install compilers — only linters. Prefer existing PATH binaries when present.
#
# Usage:
#   ./scripts/lint/install-tools.sh
#   ./scripts/lint/install-tools.sh --force   # re-download even if on PATH
#
# Policy: docs/LOCAL_LINT.md · versions: scripts/lint/tool-versions.env

set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
# shellcheck source=lib.sh
source "$ROOT/scripts/lint/lib.sh"

cb_lint_load_versions
cb_lint_ensure_path

FORCE=0
for arg in "$@"; do
  case "$arg" in
    --force) FORCE=1 ;;
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

INSTALL_ROOT="$(cb_lint_install_root)"
BIN="$INSTALL_ROOT/bin"
mkdir -p "$BIN"

os="$(uname -s | tr '[:upper:]' '[:lower:]')"
arch="$(uname -m)"
case "$arch" in
  x86_64|amd64) arch_norm=x86_64 ;;
  aarch64|arm64) arch_norm=aarch64 ;;
  *) arch_norm="$arch" ;;
esac

# Returns 0 when the named tool must be installed (missing or --force).
# Use: High (each install_*). Scope: install-tools decision.
need_tool() {
  local name="$1"
  if [[ "$FORCE" -eq 1 ]]; then
    return 0
  fi
  if command -v "$name" >/dev/null 2>&1; then
    echo "ok: $name already on PATH ($(command -v "$name"))"
    return 1
  fi
  return 0
}

# Looks up PREFIX_SHA256_${os}_${arch} pins from tool-versions.env.
# Use: High (curl install paths). Scope: install-tools digest dispatch.
digest_for() {
  local prefix="$1"
  local key="${prefix}_${os}_${arch_norm}"
  key="${key//-/_}"
  # shellcheck disable=SC2086
  eval "printf '%s' \"\${$key:-}\""
}

# Downloads and installs pinned shellcheck when missing.
# Use: Medium (install-tools). Scope: cb-lint bin prefix.
install_shellcheck() {
  local ver="${SHELLCHECK_VERSION:-0.10.0}"
  if ! need_tool shellcheck; then
    return 0
  fi
  local asset
  case "$os-$arch_norm" in
    linux-x86_64) asset="shellcheck-v${ver}.linux.x86_64.tar.xz" ;;
    linux-aarch64) asset="shellcheck-v${ver}.linux.aarch64.tar.xz" ;;
    darwin-x86_64) asset="shellcheck-v${ver}.darwin.x86_64.tar.xz" ;;
    darwin-aarch64) asset="shellcheck-v${ver}.darwin.aarch64.tar.xz" ;;
    *)
      echo "warn: no shellcheck asset for $os-$arch_norm — install via package manager" >&2
      return 0
      ;;
  esac
  local url="https://github.com/koalaman/shellcheck/releases/download/v${ver}/${asset}"
  local expected
  expected="$(digest_for SHELLCHECK_SHA256)"
  local tmp
  tmp="$(mktemp -d)"
  echo "==> shellcheck v${ver}"
  curl -fsSL "$url" -o "$tmp/$asset"
  cb_lint_verify_sha256 "$tmp/$asset" "$expected"
  tar -xJf "$tmp/$asset" -C "$tmp"
  install -m 0755 "$tmp/shellcheck-v${ver}/shellcheck" "$BIN/shellcheck"
  rm -rf "$tmp"
  echo "installed: $BIN/shellcheck"
}




echo "Install root: $INSTALL_ROOT"
install_shellcheck
echo
echo "Done. Ensure PATH includes: $BIN"
echo "Then: ./scripts/lint.sh"
