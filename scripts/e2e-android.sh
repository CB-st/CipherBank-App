#!/usr/bin/env bash
# CipherBank MAUI Android Appium E2E harness.
# Boots the AVD, builds/installs the app, starts Appium, and runs the
# requested story/wave/full E2E filter against CipherBank-app.E2ETests.
#
# Usage:
#   ./scripts/e2e-android.sh --story CB-ACCOUNT-001
#   ./scripts/e2e-android.sh --wave account
#   ./scripts/e2e-android.sh --all
#   ./scripts/e2e-android.sh --help
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

# shellcheck source=scripts/lib/android-env.sh
source "$ROOT/scripts/lib/android-env.sh"

APP_PROJECT="CipherBank-app/CipherBank-app.csproj"
E2E_PROJECT="CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj"
TARGET_FRAMEWORK="net10.0-android"
APK_SEARCH_DIR="CipherBank-app/bin/Debug/$TARGET_FRAMEWORK"
APPIUM_PORT="${APPIUM_PORT:-4723}"
APPIUM_LOG="${APPIUM_LOG:-/tmp/cb-e2e-appium.log}"
EMULATOR_LOG="${EMULATOR_LOG:-/tmp/cb-e2e-emulator.log}"
BOOT_WAIT_ATTEMPTS="${BOOT_WAIT_ATTEMPTS:-90}"

# Maps a --wave name to the space-separated FullyQualifiedName substrings that make up that
# wave's dotnet test filter. Most waves are a single story-id prefix; "account" needs an extra
# two entries because the US-ONB-03/04 negative Facts live in AccountStories.cs but keep their
# US_ONB_* method-name prefix instead of CB_ACCOUNT_* (see StoryIds.cs / AccountStories.cs).
# Use: Medium (once per --wave invocation). Scope: arg -> filter resolution.
declare -A WAVE_STORY_PREFIXES=(
  [account]="CB_ACCOUNT US_ONB_03 US_ONB_04"
  [market]="CB_MARKET"
  [wallets]="CB_WALLET"
  [fund]="CB_FUND"
  [pay]="CB_PAY"
  [cards]="CB_CARD"
)

# Prints CLI usage/help text.
# Use: Low (only on --help or an arg error). Scope: process-wide stdout.
print_usage() {
  cat <<'EOF'
CipherBank MAUI Android Appium E2E harness

Usage:
  scripts/e2e-android.sh --story <CB-ID>   Run one story (e.g. CB-ACCOUNT-001)
  scripts/e2e-android.sh --wave <name>     Run one wave (account|market|wallets|fund|pay|cards)
  scripts/e2e-android.sh --all             Run the full E2E suite
  scripts/e2e-android.sh --help            Show this help

Environment overrides:
  E2E_TEST_PIN       Unlock PIN journaled for the test run (default: 246810)
  E2E_JOURNAL_DIR    Story journal output dir (default: artifacts/e2e-journal)
  APPIUM_PORT        Appium server port (default: 4723)

Requires: CB_AVD emulator image, ANDROID_HOME, DOTNET_ROOT (see scripts/lib/android-env.sh).
EOF
}

# Writes a timestamped progress line to stderr so stdout stays clean for test output.
# Use: High (every phase of a harness run). Scope: process-wide logging.
log() { echo "==> $*" >&2; }

# Prints an error to stderr and exits non-zero.
# Use: Low (only on unrecoverable setup failures). Scope: process-wide.
die() { echo "ERROR: $*" >&2; exit 1; }

# Parses CLI flags into MODE/MODE_VALUE globals via case-based dispatch.
# Use: High (every invocation). Scope: script entry point.
parse_args() {
  MODE=""
  MODE_VALUE=""
  case "${1:-}" in
    --story)
      MODE="story"
      MODE_VALUE="${2:-}"
      [[ -n "$MODE_VALUE" ]] || die "--story requires a CB-* id, e.g. --story CB-ACCOUNT-001"
      ;;
    --wave)
      MODE="wave"
      MODE_VALUE="${2:-}"
      [[ -n "$MODE_VALUE" ]] || die "--wave requires a wave name, e.g. --wave account"
      ;;
    --all)
      MODE="all"
      ;;
    --help|-h|"")
      print_usage
      exit 0
      ;;
    *)
      print_usage
      die "unrecognized argument: ${1:-}"
      ;;
  esac
}

# Resolves the --story/--wave/--all selection into a dotnet test --filter expression.
# Use: High (every run). Scope: story/wave -> FullyQualifiedName filter mapping.
resolve_test_filter() {
  case "$MODE" in
    story)
      local sanitized="${MODE_VALUE//-/_}"
      echo "FullyQualifiedName~${sanitized}"
      ;;
    wave)
      local prefixes="${WAVE_STORY_PREFIXES[$MODE_VALUE]:-}"
      [[ -n "$prefixes" ]] || die "unknown wave '$MODE_VALUE' (known: ${!WAVE_STORY_PREFIXES[*]})"
      join_wave_filter "$prefixes"
      ;;
    all)
      echo ""
      ;;
  esac
}

# Joins a wave's space-separated FullyQualifiedName substrings into a single dotnet test
# --filter OR-expression, e.g. "CB_ACCOUNT US_ONB_03" -> "FullyQualifiedName~CB_ACCOUNT|FullyQualifiedName~US_ONB_03".
# Use: Medium (once per --wave invocation). Scope: single filter-string build.
join_wave_filter() {
  local prefixes="$1"
  local -a clauses=()
  local prefix
  for prefix in $prefixes; do
    clauses+=("FullyQualifiedName~${prefix}")
  done
  local IFS='|'
  echo "${clauses[*]}"
}

# Starts CB_AVD if no emulator- device is already attached, then waits for boot.
# Use: Medium (once per harness run; skipped if a device is already up). Scope: single AVD instance.
ensure_emulator_running() {
  if adb devices 2>/dev/null | grep -qE '^emulator-[0-9]+[[:space:]]+device$'; then
    log "Emulator already running"
    return
  fi
  log "Starting emulator $CB_AVD (log: $EMULATOR_LOG)"
  nohup emulator -avd "$CB_AVD" -netdelay none -netspeed full -gpu auto \
    >"$EMULATOR_LOG" 2>&1 &
  disown
  wait_for_boot_completed
}

# Polls sys.boot_completed until the emulator finishes booting or attempts run out.
# Use: Medium (only right after a cold emulator start). Scope: single AVD instance.
wait_for_boot_completed() {
  log "Waiting for device boot..."
  adb wait-for-device
  local attempt boot
  for ((attempt = 0; attempt < BOOT_WAIT_ATTEMPTS; attempt++)); do
    boot="$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r' || true)"
    [[ "$boot" == "1" ]] && { log "Emulator booted"; return; }
    sleep 2
  done
  die "Emulator did not finish booting — check $EMULATOR_LOG"
}

# Builds the MAUI Android app with assemblies embedded so the APK is self-contained.
# Use: Medium (once per harness run). Scope: CipherBank-app project build output.
build_apk() {
  log "Building $APP_PROJECT ($TARGET_FRAMEWORK)"
  dotnet build "$APP_PROJECT" \
    -f "$TARGET_FRAMEWORK" -c Debug -p:EmbedAssembliesIntoApk=true
}

# Finds the freshly built debug APK under the app's Android bin output and prints an absolute path —
# dotnet test's process cwd is the test assembly's bin/ dir, not $ROOT, so a path relative to $ROOT would
# resolve to the wrong location once ANDROID_APK_PATH reaches AppiumFixture.
# Use: Medium (once per harness run). Scope: single build artifact resolution.
locate_apk() {
  local apk
  apk="$(find "$APK_SEARCH_DIR" -maxdepth 1 -iname '*-Signed.apk' -print -quit 2>/dev/null)"
  [[ -n "$apk" ]] || apk="$(find "$APK_SEARCH_DIR" -maxdepth 1 -iname '*.apk' -print -quit 2>/dev/null)"
  [[ -n "$apk" ]] || die "no APK found under $APK_SEARCH_DIR — did the build succeed?"
  echo "$ROOT/$apk"
}

# Installs (or reinstalls) the given APK onto the running device.
# Use: Medium (once per harness run). Scope: single AVD instance.
install_apk() {
  local apk="$1"
  log "Installing $apk"
  adb install -r "$apk"
}

# Starts an Appium (UiAutomator2) server on APPIUM_PORT if one isn't already listening.
# Use: Medium (once per harness run; skipped if Appium is already up). Scope: local Appium process.
ensure_appium_running() {
  if curl -fsS -m 2 "http://localhost:$APPIUM_PORT/status" >/dev/null 2>&1; then
    log "Appium already running on :$APPIUM_PORT"
    return
  fi
  log "Starting Appium on :$APPIUM_PORT (log: $APPIUM_LOG)"
  nohup npx --yes appium --port "$APPIUM_PORT" >"$APPIUM_LOG" 2>&1 &
  disown
  local attempt
  for ((attempt = 0; attempt < 30; attempt++)); do
    curl -fsS -m 2 "http://localhost:$APPIUM_PORT/status" >/dev/null 2>&1 && { log "Appium ready"; return; }
    sleep 1
  done
  die "Appium did not become ready on :$APPIUM_PORT — check $APPIUM_LOG"
}

# Runs the E2E suite against the installed APK, scoped by the resolved filter.
# Use: High (every harness run). Scope: CipherBank-app.E2ETests process.
run_e2e_tests() {
  local apk="$1" filter="$2"
  local journal_dir="${E2E_JOURNAL_DIR:-artifacts/e2e-journal}"
  mkdir -p "$journal_dir"
  local -a test_args=("$E2E_PROJECT" --nologo)
  [[ -n "$filter" ]] && test_args+=(--filter "$filter")
  log "Running: dotnet test ${test_args[*]}"
  E2E_RUN=1 TEST_PLATFORM=android \
    ANDROID_APK_PATH="$apk" \
    E2E_TEST_PIN="${E2E_TEST_PIN:-246810}" \
    E2E_JOURNAL_DIR="$journal_dir" \
    dotnet test "${test_args[@]}"
}

# Orchestrates the full harness run: emulator -> build -> install -> Appium -> tests.
# Use: High (every invocation). Scope: whole script process.
main() {
  parse_args "$@"
  local filter
  filter="$(resolve_test_filter)"
  ensure_emulator_running
  build_apk
  local apk
  apk="$(locate_apk)"
  install_apk "$apk"
  ensure_appium_running
  run_e2e_tests "$apk" "$filter"
}

main "$@"
