# Persist systems + mnemonic file backup — design

**Date:** 2026-07-20  
**Branch:** `feat/cora-redesign-maui` (PR #16)  
**Status:** Draft for review  
**Supersedes for scope:** scorecard “persistence-first 100%” (option 3) + Design B  
**Related:** `docs/superpowers/plans/2026-07-20-cora-maui-overhaul-to-main.md`, Expo `docs/PERSISTENCE.md`, Expo `docs/CUSTODY.md`

## Goal

1. Close **in-scope** scorecard gaps that are about **local/persistent data presentation and management**.
2. Add a **ciphered mnemonic recovery file** protected by a **recovery password** (separate from daily PIN), so device loss / forgotten PIN is not catastrophic if the user kept the file.
3. Re-score the feature canvas after shipping; treat header bell, POS merchant amount UI, and other long-term items as later work (see Long-term goals).

## Long-term goals (not in this delivery)

These remain product direction; they are **out of the persistence-first waves** below but are not abandoned.

| Item | Horizon |
|------|---------|
| Cloud / CipherBank-hosted seed backup | Only if threat model and product explicitly allow a server-mediated path; default remains **never** send mnemonic off-device |
| Replacing daily PIN with recovery password | Keep PIN as day-to-day gate; recovery password stays backup-only unless UX research says otherwise |
| Activity tab, securities teaser, HCE/VTS/MDES, demo PIN `000000` | Post–persistence-first / main cutover polish and partnerships |
| Full Expo P3 (idle + charging) JobQueue | After P1+P2 prove value on device |
| Header bell / POS merchant amount UI | Chrome polish follow-up |

## Scorecard “100%” definition

- Denominator = scored features **minus** long-term rows (Activity, securities teaser, demo seed flags; HCE already “parity” as deferred-both).
- Flip to parity when shipped: rates cache, Home currency visibility filter, vault card add/remove, wallet/recipient delete management, JobQueue P1+P2, mnemonic file backup (new row).
- After Waves 1–5 + re-score, report % against that denominator.

## Threat model (custody backup)

| Asset | Rule |
|-------|------|
| Mnemonic / entropy | Never plaintext on disk, never on HTTP, never in SQLite |
| Daily PIN | Hash only in SecureStorage; not used as backup file key |
| Recovery password | User-chosen; **never** stored on device; used only to derive backup file key |
| Device secret | Stays in SecureStorage; **not** written into the backup file |
| Backup file | Self-contained ciphertext; safe to copy to USB / cloud drive **as ciphertext only** |
| Restore | File + recovery password → mnemonic in RAM → user sets new PIN → re-seal under new device secret |

**Catastrophic loss scenarios we mitigate**

| Scenario | Without backup | With file backup |
|----------|----------------|------------------|
| App uninstall / SecureStorage wipe | Seed gone | Restore from file + recovery password |
| Forgotten daily PIN (device still has blob) | Locked out of spend | Optional: recovery password path to re-PIN (same as restore-into-new-seal) |
| Lost phone + no backup file | Seed gone | Still gone — user must keep the file offline |

**We do not mitigate:** user loses both device and backup file; user forgets recovery password; user screenshots plaintext mnemonic.

---

## Part A — Mnemonic ciphered file backup

### A.1 Concepts

| Term | Meaning |
|------|---------|
| **Daily PIN** | Existing 6+ digit gate; PBKDF2 hash in SecureStorage |
| **Recovery password** | Separate passphrase (≥12 chars recommended); never persisted |
| **Backup file** | `cipherbank-recovery-v1.json` (or `.cbr`) containing only ciphertext + KDF params |
| **Export** | Unlocked custody → step-up → choose recovery password → write file via share/save |
| **Restore / recover** | No sealed wallet (or “Replace wallet”) → pick file → recovery password → validate BIP39 → Set PIN → `SealAsync` |

### A.2 File format (`cipherbank-recovery-v1`)

```json
{
  "FORMAT": "cipherbank-recovery-v1",
  "KDF": "PBKDF2-SHA256",
  "ITERATIONS": 600000,
  "SALT_B64": "…",
  "NONCE_B64": "…",
  "TAG_B64": "…",
  "CIPHERTEXT_B64": "…",
  "CREATED_AT": 1720900000000,
  "HINT": "optional user hint — never the password"
}
```

- Payload plaintext before seal: UTF-8 normalized BIP39 mnemonic only (no PIN, no device secret).
- AEAD: AES-256-GCM (same family as `CryptoBox`).
- KDF: PBKDF2-SHA256, **600k** iterations (harder than daily PIN hash — offline file attack surface).
- `HINT` is optional non-secret string (e.g. “USB blue stick”); never echo password.

### A.3 Core API

```csharp
public interface IMnemonicBackupService
{
    Task<byte[]> CreateBackupFileAsync(string mnemonic, string recoveryPassword, string? hint = null);
    Task<string> OpenBackupFileAsync(byte[] fileBytes, string recoveryPassword);
}
```

- `CreateBackupFileAsync`: validate mnemonic → derive key from recovery password → AES-GCM → UTF-8 JSON bytes.
- `OpenBackupFileAsync`: parse JSON → derive key → open → `MnemonicHelper.Validate` → return phrase; zero buffers after use where practical.
- Unit tests: round-trip; wrong password fails; tampered ciphertext fails; JSON never contains plaintext words.

### A.4 UI flows

**Export (Profile → “Backup recovery file”)**

1. Require unlocked + `StepUpAuth(AuthReason.RevealKeys)` (or new `AuthReason.BackupExport`).
2. Prompt recovery password + confirm (≥12 chars, not equal to daily PIN if we can check length-only / not store PIN).
3. Optional hint.
4. Build file bytes → platform save/share (`FileSaver` / Android create document / Share).
5. Confirm dialog: “Store this file offline. CipherBank never receives it.”
6. Clear password fields from VM immediately.

**Restore (Welcome / Unlock when no sealed wallet, or Profile “Restore from backup”)**

1. Pick file → enter recovery password → open → hold mnemonic in VM only for navigation.
2. Navigate existing `SetPin` (or dedicated RestorePin) with mnemonic query / secure in-memory handoff.
3. `FinishCustodySetupAsync` as today (seal + seed wallets).
4. Never write recovery password to SecureStorage.

**Forgotten PIN with sealed wallet still present**

- Offer “Recover with backup file” → opens file → on success: `SealAsync` with **new** PIN (overwrites blob + device secret) after confirm “This replaces the on-device wallet seal.”
- Does **not** require knowing old PIN.

### A.5 Constraints

- No HTTP upload of backup files or recovery passwords.
- No automatic cloud sync of the file.
- Logging: never log mnemonic, recovery password, or raw ciphertext beyond length.
- Backup file must remain usable across reinstalls (no binding to Android Keystore / device secret).

---

## Part B — Persistence systems (Design B waves)

### Wave 1 — Rescore + prefs presentation

1. Update `cora-maui-feature-compare.canvas.tsx` + F6.3 scorecard: splash, public `/iquote`, currency toggles, CoraBar → **parity**.
2. **Home:** filter holdings by `EnabledCurrencies`; expandable **Other assets (N)** for hidden (Expo `useVisibleHoldings`).
3. Emulator smoke after Wave 1 (baseline).

### Wave 2 — Local market persist

1. Migrate `LocalDb`:
   - `rates_snapshot (symbol PK, usd, change24h, updated_at)`
   - Improve OHLC: keep `(symbol, t, v)` or add `granularity` if needed for Expo parity
   - `sync_meta (key, value, updated_at)`
2. `IRatesCache` / `IMarketRepository` in Core.
3. P2 cold hydrate: held ∩ enabled symbols from SQLite → then refresh via `IPublicQuoteService`.
4. P1 charts: after `GetHistoryAsync`, write-through OHLC; prefer cache when fresh.

### Wave 3 — Sync queue (simplified)

1. `ISyncJobQueue`: priorities P1 (chart) / P2 (cold bootstrap), concurrency 2, one in-flight per symbol key.
2. Boot → enqueue P2; Home range change → enqueue P1 OHLC persist.
3. **Defer P3** (idle + charging); document under Long-term goals — does not block persistence-first 100%.

### Wave 4 — Management UIs + mnemonic backup

1. Vault: `AddVaultCardAsync` / `DeleteVaultCardAsync` on `IProductApi` + Mock/Http; Profile add/remove (token metadata only).
2. Wallets: wire `WalletRepository.DeleteAsync` with confirm from Home/AddWallet.
3. Recipients: `DeleteAsync` + Send UI remove.
4. Optional: post-create QR on AddWallet success.
5. **Part A** export/restore UI + `IMnemonicBackupService`.

### Wave 5 — Emulator verify + re-score

1. Manual smoke: splash → onboard **or** restore-from-file → Home (visibility) → Convert (cache) → Send (delete recipient) → Profile (vault CRUD, backup export) → reinstall/restore path if practical.
2. Update canvas/scorecard to persistence-first 100%.
3. Note remaining chrome gaps for a later wave.

---

## Architecture sketch

```
┌───────────── UI ─────────────┐
│ Home (visible/other assets)  │
│ Profile (vault CRUD, backup) │
│ Welcome/Unlock (restore)     │
│ Send (recipient delete)      │
└──────────┬───────────────────┘
           │
┌──────────▼───────────────────┐
│ AppSession / Custody         │
│ IMnemonicBackupService       │──► recovery file (AES-GCM + PBKDF2)
│ IStepUpAuth                  │
└──────────┬───────────────────┘
           │
┌──────────▼───────────────────┐
│ ISyncJobQueue (P1/P2)        │
│ IRatesCache / IMarketRepo    │
│ Prefs / Wallets / Recipients │
│ IProductApi vault writes     │
└──────────┬───────────────────┘
           │
┌──────────▼───────────────────┐
│ SQLite: rates_snapshot, ohlc,│
│ sync_meta, wallets, prefs,   │
│ recipients                   │
│ SecureStorage: sealed blob + │
│ device secret + PIN hash     │
└──────────────────────────────┘
```

## File map (expected)

| Area | Create / extend |
|------|-----------------|
| Backup | `Core/Custody/MnemonicBackupService.cs`, Profile/Welcome VMs, platform file picker/share |
| Schema | `Persist/LocalDb.cs` migrate |
| Market | `Persist/RatesCache.cs`, `MarketRepository.cs` |
| Queue | `Persist/SyncJobQueue.cs` or `Services/SyncJobQueue.cs` |
| Vault | `IProductApi` + Mock/Http + `ProfileViewModel` |
| Home | `HomeViewModel` visible/hidden split |
| Recipients | `RecipientRepository.DeleteAsync` + Send UI |
| Docs | This spec; update scorecard + overhaul plan; function ref HTML later |

## Verification

- Unit: backup round-trip / wrong password / schema migrate / rates upsert / job queue ordering.
- `dotnet test CipherBank-app.Tests` green.
- Emulator: export backup → clear app data → restore → Set PIN → Home.
- Confirm no mnemonic/recovery password in logcat or HTTP bodies.

## Open decisions (defaults locked unless you change them)

| Decision | Default |
|----------|---------|
| Recovery password min length | 12 characters |
| Recovery password ≠ daily PIN | Soft warn if equal (cannot prove equality to stored hash without verify) |
| File extension | `.cbr.json` / `cipherbank-recovery-v1` |
| Binding to device | **None** (portable file) |
| Cloud upload | **Forbidden** |
| P3 JobQueue | Long-term (after P1+P2) |
| Forgotten-PIN path | Allowed via backup file replace-seal |

---

## Approval

- [x] Design B (persistence-first) chosen by user 2026-07-20  
- [x] Mnemonic ciphered file backup + recovery password added by user  
- [x] User review: rename Non-goals → Long-term goals; otherwise approved 2026-07-20  

**Next:** implementation plan → Wave 1.
