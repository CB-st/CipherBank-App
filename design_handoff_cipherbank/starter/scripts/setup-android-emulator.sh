#!/usr/bin/env bash
# CipherBank — one-shot Android emulator + native dev-client setup
# Usage: ./scripts/setup-android-emulator.sh
#        npm run android:setup
#
# Idempotent: installs portable JDK/SDK pieces if missing, boots AVD,
# builds the native app, and starts Metro for the emulator.

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

AVD_NAME="${CB_AVD_NAME:-CipherBank_API34}"
SDK="${ANDROID_HOME:-${ANDROID_SDK_ROOT:-$HOME/Android/Sdk}}"
JDK_DIR="${JAVA_HOME:-$HOME/.local/jdk-17}"
export PATH="${HOME}/.local/node/bin:${PATH}"

log() { echo "==> $*"; }
die() { echo "ERROR: $*" >&2; exit 1; }

# --- Node ---
command -v node >/dev/null || die "Node.js required (install Node 20+ or put it on PATH)"
command -v npm >/dev/null || die "npm required"

# --- JDK (portable Temurin if needed) ---
ensure_jdk() {
  if [[ -x "${JDK_DIR}/bin/java" ]]; then
    export JAVA_HOME="$JDK_DIR"
  elif command -v java >/dev/null; then
    export JAVA_HOME="$(dirname "$(dirname "$(readlink -f "$(command -v java)")")")"
  else
    log "Installing portable JDK 17 → $HOME/.local/jdk-17"
    mkdir -p "$HOME/.local/jdk-17"
    curl -fsSL -o /tmp/cb-jdk17.tar.gz \
      "https://api.adoptium.net/v3/binary/latest/17/ga/linux/x64/jdk/hotspot/normal/eclipse?project=jdk"
    tar -xzf /tmp/cb-jdk17.tar.gz -C "$HOME/.local/jdk-17" --strip-components=1
    rm -f /tmp/cb-jdk17.tar.gz
    export JAVA_HOME="$HOME/.local/jdk-17"
  fi
  export PATH="$JAVA_HOME/bin:$PATH"
  java -version 2>&1 | head -1
}

# --- Android SDK ---
ensure_sdk() {
  export ANDROID_HOME="$SDK"
  export ANDROID_SDK_ROOT="$SDK"
  export PATH="$SDK/cmdline-tools/latest/bin:$SDK/platform-tools:$SDK/emulator:$PATH"

  if [[ ! -x "$SDK/cmdline-tools/latest/bin/sdkmanager" ]]; then
    log "Installing Android cmdline-tools → $SDK"
    mkdir -p "$SDK/cmdline-tools"
    curl -fsSL -o /tmp/cb-cmdtools.zip \
      "https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip"
    rm -rf /tmp/cb-cmdline-tools
    mkdir -p /tmp/cb-cmdline-tools
    unzip -qo /tmp/cb-cmdtools.zip -d /tmp/cb-cmdline-tools
    mkdir -p "$SDK/cmdline-tools/latest"
    cp -a /tmp/cb-cmdline-tools/cmdline-tools/. "$SDK/cmdline-tools/latest/"
    rm -f /tmp/cb-cmdtools.zip
  fi

  yes | sdkmanager --licenses >/tmp/cb-sdk-licenses.log 2>&1 || true
  log "Ensuring platform-tools, emulator, API 34 image"
  sdkmanager --install \
    "platform-tools" \
    "emulator" \
    "platforms;android-34" \
    "system-images;android-34;google_apis;x86_64" >/tmp/cb-sdk-install.log
}

ensure_avd() {
  if ! avdmanager list avd 2>/dev/null | grep -q "$AVD_NAME"; then
    log "Creating AVD $AVD_NAME"
    echo no | avdmanager create avd -n "$AVD_NAME" \
      -k "system-images;android-34;google_apis;x86_64" \
      -d pixel_6 --force
  else
    log "AVD $AVD_NAME already exists"
  fi
}

ensure_emulator() {
  if adb devices 2>/dev/null | grep -qE 'emulator-[0-9]+[[:space:]]+device'; then
    log "Emulator already running"
  else
    log "Starting emulator $AVD_NAME (log: /tmp/cb-emulator.log)"
    nohup emulator -avd "$AVD_NAME" -netdelay none -netspeed full -gpu auto \
      >/tmp/cb-emulator.log 2>&1 &
    echo $! >/tmp/cb-emulator.pid
  fi
  log "Waiting for device boot…"
  adb wait-for-device
  for _ in $(seq 1 90); do
    boot="$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r' || true)"
    [[ "$boot" == "1" ]] && break
    sleep 2
  done
  [[ "$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" == "1" ]] \
    || die "Emulator did not finish booting — check /tmp/cb-emulator.log"
  adb reverse tcp:8081 tcp:8081 || true
  log "Emulator ready"
}

# --- App ---
log "CipherBank Android emulator setup"
log "root: $ROOT"
ensure_jdk
ensure_sdk
ensure_avd
ensure_emulator

if [[ ! -f .env && -f .env.example ]]; then
  cp .env.example .env
  log "Created .env from .env.example"
fi

if [[ ! -d node_modules ]]; then
  log "npm install"
  npm install
fi

log "Typecheck"
npm run typecheck

log "Native prebuild (android/)"
npx expo prebuild --platform android --no-install
echo "sdk.dir=$ANDROID_HOME" > android/local.properties

log "Build + install debug APK on emulator"
npx expo run:android --no-bundler

log "Starting Metro (dev client) on :8081"
# Free stale Metro if any
fuser -k 8081/tcp 2>/dev/null || true
sleep 1
nohup npx expo start --dev-client --port 8081 >/tmp/cb-metro.log 2>&1 &
echo $! >/tmp/cb-metro.pid
sleep 4
adb reverse tcp:8081 tcp:8081 || true

log "Opening CipherBank on emulator"
adb shell am start -a android.intent.action.VIEW \
  -d "exp+cipherbank://expo-development-client/?url=http%3A%2F%2F127.0.0.1%3A8081" \
  com.cipherbank.app >/dev/null || \
  adb shell am start -n com.cipherbank.app/.MainActivity >/dev/null || true

cat <<EOF

==> Setup complete
    Emulator:  $AVD_NAME
    Package:   com.cipherbank.app
    Metro:     http://localhost:8081  (log: /tmp/cb-metro.log)
    POS lab:   Profile → Tap to pay lab → Simulate exchange

    Re-open later:
      adb reverse tcp:8081 tcp:8081
      npx expo start --dev-client
      # then launch CipherBank on the emulator

EOF
