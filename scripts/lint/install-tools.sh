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

# Downloads and installs pinned ruff (standalone tarball preferred).
# Use: Medium (install-tools). Scope: cb-lint bin prefix.
install_ruff() {
  local ver="${RUFF_VERSION:-0.12.4}"
  if ! need_tool ruff; then
    return 0
  fi

  # Prefer official standalone binaries (works under PEP 668 / no pipx).
  # Asset names omit the version: ruff-<triple>.tar.gz
  local target=""
  case "$os-$arch_norm" in
    linux-x86_64) target="ruff-x86_64-unknown-linux-gnu" ;;
    linux-aarch64) target="ruff-aarch64-unknown-linux-gnu" ;;
    darwin-x86_64) target="ruff-x86_64-apple-darwin" ;;
    darwin-aarch64) target="ruff-aarch64-apple-darwin" ;;
  esac

  [[ -n "$target" ]] || {
    echo "error: unsupported OS/arch for hashed ruff install: ${os}-${arch_norm}" >&2
    return 1
  }

  local url="https://github.com/astral-sh/ruff/releases/download/${ver}/${target}.tar.gz"
  local expected
  expected="$(digest_for RUFF_SHA256)"
  [[ -n "$expected" ]] || {
    echo "error: missing RUFF_SHA256 digest for ${os}-${arch_norm} in tool-versions.env" >&2
    return 1
  }
  local tmp
  tmp="$(mktemp -d)"
  echo "==> ruff ${ver} (standalone, hash-verified)"
  if ! curl -fsSL "$url" -o "$tmp/ruff.tgz"; then
    rm -rf "$tmp"
    echo "error: ruff download failed for ${url}" >&2
    return 1
  fi
  if ! cb_lint_verify_sha256 "$tmp/ruff.tgz" "$expected"; then
    rm -rf "$tmp"
    echo "error: ruff tarball digest mismatch (refusing unverified fallbacks)" >&2
    return 1
  fi
  tar -xzf "$tmp/ruff.tgz" -C "$tmp"
  local binpath
  binpath="$(find "$tmp" -type f -name ruff | head -1)"
  if [[ -z "$binpath" ]]; then
    rm -rf "$tmp"
    echo "error: ruff binary missing from verified tarball" >&2
    return 1
  fi
  install -m 0755 "$binpath" "$BIN/ruff"
  rm -rf "$tmp"
  echo "installed: $BIN/ruff"
}

# Downloads checkmake release asset (or go install) when missing.
# Use: Medium (install-tools). Scope: cb-lint bin prefix.
install_checkmake() {
  local ver="${CHECKMAKE_VERSION:-0.2.2}"
  if ! need_tool checkmake; then
    return 0
  fi

  # Prefer go install when available (release asset names vary by tag).
  if command -v go >/dev/null 2>&1; then
    echo "==> checkmake ${ver} via go install"
    GOBIN="$BIN" go install "github.com/mrtazz/checkmake/cmd/checkmake@${ver}" \
      || GOBIN="$BIN" go install "github.com/mrtazz/checkmake/cmd/checkmake@v${ver}" \
      || true
    if [[ -x "$BIN/checkmake" ]]; then
      echo "installed: $BIN/checkmake"
      return 0
    fi
  fi

  local asset
  case "$os-$arch_norm" in
    linux-x86_64) asset="checkmake-${ver}.linux.amd64" ;;
    darwin-x86_64) asset="checkmake-${ver}.darwin.amd64" ;;
    linux-aarch64|darwin-aarch64)
      echo "warn: no checkmake ${ver} release asset for $os-$arch_norm — install go and re-run, or skip make lint" >&2
      return 0
      ;;
    *)
      echo "warn: no checkmake asset for $os-$arch_norm — install go and re-run, or skip make lint" >&2
      return 0
      ;;
  esac
  local url="https://github.com/mrtazz/checkmake/releases/download/${ver}/${asset}"
  local expected
  expected="$(digest_for CHECKMAKE_SHA256)"
  echo "==> checkmake ${ver} (release asset)"
  if curl -fsSL "$url" -o "$BIN/checkmake"; then
    if ! cb_lint_verify_sha256 "$BIN/checkmake" "$expected"; then
      rm -f "$BIN/checkmake"
      return 1
    fi
    chmod 0755 "$BIN/checkmake"
    echo "installed: $BIN/checkmake"
  else
    rm -f "$BIN/checkmake"
    echo "warn: checkmake download failed — make lint will skip until installed" >&2
  fi
}

# Reports whether clang-tidy / clang-format are available (no download).
# Use: Low (end of install-tools). Scope: operator hints.
verify_clang() {
  if command -v clang-tidy >/dev/null 2>&1; then
    echo "ok: clang-tidy ($(command -v clang-tidy))"
  else
    echo "hint: clang-tidy not found — install llvm/clang (e.g. apt install clang-tidy) for C++ lint"
  fi
  if command -v clang-format >/dev/null 2>&1; then
    echo "ok: clang-format ($(command -v clang-format))"
  else
    echo "hint: clang-format not found — optional for C++ style dry-run"
  fi
}

echo "Install root: $INSTALL_ROOT"
install_shellcheck
install_ruff
install_checkmake
verify_clang
echo
echo "Done. Ensure PATH includes: $BIN"
echo "Then: ./scripts/lint.sh"
