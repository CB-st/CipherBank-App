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
# Pin Appium + UiAutomator2 so clean machines don't fetch floating majors mid-run.
APPIUM_VERSION="${APPIUM_VERSION:-2.19.0}"
APPIUM_UIAUTOMATOR2_VERSION="${APPIUM_UIAUTOMATOR2_VERSION:-3.9.6}"
APPIUM_LOG="${APPIUM_LOG:-/tmp/cb-e2e-appium.log}"
EMULATOR_LOG="${EMULATOR_LOG:-/tmp/cb-e2e-emulator.log}"
BOOT_WAIT_ATTEMPTS="${BOOT_WAIT_ATTEMPTS:-90}"

# Planned wave → Story trait map for when CipherBank-app.E2ETests ships Facts (M7).
# On this harness branch there are no Story traits; --story/--wave/--all defer (see e2e_story_facts_ready).
# Use: Medium (once per --wave invocation). Scope: arg -> filter resolution.
declare -A WAVE_STORIES=(
  [account]="CB-ACCOUNT-001 CB-ACCOUNT-002 CB-ACCOUNT-PIN-CHANGE US-ONB-03 US-ONB-04"
  [market]="CB-MARKET-001"
  [wallets]="CB-WALLET-001 CB-WALLET-002"
  [fund]="CB-FUND-001"
  [pay]="CB-PAY-001 CB-PAY-003"
  [cards]="CB-CARD-001"
)

# Prints CLI usage/help text.
# Use: Low (only on --help or an arg error). Scope: process-wide stdout.
print_usage() {
  cat <<'EOF'
CipherBank MAUI Android Appium E2E harness

Usage:
  scripts/e2e-android.sh --story <CB-ID>   Run one story (e.g. CB-ACCOUNT-001)
  scripts/e2e-android.sh --wave <name>     Run one wave (account|market|wallets|fund|pay|cards)
  scripts/e2e-android.sh --all             Fresh AccountStories then sealed smoke (M7 Story Facts required)
  scripts/e2e-android.sh --help            Show this help

Harness credentials (required once Story-trait Facts land on M7):
  E2E_TEST_PIN / E2E_TEST_PIN_ALT / E2E_RECOVERY_PASSWORD
  Copy docs/tests/e2e-local.env.example → artifacts/e2e-local.env (gitignored)

Until CipherBank-app.E2ETests contains [Trait("Story", …)] Facts, --story/--wave/--all
exit with a clear deferral (those Facts ship on M7 / prototype/maui-m7).

Requires: CB_AVD emulator image, ANDROID_HOME, DOTNET_ROOT (see scripts/lib/android-env.sh).
EOF
}

# Writes a timestamped progress line to stderr so stdout stays clean for test output.
# Use: High (every phase of a harness run). Scope: process-wide logging.
log() { echo "==> $*" >&2; }

# Prints an error to stderr and exits non-zero.
# Use: Low (only on unrecoverable setup failures). Scope: process-wide.
die() { echo "ERROR: $*" >&2; exit 1; }

# True when E2E Facts declare [Trait("Story", …)] (account wave / M4).
# Use: High (arg + credential gates). Scope: CipherBank-app.E2ETests tree.
e2e_story_facts_ready() {
  grep -R --include='*.cs' -E 'Trait\(\s*"Story"' "$ROOT/CipherBank-app.E2ETests" >/dev/null 2>&1
}

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
      [[ -z "${3:-}" ]] || die "--story does not accept trailing arguments: ${3}"
      ;;
    --wave)
      MODE="wave"
      MODE_VALUE="${2:-}"
      [[ -n "$MODE_VALUE" ]] || die "--wave requires a wave name, e.g. --wave account"
      [[ -z "${3:-}" ]] || die "--wave does not accept trailing arguments: ${3}"
      ;;
    --all)
      MODE="all"
      [[ -z "${2:-}" ]] || die "--all does not accept trailing arguments: ${2}"
      ;;
    --help|-h)
      [[ -z "${2:-}" ]] || die "--help does not accept trailing arguments: ${2}"
      print_usage
      exit 0
      ;;
    "")
      print_usage >&2
      echo "ERROR: missing required argument (use --help for usage)" >&2
      exit 2
      ;;
    *)
      print_usage >&2
      die "unrecognized argument: ${1:-}"
      ;;
  esac

  if [[ "$MODE" == "story" || "$MODE" == "wave" || "$MODE" == "all" ]] && ! e2e_story_facts_ready; then
    die "--${MODE} requires [Trait(\"Story\", …)] Facts under CipherBank-app.E2ETests (lands on M7 / prototype/maui-m7). This APK does not implement CriticalUserJourneyTests purchase controls."
  fi
}

# Resolves the --story/--wave/--all selection into a dotnet test --filter expression using Story traits.
# Use: High (every run). Scope: story/wave -> Story= filter mapping.
resolve_test_filter() {
  case "$MODE" in
    story)
      echo "Story=${MODE_VALUE}"
      ;;
    wave)
      local stories="${WAVE_STORIES[$MODE_VALUE]:-}"
      [[ -n "$stories" ]] || die "unknown wave '$MODE_VALUE' (known: ${!WAVE_STORIES[*]})"
      join_story_filter "$stories"
      ;;
    all)
      echo ""
      ;;
  esac
}

# Joins space-separated story IDs into a Story= OR-filter, e.g. "CB-ACCOUNT-001 US-ONB-03" ->
# "Story=CB-ACCOUNT-001|Story=US-ONB-03".
# Use: Medium (once per --wave). Scope: single filter-string build.
join_story_filter() {
  local stories="$1"
  local -a clauses=()
  local story
  for story in $stories; do
    clauses+=("Story=${story}")
  done
  local IFS='|'
  echo "${clauses[*]}"
}

# Fails the harness if the resolved filter matches zero discovered tests (no silent empty runs).
# Use: High (every filtered run). Scope: CipherBank-app.E2ETests discovery.
preflight_filter_or_die() {
  local filter="$1"
  [[ -n "$filter" ]] || return 0
  log "Preflight: listing tests for filter '$filter'"
  local discovery
  discovery="$(dotnet test "$E2E_PROJECT" --nologo --no-restore --list-tests --filter "$filter" 2>&1 || true)"
  if ! grep -qE '^[[:space:]]+CipherBank_app\.' <<<"$discovery"; then
    printf '%s\n' "$discovery" >&2
    die "filter matched zero tests: $filter"
  fi
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
  log "Clearing ${CB_MAUI_PACKAGE} application data"
  adb shell pm clear "$CB_MAUI_PACKAGE" >/dev/null \
    || die "failed to pm clear $CB_MAUI_PACKAGE after install"
}

# Ensures the pinned UiAutomator2 driver is installed for APPIUM_VERSION.
# Use: Medium (once per harness run before starting/reusing Appium). Scope: local npm/appium install.
ensure_appium_driver() {
  local list
  log "Ensuring Appium ${APPIUM_VERSION} + UiAutomator2 ${APPIUM_UIAUTOMATOR2_VERSION}"
  list="$(npx --yes "appium@${APPIUM_VERSION}" driver list --installed 2>/dev/null || true)"
  if ! grep -qiE "uiautomator2@${APPIUM_UIAUTOMATOR2_VERSION}([[:space:]@]|$)" <<<"$list"; then
    log "Installing/replacing uiautomator2@${APPIUM_UIAUTOMATOR2_VERSION} (current: ${list:-none})"
    npx --yes "appium@${APPIUM_VERSION}" driver uninstall uiautomator2 >/dev/null 2>&1 || true
    npx --yes "appium@${APPIUM_VERSION}" driver install "uiautomator2@${APPIUM_UIAUTOMATOR2_VERSION}" \
      || die "failed to install Appium UiAutomator2 driver ${APPIUM_UIAUTOMATOR2_VERSION}"
    list="$(npx --yes "appium@${APPIUM_VERSION}" driver list --installed 2>/dev/null || true)"
  fi
  grep -qiE "uiautomator2@${APPIUM_UIAUTOMATOR2_VERSION}([[:space:]@]|$)" <<<"$list" \
    || die "UiAutomator2 ${APPIUM_UIAUTOMATOR2_VERSION} not installed for Appium ${APPIUM_VERSION} — got: ${list}"
}

# Reads /status from a listening Appium and fails when build.version ≠ APPIUM_VERSION.
# Use: Medium (reuse path). Scope: local Appium process on APPIUM_PORT.
assert_running_appium_pin() {
  local status body version
  status="$(curl -fsS -m 2 "http://localhost:$APPIUM_PORT/status" 2>/dev/null)" \
    || die "Appium on :$APPIUM_PORT returned no /status — refuse unknown server"
  body="$status"
  version="$(python3 -c 'import json,sys
raw=sys.stdin.read()
try:
  d=json.loads(raw)
except Exception as exc:
  raise SystemExit(f"unreadable Appium /status JSON: {exc}")
# Appium 2: value.build.version; some builds nest under value.value
v=d
for key in ("value","build","version"):
  if isinstance(v, dict) and key in v:
    v=v[key]
  else:
    v=None
    break
if not isinstance(v, str) or not v:
  # fallback: walk for build.version
  def walk(o):
    if isinstance(o, dict):
      b=o.get("build")
      if isinstance(b, dict) and isinstance(b.get("version"), str):
        return b["version"]
      for x in o.values():
        r=walk(x)
        if r: return r
    return None
  v=walk(d)
if not isinstance(v, str) or not v:
  raise SystemExit("Appium /status missing build.version")
print(v)' <<<"$body")" \
    || die "could not parse Appium version from :$APPIUM_PORT /status"
  [[ "$version" == "$APPIUM_VERSION" || "$version" == ${APPIUM_VERSION}* ]] \
    || die "Appium on :$APPIUM_PORT is ${version}, need ${APPIUM_VERSION} — stop the foreign server or change APPIUM_PORT"
  ensure_appium_driver
  log "Reusing Appium ${version} on :$APPIUM_PORT (pin ${APPIUM_VERSION}, UiAutomator2 ${APPIUM_UIAUTOMATOR2_VERSION})"
}

# Starts a pinned Appium (UiAutomator2) server on APPIUM_PORT if one isn't already listening.
# Use: Medium (once per harness run; reuses only when pin matches). Scope: local Appium process.
ensure_appium_running() {
  if curl -fsS -m 2 "http://localhost:$APPIUM_PORT/status" >/dev/null 2>&1; then
    assert_running_appium_pin
    return
  fi
  ensure_appium_driver
  log "Starting Appium ${APPIUM_VERSION} on :$APPIUM_PORT (log: $APPIUM_LOG)"
  nohup npx --yes "appium@${APPIUM_VERSION}" --port "$APPIUM_PORT" >"$APPIUM_LOG" 2>&1 &
  disown
  local attempt
  for ((attempt = 0; attempt < 30; attempt++)); do
    curl -fsS -m 2 "http://localhost:$APPIUM_PORT/status" >/dev/null 2>&1 && { log "Appium ready"; return; }
    sleep 1
  done
  die "Appium did not become ready on :$APPIUM_PORT — check $APPIUM_LOG"
}

# Loads gitignored lab credentials from artifacts/e2e-local.env into the
# process environment when present, then requires PIN / alt / recovery password to be set.
# Required once Story-trait Facts exist (M7). --all is fenced until then.
# Use: High (every harness run before device tests that need credentials). Scope: scripts/e2e-android.sh.
ensure_e2e_credentials() {
  if ! e2e_story_facts_ready; then
    log "Skipping harness credentials (no Story-trait Facts in CipherBank-app.E2ETests yet)"
    return 0
  fi
  local env_file="$ROOT/artifacts/e2e-local.env"
  local example="$ROOT/docs/tests/e2e-local.env.example"
  if [[ -f "$env_file" ]]; then
    log "Loading harness credentials from artifacts/e2e-local.env (export-if-unset)"
    apply_e2e_env_file_if_unset "$env_file"
  fi
  if [[ -z "${E2E_TEST_PIN:-}" || -z "${E2E_TEST_PIN_ALT:-}" || -z "${E2E_RECOVERY_PASSWORD:-}" ]]; then
    die "Missing E2E harness credentials. Copy $example to $env_file and fill lab values, or export E2E_TEST_PIN, E2E_TEST_PIN_ALT, and E2E_RECOVERY_PASSWORD."
  fi
}

# Applies KEY=VALUE lines from a harness env file without clobbering already-exported vars.
# Use: High (ensure_e2e_credentials). Scope: current shell process.
apply_e2e_env_file_if_unset() {
  local file="$1" line key value
  while IFS= read -r line || [[ -n "$line" ]]; do
    [[ -z "$line" || "$line" =~ ^[[:space:]]*# ]] && continue
    key="${line%%=*}"
    value="${line#*=}"
    key="${key%"${key##*[![:space:]]}"}"
    key="${key#"${key%%[![:space:]]*}"}"
    [[ -z "$key" ]] && continue
    if [[ ! -v "$key" ]]; then
      export "$key=$value"
    fi
  done < "$file"
}


# Runs the E2E suite against the installed APK, scoped by the resolved Story-trait filter.
# Use: High (every harness run). Scope: CipherBank-app.E2ETests process.
run_e2e_tests() {
  local apk="$1" filter="$2"
  local journal_dir="${E2E_JOURNAL_DIR:-artifacts/e2e-journal}"
  mkdir -p "$journal_dir"
  local appium_server_url="${APPIUM_SERVER_URL:-http://127.0.0.1:${APPIUM_PORT}}"
  local -a test_args=("$E2E_PROJECT" --nologo)
  [[ -n "$filter" ]] && test_args+=(--filter "$filter")
  log "Running: dotnet test ${test_args[*]} (Appium $appium_server_url)"
  APPIUM_SERVER_URL="$appium_server_url" \
  APPIUM_PORT="$APPIUM_PORT" \
  E2E_RUN=1 TEST_PLATFORM=android \
    ANDROID_APK_PATH="$apk" \
    E2E_TEST_PIN="${E2E_TEST_PIN:-}" \
    E2E_TEST_PIN_ALT="${E2E_TEST_PIN_ALT:-}" \
    E2E_RECOVERY_PASSWORD="${E2E_RECOVERY_PASSWORD:-}" \
    E2E_JOURNAL_DIR="$journal_dir" \
    dotnet test "${test_args[@]}"
}

# Orchestrates the full harness run: emulator -> build -> install -> Appium -> tests.
# Use: High (every invocation). Scope: whole script process.
main() {
  parse_args "$@"
  ensure_e2e_credentials
  local filter
  filter="$(resolve_test_filter)"
  # Restore so preflight --list-tests works even before the MAUI build.
  dotnet restore "$E2E_PROJECT" --nologo >/dev/null
  preflight_filter_or_die "$filter"
  ensure_emulator_running
  build_apk
  local apk
  apk="$(locate_apk)"
  install_apk "$apk"
  ensure_appium_running
  run_e2e_tests "$apk" "$filter"
}

main "$@"
