# CipherBank — Testing (Web, Android, EAS, iOS later)

This app targets **iOS, Android, and web**. NFC presentment is **Android-first**; iOS builds stay compile-safe for Mac later.

## One-line starts

From `design_handoff_cipherbank/starter`:

```bash
npm run demo          # typecheck + print API/fixture inventory
npm run demo:web      # typecheck + contract dump + Expo web
npm run demo:android  # same → Android emulator/device
npm run android:setup # full SDK/AVD/native install + Metro (see ANDROID_SETUP.md)
npm run android:apk   # compile debug APK into dist/
npm run contract      # fixture + endpoint inventory only
npm run api-ref       # regenerate CB_FullAPIRef.html
```

Or: `./scripts/demo.sh web` · Full Android guide: [`ANDROID_SETUP.md`](./ANDROID_SETUP.md).

**User-story E2E (Playwright):** plan and story catalog in [`PLAYWRIGHT_PLAN.md`](./PLAYWRIGHT_PLAN.md); CB-* procedures in [`USER_STORIES.md`](./USER_STORIES.md); ID bridge in [`STORY_ID_MAP.md`](./STORY_ID_MAP.md); env/selectors in [`E2E_CONFIGURABLES.md`](./E2E_CONFIGURABLES.md). Target Expo web + mock first; MAUI keeps Appium (`CipherBank-app.E2ETests`) with shared `US-*` IDs.

## Quick matrix

| Target | Command | NFC |
|--------|---------|-----|
| Web | `npm run web` | **Simulate exchange** (EMV-shaped stages) |
| Android emulator | `npm run demo:android` after AVD | Emulator RF limited — use **Simulate exchange** |
| Android device | EAS `development` APK or `npx expo run:android` | Real NFC via `react-native-nfc-manager`; HCE APDUs later |
| iOS | On **Mac**: `npm run eas:build:ios` | Stub only |

NFC **does not work in stock Expo Go**. Use a **dev client** (`expo-dev-client`) or EAS development build for RF. Emulator/web always use the staged lab exchange.

Digital card / Visa VTS / Mastercard MDES mapping: [`DIGITAL_CARDS_NFC.md`](./DIGITAL_CARDS_NFC.md).


## Web (this Linux box)

```bash
cd design_handoff_cipherbank/starter
npm install
npm run web
```

Open the Metro URL; use **Profile → Tap to pay lab** for the full mock POS flow.

## Android emulator (Linux)

SDK path used in this workspace: `$HOME/Android/Sdk` (cmdline-tools + API 34 Google APIs image). JDK: `$HOME/.local/jdk-17`.

```bash
export JAVA_HOME=$HOME/.local/jdk-17
export ANDROID_HOME=$HOME/Android/Sdk
export PATH=$JAVA_HOME/bin:$ANDROID_HOME/emulator:$ANDROID_HOME/platform-tools:$PATH

# AVD name from setup: CipherBank_API34
emulator -avd CipherBank_API34 &
adb wait-for-device

cd design_handoff_cipherbank/starter
npm run demo:android
# or: npx expo start --android
adb reverse tcp:8081 tcp:8081   # if Metro not reachable
```

In the app: **Profile → Tap to pay lab → Authorize → Simulate exchange**.

Prefer a **development client** for native NFC hooks (required — Expo Go will not load this project):

```bash
npx expo prebuild --platform android
npx expo run:android          # builds APK + installs on emulator/device
npx expo start --dev-client   # Metro for the installed app
```

Or: `npm run android:native` after the first prebuild.

## EAS Build

```bash
npx eas-cli login
# Set a real project in app.config.js extra.eas.projectId, or:
# npx eas-cli init

npm run eas:build:android   # development APK
npm run eas:build:ios       # run on Mac CI / Mac hardware
```

Profiles are in [`eas.json`](../eas.json): `development` (dev client), `preview`, `production`.

## Hardware NFC bench

1. Preload / select a card tagged `hardwareTest` in Profile (fixture: `card_tok_nfc_bench_4242`).
2. Optional env:

```bash
EXPO_PUBLIC_HARDWARE_CARD_ID=card_tok_nfc_bench_4242
EXPO_PUBLIC_POS_REQUIRE_TEST_CARD=true
```

3. On a physical Android with NFC: open Tap to pay lab → authorize → **Start NFC**.
4. Payload is `sessionId` + `tokenRef` only (never PAN). Full HCE payment APDUs are processor-specific and out of band.

## iOS port (Mac later)

- Bundle ID: `com.cipherbank.app`
- `NFCReaderUsageDescription` is already in `app.config.js`
- Host Card Emulation for arbitrary schemes is **not** available like Android; document product choice before investing in Apple Tap to Pay / Wallet
- Build: `eas build -p ios --profile development` on Mac with credentials

## Clean install vs lab seed

| Mode | Env | Expect |
|------|-----|--------|
| **Clean OOTB** | `EXPO_PUBLIC_SEED_DEMO=false`, `EXPO_PUBLIC_MOCK_HAS_WALLET=false` | Welcome (Create / Set up this device); empty portfolio; no Maya/Jordan until setup or bootstrap |
| **Lab seed** | `EXPO_PUBLIC_SEED_DEMO=true` (or legacy `MOCK_HAS_WALLET=true`) | Demo custody PIN `000000`, seeded ACH, rich `portfolio.demo.json` |

`EXPO_PUBLIC_USE_MOCK=true` is fine either way (API stubs without fabricating a filled account in clean mode).

Wipe SecureStore + SQLite on the emulator before verifying clean:

```bash
adb shell pm clear com.cipherbank.app
# restart Metro after .env changes, then relaunch the app
```

After clear + clean env: land on Welcome → Create → empty Home with Cora setup prompt (Pull / Add ACH / Skip). Returning path: Welcome → Set up this device → after PIN, bootstrap pulls fixture contacts.

## POS API contract

See [`src/mocks/POS_API.md`](../src/mocks/POS_API.md) for POS checklist.  
Full app API/JSON shapes: [`src/mocks/API_CONTRACT.md`](../src/mocks/API_CONTRACT.md).
