# Android setup — emulator demo & APK builds

Two scripts get CipherBank from clone → running on an emulator, or to a sideloadable APK.

| Goal | Command |
|------|---------|
| Emulator + native app + Metro | `./scripts/setup-android-emulator.sh` or `npm run android:setup` |
| Compile APK (debug) | `./scripts/build-android-apk.sh` or `npm run android:apk` |
| Compile APK (release) | `./scripts/build-android-apk.sh release` or `npm run android:apk:release` |

Also see [`TESTING.md`](./TESTING.md) and [`DIGITAL_CARDS_NFC.md`](./DIGITAL_CARDS_NFC.md).

## Prerequisites

- Linux (tested on Pop!_OS / Ubuntu-class) with KVM (`/dev/kvm`) for a fast emulator
- Node.js 20+ and npm (`PATH` may include `$HOME/.local/node/bin`)
- Network once for JDK / Android SDK downloads
- Disk: ~8–12 GB for SDK + system image + Gradle caches

The setup script will install a **portable JDK 17** under `$HOME/.local/jdk-17` and the **Android SDK** under `$HOME/Android/Sdk` if they are missing (no `sudo` required).

## 1 · Emulator testing (recommended)

From `design_handoff_cipherbank/starter`:

```bash
chmod +x scripts/*.sh
npm run android:setup
```

What it does:

1. Ensures JDK 17 + Android SDK (platform-tools, emulator, API 34 Google APIs image)
2. Creates AVD `CipherBank_API34` (override with `CB_AVD_NAME=…`)
3. Boots the emulator and waits for `sys.boot_completed`
4. `npm install`, copies `.env.example` → `.env` if needed
5. `expo prebuild` → `expo run:android` (installs `com.cipherbank.app`)
6. Starts Metro (`--dev-client` on `:8081`) and opens the app

**In the app:** Profile → Tap to pay lab → Authorize → **Simulate exchange**  
(Emulator NFC RF is limited; use Simulate exchange. Physical devices can use Start NFC.)

### Re-open later (already set up)

```bash
export JAVA_HOME=$HOME/.local/jdk-17
export ANDROID_HOME=$HOME/Android/Sdk
export PATH=$JAVA_HOME/bin:$ANDROID_HOME/emulator:$ANDROID_HOME/platform-tools:$HOME/.local/node/bin:$PATH

emulator -avd CipherBank_API34 &
adb wait-for-device && adb reverse tcp:8081 tcp:8081

cd design_handoff_cipherbank/starter
npx expo start --dev-client
```

## 2 · Build an APK

```bash
npm run android:apk            # debug → dist/cipherbank-debug-0.1.0.apk
npm run android:apk:release    # release → dist/cipherbank-release-0.1.0.apk
```

Install:

```bash
adb install -r dist/cipherbank-debug-0.1.0.apk
```

Notes:

- Scripts run `expo prebuild` so `android/` is generated locally (gitignored).
- Release APKs for store distribution should use EAS / a real keystore (`npm run eas:build:android`). The local release assemble is for internal sideload testing.
- First Gradle build can take several minutes.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| Emulator never boots | Check `/tmp/cb-emulator.log`; confirm KVM; try `-gpu swiftshader_indirect` |
| App blank / won’t load | `adb reverse tcp:8081 tcp:8081`; Metro log `/tmp/cb-metro.log` |
| Expo Go errors | Use the **dev client** from these scripts — not Expo Go (NFC + native modules) |
| `sdk.dir` / Gradle fails | Ensure `android/local.properties` has `sdk.dir=$HOME/Android/Sdk` (scripts write this) |

## Env defaults (mock POS)

See `.env.example`. Lab defaults include hardware-test card `card_tok_nfc_bench_4242` and `EXPO_PUBLIC_USE_MOCK=true`.
