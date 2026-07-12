#!/usr/bin/env bash
# One-line CipherBank test environment launcher
# Usage: npm run demo | npm run demo:web | ./scripts/demo.sh [web|android|check]

set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"
export PATH="${HOME}/.local/node/bin:${PATH}"

MODE="${1:-web}"

echo "==> CipherBank demo (${MODE})"
echo "    root: ${ROOT}"

if [[ ! -d node_modules ]]; then
  echo "==> npm install"
  npm install
fi

case "${MODE}" in
  check)
    echo "==> typecheck"
    npm run typecheck
    echo "==> fixture inventory"
    node scripts/dump-contract.mjs
    echo "==> OK — mock-first app ready (EXPO_PUBLIC_USE_MOCK=${EXPO_PUBLIC_USE_MOCK:-from .env})"
    ;;
  web)
    npm run typecheck
    node scripts/dump-contract.mjs
    echo "==> starting Expo web (Ctrl+C to stop)"
    echo "    Open Profile → Tap to pay lab for POS mock"
    exec npx expo start --web
    ;;
  android)
    npm run typecheck
    node scripts/dump-contract.mjs
    echo "==> starting Expo (Android). Ensure emulator/device is connected."
    echo "    NFC needs a dev client / EAS build — see docs/TESTING.md"
    exec npx expo start --android
    ;;
  ios)
    npm run typecheck
    node scripts/dump-contract.mjs
    echo "==> iOS start (requires Mac / Xcode or EAS). See docs/TESTING.md"
    exec npx expo start --ios
    ;;
  *)
    echo "Usage: $0 [web|android|ios|check]"
    exit 1
    ;;
esac
