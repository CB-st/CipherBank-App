# CipherBank App (Expo)

Runnable Expo + React Native + TypeScript consumer app for **iOS, Android, and web**. Design handoff lives one level up (`../designs`, `../README.md`).

## Quick start

```bash
cd design_handoff_cipherbank/starter
cp .env.example .env   # defaults to mock mode
npm install
npm run demo           # verify typecheck + API contract inventory
npm run demo:web       # one-line: launch web demo
```

| Script | Target |
|--------|--------|
| `npm run demo` | Typecheck + dump fixtures/endpoints |
| `npm run demo:web` | Web demo (mock API) |
| `npm run demo:android` | Android via Expo |
| `npm start` | Expo bundler |
| `npm run web` | Web only |
| `npm run ios` / `android` | Native targets |
| `npm run typecheck` | `tsc --noEmit` |
| `npm run contract` | Fixture inventory |

Defaults (`EXPO_PUBLIC_USE_MOCK=true`) serve fixture JSON from `src/mocks/`.  
**API shapes for backend:** [`src/mocks/API_CONTRACT.md`](src/mocks/API_CONTRACT.md) · POS: [`src/mocks/POS_API.md`](src/mocks/POS_API.md) · Devices: [`docs/TESTING.md`](docs/TESTING.md).

## Architecture highlights

- **Motion:** `PressableScale` / `FadeIn` via Reanimated; gold CTAs use spring press feedback.
- **Prefs:** local AsyncStorage + `GET/PUT /prefs` sync — Home layout, privacy, Cora, default send speed (Profile tab).
- **Hybrid vault:** local SecureStore custody (mnemonic never uploaded) + server `/vault/binaries` and `/vault/cards` (processor tokens only).
- **POS lab:** Profile → Tap to pay lab — mock POS + Android NFC presentment (`src/features/pos/`). Contract: `src/mocks/POS_API.md`.
- **Platforms:** `SafeAreaProvider`, biometric unlock on native, web secure-storage fallback. See [`docs/TESTING.md`](docs/TESTING.md).

## Docs

- [`AGENTS.md`](AGENTS.md) — build rules and layering
- [`API.md`](API.md) — live `/v1` contract
- [`../ARCHITECTURE.md`](../ARCHITECTURE.md) — UI↔backend async patterns
