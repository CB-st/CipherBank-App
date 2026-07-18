# Phase E Plan — Staging cutover, E2E, PR to main

> After Phase D on `feat/cora-maui-port`. Lands the Cora→MAUI port for review/merge.

**Goal:** Swap mocks for live `/v1` against staging when configured, update Appium E2E for the new Shell, open/merge PR onto `main`.

## E1 — Live product API client
1. Implement `HttpProductApi : IProductApi` using existing `AddCipherBankHttpClient` pipeline (auth header, resilience, pinning).
2. SCREAMING_SNAKE JSON via `JsonSerializerOptions` / attributes already on DTOs.
3. Send `Idempotency-Key` on convert/transfer/pay/POS mutations.
4. DI factory (DEBUG): `UseMockServices` → `MockProductApi`, else `HttpProductApi` (same pattern as Auth/Wallet).
5. Stream: when not mocking, register `ClientWebSocketStreamService` with WSS from settings (`wss://…/v1/stream`).

**Files:** `Services/HttpProductApi.cs`, `MauiProgram.cs`, `ISettingsService` (+ optional `StreamEndpoint`).

## E2 — Session + auth alignment
1. Map custody unlock → `POST /session` body (device attestation stub field ok).
2. Store access/refresh via existing `SecureStorage` auth paths or dedicated product-session keys.
3. Refresh on 401 for product calls.

## E3 — E2E Appium update
1. Replace Login→Dashboard flows with Welcome/Unlock→Home.
2. Page objects: `UnlockPage`, `HomePage`, `ConvertPage`, `ReceivePage`, `ProfilePage`, `PosLabPage`.
3. Keep AutomationIds on primary buttons (add where missing).
4. Smoke path: onboard (or unlock with test PIN in DEBUG) → Home → Convert lock quote → Receive QR visible → Profile → PosLab Simulate.

**Files:** `CipherBank-app.E2ETests/PageObjects/*`, test specs.

## E4 — Branch hygiene & PR
1. Ensure `feat/cora-maui-port` includes CIP-19 main + Cora handoff tree + MAUI port commits.
2. Push branch; open PR to `main` (or retarget/undraft PR #2 if preferred).
3. PR body: Phase A–D summary, test plan (unit 170+, Android manual NFC/Simulate, light/dark prefs).
4. Respect ruleset: approve + admin bypass if needed (as with CIP-19).

## E5 — Verification checklist
- [x] `dotnet test CipherBank-app.Tests` green (173)
- [x] DEBUG mock path: DI factory → `MockProductApi` / `MockStreamService` when `UseMockServices`
- [x] DEBUG live path: `HttpProductApi` + `ClientWebSocketStreamService` + `StreamEndpoint` setting
- [ ] Android: NFC presentment or graceful timeout message (manual)
- [x] E2E smoke page objects + `CoraShellSmokeTests` (legacy journeys skipped)

## Non-goals
- Production HCE / VTS / MDES
- Dropping Expo handoff tree (keep as reference under `design_handoff_cipherbank/`)
- Force-push / rewriting CIP-19 history

## Order
E1 → E2 → E3 → E5 → E4 (PR last once smoke is green).
