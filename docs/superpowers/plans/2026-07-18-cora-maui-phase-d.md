# Phase D Plan — Profile, Vault UI, PosLab, Android NDEF

> Follow-on to Phase C polish on `feat/cora-maui-port`. Do not expand into production HCE/VTS/MDES.

**Goal:** Ship Profile prefs/vault surfaces and the POS lab with Android NDEF presentment parity to Cora, then leave HCE as Phase-2.

## Already scaffolded (reuse)
- `ProfileViewModel` / `ProfilePage` — prefs, mnemonic reveal, vault cards list, advanced API endpoint, PosLab nav, lock
- `PosLabViewModel` / `PosLabPage` — session authorize/confirm, Simulate EMV stages, Present NFC button
- `NullNfcPresentmentService` + `AndroidNdefPresentmentService` stub
- AndroidManifest NFC permission + optional feature
- `MockProductApi` POS + vault card endpoints
- `EmvExchangeSimulator` stage timeline

## D1 — Profile polish (1–2 days)
1. Wire `GlassCard` sections: Appearance, Home layout toggles (from `UserPrefs.HomeOrder` / `HomeVisible`), Cora toggle, Base currency, Lock idle seconds.
2. Persist idle seconds into `IAppSession.IdleMs` on save.
3. Vault section: list `GET /vault/cards` + binaries placeholder; mark active hardware-test card for POS.
4. Auth-gate mnemonic reveal via PIN re-entry (`IPinService.VerifyPinAsync`) before `ExportMnemonic`.
5. Keep Advanced (API endpoint / mocks) collapsed by default.

**Files:** `ProfileViewModel.cs`, `ProfilePage.xaml`, `PrefsStore` / `UserPrefs`, optional `VaultBinaries` mock DTO.

## D2 — PosLab completion (1–2 days)
1. Glass polish PosLab UI; show last4/brand/TTL from authorize response.
2. Wire `SimulateAsync` timeline into a vertical stage list with status chips.
3. Gate Start Session on `IAppSession.IsUnlocked`.
4. Persist selected vault card id in Preferences (`pos_active_card`).

**Files:** `PosLabViewModel.cs`, `PosLabPage.xaml`, `IProductApi` POS DTOs (already present).

## D3 — Android NDEF presentment (2–3 days)
1. Replace stub success in `AndroidNdefPresentmentService` with real Reader Mode:
   - `NfcAdapter.EnableReaderMode` on `MainActivity`
   - On tag discovered → `Ndef.Get(tag)` → `WriteNdefMessage` with JSON `{v,sessionId,tokenRef}`
2. Timeout + cancel UX; surface errors to PosLab.
3. iOS/Mac/Windows remain Simulate-only with clear copy.
4. Unit-test payload JSON shape in Core (no Android dependency).

**Files:** `Platforms/Android/Nfc/AndroidNdefPresentmentService.cs`, `MainActivity.cs`, optional `INfcPresentmentService` timeout API.

## D4 — Verification
- Unit: POS payload serialization; prefs idle round-trip
- Manual Android emulator with NFC sim / real device: Start session → Present NFC / Simulate
- Profile appearance light/dark applies `UserAppTheme`

## Explicit non-goals (still Phase-2)
- `HostApduService` / EMV APDU cryptograms
- Visa VTS / Mastercard MDES SDKs
- Production CDCVM attestation beyond biometric unlock flag

## Suggested order
D1 → D2 → D3 → D4, then Phase E (staging cutover + E2E + PR).
