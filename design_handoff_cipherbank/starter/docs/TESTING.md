# CipherBank — Testing (Web, Android, EAS, iOS later)

This app targets **iOS, Android, and web**. NFC presentment is **Android-first**; iOS builds stay compile-safe for Mac later.

## One-line starts

From `design_handoff_cipherbank/starter`:

```bash
npm run demo          # typecheck + print API/fixture inventory
npm run demo:web      # typecheck + contract dump + Expo web
npm run demo:android  # same → Android emulator/device
npm run contract      # fixture + endpoint inventory only
```

Or: `./scripts/demo.sh web`

## Quick matrix

| Target | Command | NFC |
|--------|---------|-----|
| Web | `npm run web` | Mock POS only (Simulate tap) |
| Android emulator | `npm run android:emu` after AVD + dev client | Emulator NFC is limited; use **Simulate tap** or a physical device |
| Android device | EAS `development` APK or `npm run prebuild && npx expo run:android` | Real NFC via `react-native-nfc-manager` |
| iOS | On **Mac**: `npm run eas:build:ios` or `npx expo run:ios` | Core NFC reader strings reserved; **no consumer HCE** — Apple Tap to Pay is a different merchant program |

NFC **does not work in stock Expo Go**. Use a **dev client** (`expo-dev-client`) or EAS development build.

## Web (this Linux box)

```bash
cd design_handoff_cipherbank/starter
npm install
npm run web
```

Open the Metro URL; use **Profile → Tap to pay lab** for the full mock POS flow.

## Android emulator (Linux)

1. Install [Android Studio](https://developer.android.com/studio) or cmdline tools; create an AVD (API 34+ recommended).
2. Start the emulator (`emulator -avd <name>` or from Android Studio).
3. Prefer a **development client** for NFC hooks:

```bash
npm run eas:build:android
# or local native:
npm run prebuild
npx expo run:android
```

4. Or for JS-only UI (no native NFC): `npm run android:emu` with Expo Go — POS lab still works via **Simulate tap**.

Ensure the emulator can reach Metro (`adb reverse tcp:8081 tcp:8081` if needed).

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

## POS API contract

See [`src/mocks/POS_API.md`](../src/mocks/POS_API.md) for POS checklist.  
Full app API/JSON shapes: [`src/mocks/API_CONTRACT.md`](../src/mocks/API_CONTRACT.md).
