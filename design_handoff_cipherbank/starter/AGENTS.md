# AGENTS.md — Cipherbank App Buildout Guide

Guidance for an AI agent (or developer) building the Cipherbank consumer app from this handoff. Read this first, then `ARCHITECTURE.md`, then `API.md`.

## What this repo is
A **starter scaffold** for the Cipherbank mobile app: Expo + React Native + TypeScript, React Query for async/server state, React Navigation for the shell. The `starter/` folder is real, runnable-shaped code — providers, navigation, theme, primitives, feature hooks, and screen stubs are wired; the visual detail of each screen is intentionally left to be filled in from the designs.

- **Design source of truth:** `../designs/*.dc.html` (open in a browser) + `../README.md` (per-screen specs) + `../tokens/` + `../assets/`.
- **Interaction contract:** `../ARCHITECTURE.md` (how UI talks to the backend).
- **Server contract:** `API.md` (endpoints/services to build).

## Golden rules
1. **Shell renders synchronously; data streams in; actions are optimistic.** Never block a tap on the network. (Full rationale in `ARCHITECTURE.md §1`.)
2. **Tokens only — no magic values.** Colors, spacing, radii, type come from `src/theme` (generated from `../tokens/tokens.json`). If you need a value that isn't a token, add it to the token source, don't inline a hex.
3. **Layered boundaries:**
   - `components/**` are pure/stateless — props in, JSX out, styled from theme. **They never fetch.**
   - `features/*/use*.ts` own **all** async (queries, mutations, streams, optimistic logic).
   - `lib/apiClient.ts` + `lib/queryClient.ts` are the only places that know HTTP/cache mechanics.
4. **Self-custody:** keys are generated **on-device** (`features/session`) and never sent to the server. The API is authenticated but custody stays local.
5. **Idempotency:** every money-moving mutation sends an `Idempotency-Key` so retries are safe.

## Where to start interfacing with the API
Everything funnels through **`src/lib/apiClient.ts`** (HTTP) and **`src/lib/socket.ts`** (real-time).

**Mock-first (default):** set `EXPO_PUBLIC_USE_MOCK=true` in `.env`. Requests hit `src/mocks/` fixtures/handlers — see `src/mocks/README.md` for the fixture→endpoint map. This is the contract lab: shape the JSON here, then make the real API compliant.

To go from mocked to live:

1. **Set env:** `EXPO_PUBLIC_USE_MOCK=false`, point `EXPO_PUBLIC_API_BASE` / `EXPO_PUBLIC_WSS` at your server.
2. **Wire auth:** fill the `Authorization` header in `apiClient.ts` from `features/session` (token after wallet unlock).
3. **First real call — Portfolio (proves the loading pattern):**
   - Implement `GET /portfolio` on the server per `API.md` (match `src/mocks/fixtures/portfolio.json`).
   - `features/portfolio/portfolio.api.ts` already calls it; `usePortfolio.ts` caches it; `HomeScreen` renders skeleton → data.
4. **Second — Quotes + Convert (proves rate-lock + optimistic settle):**
   - `POST /quotes` drives `useQuoteLock`.
   - `POST /convert` drives `useConvert`. Emit `convert.settled` over the socket to reconcile.
5. **Then** Send (`POST /transfers`), Pay-mix (`POST /payments`), Receive (`GET /receive/:asset`), Onboarding/session.

Each feature is independent — build vertically (one endpoint → its hook → its screen) rather than all endpoints at once.

## Build order (matches README §Suggested Build Order)
1. Foundations — fonts, `theme`, `assets`, primitives (`Button`, `Card`, `Skeleton`, `Toast`).
2. **Home** — `usePortfolio` + `BalanceHero` + AssetList (build `AssetList` + `AssetGlyph` + its `.Skeleton`).
3. **Convert** — `useQuoteLock` + `useConvert` + amount cards + `RateLockStrip`.
4. **Send** + **Receive**.
5. **Pay-with-a-mix** — `usePayMix` + `FundingMixBar` (coverage must reach 100% before confirm).
6. **Onboarding** — `features/session` (on-device keygen, biometrics, recovery phrase).
7. **Securities** — extend `AssetType` and the asset model; unlock "pay/invest with anything."

## Filling in a screen (repeatable recipe)
1. Open the matching `designs/Cipherbank App.dc.html` screen + its `README.md` §Screens entry for exact layout, colors, copy, and Cora line.
2. Compose existing `components/**`; build any missing presentational component in the right subfolder (`money/`, `chrome/`, …) — pure, token-styled, with a `.Skeleton` if it shows async data.
3. Get data/actions from the feature hook (`use*`), never fetch in the screen.
4. Cover every state from `ARCHITECTURE.md §7`: loading · empty · error · offline · success (+ screen-specific: rate-expired, insufficient, under-funded).

## Conventions
- **Money math:** amounts are strings end-to-end (bignumber-safe); format only at the edge via `lib/money.ts`. Never do float math on balances.
- **Cora:** one line per screen, dry and short. Her artwork is a PNG slot on `CoraBar` — the client supplies transparent cutouts (not in repo).
- **Icons/logos:** SVGs in `../assets/` (`react-native-svg`). Swap the placeholder squares in `TabBar` for `ui-*.svg`.
- **Do not** port anything from `designs/*.dc.html` runtime (`support.js`, `ios-frame.jsx`, etc.) — those render the *references* only.
- **Typecheck** before done: `npm run typecheck`.

## Definition of done for a feature
- [ ] Endpoint implemented per `API.md` (request/response shapes match).
- [ ] Feature hook: query or optimistic mutation with rollback + idempotency key.
- [ ] Screen composes pure components, styled from tokens, pixel-close to the design.
- [ ] All async states handled (skeleton/empty/error/offline/success).
- [ ] Real-time reconcile wired where relevant (`socket.ts`).
- [ ] `npm run typecheck` clean.
