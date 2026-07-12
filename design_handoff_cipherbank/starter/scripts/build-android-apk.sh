#!/usr/bin/env bash
# CipherBank — compile a shareable Android APK (debug or release)
# Usage: ./scripts/build-android-apk.sh [debug|release]
#        npm run android:apk
#        npm run android:apk:release
#
# Output: dist/cipherbank-<variant>-<version>.apk

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

VARIANT="${1:-debug}"
case "$VARIANT" in
  debug|release) ;;
  *) echo "Usage: $0 [debug|release]" >&2; exit 1 ;;
esac

SDK="${ANDROID_HOME:-${ANDROID_SDK_ROOT:-$HOME/Android/Sdk}}"
JDK_DIR="${JAVA_HOME:-$HOME/.local/jdk-17}"
export PATH="${HOME}/.local/node/bin:${PATH}"

if [[ -x "${JDK_DIR}/bin/java" ]]; then
  export JAVA_HOME="$JDK_DIR"
elif command -v java >/dev/null; then
  export JAVA_HOME="$(dirname "$(dirname "$(readlink -f "$(command -v java)")")")"
else
  echo "ERROR: Java not found. Run ./scripts/setup-android-emulator.sh first." >&2
  exit 1
fi

export ANDROID_HOME="$SDK"
export ANDROID_SDK_ROOT="$SDK"
export PATH="$JAVA_HOME/bin:$SDK/platform-tools:$PATH"

[[ -d "$SDK" ]] || { echo "ERROR: Android SDK missing at $SDK — run setup-android-emulator.sh first." >&2; exit 1; }

echo "==> CipherBank APK build ($VARIANT)"
echo "    root: $ROOT"

if [[ ! -d node_modules ]]; then
  echo "==> npm install"
  npm install
fi

if [[ ! -f .env && -f .env.example ]]; then
  cp .env.example .env
fi

echo "==> typecheck"
npm run typecheck

echo "==> expo prebuild"
npx expo prebuild --platform android --no-install
echo "sdk.dir=$ANDROID_HOME" > android/local.properties

GRADLE_TASK="assembleDebug"
OUT_REL="android/app/build/outputs/apk/debug/app-debug.apk"
if [[ "$VARIANT" == "release" ]]; then
  GRADLE_TASK="assembleRelease"
  OUT_REL="android/app/build/outputs/apk/release/app-release.apk"
  # Unsigned release is fine for internal sideload testing; production should use EAS/signing.
  if [[ ! -f android/app/cipherbank-release.keystore ]]; then
    echo "==> note: release APK may be unsigned/debug-signed unless you configure signing"
  fi
fi

echo "==> ./gradlew :app:$GRADLE_TASK"
(
  cd android
  chmod +x gradlew
  ./gradlew ":app:$GRADLE_TASK" --quiet
)

[[ -f "$OUT_REL" ]] || { echo "ERROR: APK not found at $OUT_REL" >&2; exit 1; }

VERSION="$(node -p "require('./package.json').version")"
mkdir -p dist
DEST="dist/cipherbank-${VARIANT}-${VERSION}.apk"
cp -f "$OUT_REL" "$DEST"
SIZE="$(du -h "$DEST" | cut -f1)"

echo ""
echo "==> APK ready: $DEST ($SIZE)"
echo "    Install on a device/emulator:"
echo "      adb install -r $DEST"
echo "    Or copy the file from dist/."
echo ""
