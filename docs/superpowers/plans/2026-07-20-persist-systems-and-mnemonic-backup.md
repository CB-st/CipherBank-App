# Persist systems + mnemonic backup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship persistence-first scorecard closure (rates cache, Home currency filter, P1/P2 sync queue, vault/wallet/recipient management) plus ciphered mnemonic recovery-file backup/restore, then emulator-verify and re-score.

**Architecture:** Extend `LocalDb` + Core repos for market/sync_meta; add `IMnemonicBackupService` (AES-GCM + PBKDF2, portable JSON file); wire Home/Profile/Welcome/Send UI; simplified `ISyncJobQueue` (P1/P2 only). Daily PIN and device-secret seal unchanged; recovery password never persisted.

**Tech Stack:** .NET 10 MAUI, Microsoft.Data.Sqlite, existing `CryptoBox`/`MnemonicHelper`/`IStepUpAuth`, `IPublicQuoteService`, xUnit + FluentAssertions.

**Spec:** `docs/superpowers/specs/2026-07-20-persist-systems-design.md`

## Global Constraints

- Mnemonic / recovery password / device secret / PIN plaintext never on HTTP or in logs.
- Backup file is self-contained ciphertext; **not** bound to device Keystore.
- Recovery password min length **12**; PBKDF2-SHA256 **600000** iterations for file KDF.
- JobQueue this delivery: **P1 + P2 only** (P3 = long-term).
- Scorecard denominator excludes long-term rows (Activity, securities, demo seed).
- Android install: `-f net10.0-android -p:EmbedAssembliesIntoApk=true`.
- Tests: `dotnet test CipherBank-app.Tests -p:CollectCoverage=false` must stay green after each task commit.
- All work on `feat/cora-redesign-maui`.

## File map

| File | Responsibility |
|------|----------------|
| `CipherBank-app.Core/Custody/MnemonicBackupService.cs` | Create/open `cipherbank-recovery-v1` files |
| `CipherBank-app.Core/Custody/AuthReason.cs` | Add `BackupExport` if not reusing `RevealKeys` |
| `CipherBank-app.Core/Persist/LocalDb.cs` | Migrate `rates_snapshot`, `sync_meta`; keep/extend `ohlc` |
| `CipherBank-app.Core/Persist/RatesCache.cs` | `IRatesCache` upsert/get |
| `CipherBank-app.Core/Persist/MarketRepository.cs` | OHLC window upsert/get |
| `CipherBank-app.Core/Persist/SyncMetaStore.cs` | KV timestamps / flags |
| `CipherBank-app.Core/Persist/SyncJobQueue.cs` | P1/P2 queue, concurrency 2 |
| `CipherBank-app.Core/Persist/RecipientRepository.cs` | Add `DeleteAsync` |
| `CipherBank-app.Core/V1/IProductApi.cs` (+ Mock/Http) | Vault card add/delete |
| `CipherBank-app/MauiProgram.cs` | DI registrations |
| `CipherBank-app/ViewModels/HomeViewModel.cs` | Visible vs Other assets |
| `CipherBank-app/ViewModels/ProfileViewModel.cs` | Vault CRUD + backup export |
| `CipherBank-app/ViewModels/WelcomeViewModel.cs` / Unlock | Restore-from-file entry |
| `CipherBank-app/ViewModels/SendViewModel.cs` | Recipient delete |
| `CipherBank-app/ViewModels/AddWalletViewModel.cs` | Wallet delete + optional post-create QR |
| Canvas + scorecard docs | Re-score after waves |
| Tests under `CipherBank-app.Tests/` | Custody backup, rates, queue, prefs home filter |

---

### Task 1: Rescore canvas for already-shipped parity

**Files:**
- Modify: `~/.cursor/projects/.../canvases/cora-maui-feature-compare.canvas.tsx` (or repo copy if present)
- Modify: `docs/superpowers/plans/2026-07-19-cora-maui-f6-scorecard.md`
- Modify: `docs/superpowers/plans/2026-07-20-cora-maui-overhaul-to-main.md` (pointer to persist plan)

**Interfaces:**
- Produces: accurate baseline % before coding

- [ ] **Step 1: Flip shipped rows to `parity`**

In the feature canvas `FEATURES` array, set `status: 'parity'` and update `maui` text for:
- Splash / boot gate → SplashPage + MinSplashDuration
- Public market /iquote client → PublicApiClient / IPublicQuoteService
- Currency visibility toggles → Profile EnabledCurrencies (note Home filter still Wave 2 UI)
- Cora FAB → also mention CoraBar if listed under Design

- [ ] **Step 2: Update F6.3 scorecard counts + “remaining gaps”**

Replace remaining-gaps list with persistence-first queue from the spec; note long-term goals separately.

- [ ] **Step 3: Commit**

```bash
git add docs/superpowers/plans/2026-07-19-cora-maui-f6-scorecard.md docs/superpowers/plans/2026-07-20-cora-maui-overhaul-to-main.md
# + canvas path if tracked in repo
git commit -m "$(cat <<'EOF'
docs: rescore canvas for splash, iquote, and currency prefs

EOF
)"
```

---

### Task 2: Mnemonic backup Core service (TDD)

**Files:**
- Create: `CipherBank-app.Core/Custody/IMnemonicBackupService.cs`
- Create: `CipherBank-app.Core/Custody/MnemonicBackupService.cs`
- Create: `CipherBank-app.Tests/Custody/MnemonicBackupServiceTests.cs`

**Interfaces:**
- Consumes: `MnemonicHelper.Validate` / `Normalize`
- Produces:

```csharp
public interface IMnemonicBackupService
{
    Task<byte[]> CreateBackupFileAsync(string mnemonic, string recoveryPassword, string? hint = null, CancellationToken ct = default);
    Task<string> OpenBackupFileAsync(ReadOnlyMemory<byte> fileBytes, string recoveryPassword, CancellationToken ct = default);
}
```

JSON fields (SCREAMING): `FORMAT`, `KDF`, `ITERATIONS`, `SALT_B64`, `NONCE_B64`, `TAG_B64`, `CIPHERTEXT_B64`, `CREATED_AT`, `HINT?`.  
`FORMAT` = `cipherbank-recovery-v1`, `KDF` = `PBKDF2-SHA256`, `ITERATIONS` = `600000`.

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public async Task RoundTrip_opens_same_mnemonic()
{
    var svc = new MnemonicBackupService();
    string mnemonic = MnemonicHelper.Generate();
    byte[] file = await svc.CreateBackupFileAsync(mnemonic, "correct-horse-battery-staple");
    string opened = await svc.OpenBackupFileAsync(file, "correct-horse-battery-staple");
    opened.Should().Be(MnemonicHelper.Normalize(mnemonic));
    Encoding.UTF8.GetString(file).Should().NotContain(mnemonic.Split(' ')[0]);
}

[Fact]
public async Task WrongPassword_throws()
{
    var svc = new MnemonicBackupService();
    byte[] file = await svc.CreateBackupFileAsync(MnemonicHelper.Generate(), "correct-horse-battery-staple");
    var act = async () => await svc.OpenBackupFileAsync(file, "wrong-password-here");
    await act.Should().ThrowAsync<CryptographicException>();
}

[Fact]
public async Task ShortPassword_rejected_on_create()
{
    var svc = new MnemonicBackupService();
    var act = async () => await svc.CreateBackupFileAsync(MnemonicHelper.Generate(), "short");
    await act.Should().ThrowAsync<ArgumentException>();
}
```

- [ ] **Step 2: Run tests — expect FAIL**

```bash
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj --filter "FullyQualifiedName~MnemonicBackup" -p:CollectCoverage=false
```

- [ ] **Step 3: Implement `MnemonicBackupService`**

- Reject recovery password length &lt; 12.
- Validate mnemonic before seal.
- PBKDF2-SHA256 600k → 32-byte key; AES-GCM; pack JSON as above (reuse patterns from `CryptoBox` but **do not** reuse device-secret salts).
- On open: parse → derive → decrypt → Validate → return normalized phrase.
- Zero key material in `finally` where practical.

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit**

```bash
git add CipherBank-app.Core/Custody/MnemonicBackupService.cs CipherBank-app.Core/Custody/IMnemonicBackupService.cs CipherBank-app.Tests/Custody/MnemonicBackupServiceTests.cs
git commit -m "$(cat <<'EOF'
feat: ciphered mnemonic recovery file (PBKDF2 + AES-GCM)

EOF
)"
```

---

### Task 3: LocalDb migrate — rates_snapshot + sync_meta

**Files:**
- Modify: `CipherBank-app.Core/Persist/LocalDb.cs`
- Create: `CipherBank-app.Tests/Persist/LocalDbMigrationTests.cs`

**Interfaces:**
- Produces: tables created on `InitializeAsync` (idempotent `CREATE TABLE IF NOT EXISTS`)

```sql
CREATE TABLE IF NOT EXISTS rates_snapshot (
  symbol TEXT PRIMARY KEY NOT NULL,
  usd REAL NOT NULL,
  change24h REAL NOT NULL DEFAULT 0,
  updated_at INTEGER NOT NULL
);
CREATE TABLE IF NOT EXISTS sync_meta (
  key TEXT PRIMARY KEY NOT NULL,
  value TEXT NOT NULL,
  updated_at INTEGER NOT NULL
);
-- existing ohlc(symbol, t, v) remains
```

- [ ] **Step 1: Failing test** — after `InitializeAsync`, `SELECT name FROM sqlite_master` contains `rates_snapshot` and `sync_meta`.

- [ ] **Step 2: Implement DDL in `InitializeAsync`**

- [ ] **Step 3: Tests pass + commit**

```bash
git commit -m "$(cat <<'EOF'
feat: add rates_snapshot and sync_meta SQLite tables

EOF
)"
```

---

### Task 4: IRatesCache + MarketRepository (TDD)

**Files:**
- Create: `CipherBank-app.Core/Persist/IRatesCache.cs`, `RatesCache.cs`
- Create: `CipherBank-app.Core/Persist/IMarketRepository.cs`, `MarketRepository.cs`
- Create: `CipherBank-app.Tests/Persist/RatesCacheTests.cs`, `MarketRepositoryTests.cs`

**Interfaces:**

```csharp
public sealed record RateRow(string Symbol, double Usd, double Change24h, long UpdatedAtMs);

public interface IRatesCache
{
    Task UpsertAsync(IEnumerable<RateRow> rows, CancellationToken ct = default);
    Task<IReadOnlyList<RateRow>> GetAsync(IEnumerable<string>? symbols = null, CancellationToken ct = default);
}

public interface IMarketRepository
{
    Task UpsertOhlcAsync(string symbol, IEnumerable<(long T, double V)> points, CancellationToken ct = default);
    Task<IReadOnlyList<(long T, double V)>> GetOhlcAsync(string symbol, long? fromT = null, CancellationToken ct = default);
}
```

- [ ] **Step 1: Tests** — upsert then get filters by symbol; OHLC ordered by `t`.

- [ ] **Step 2: Implement against `ILocalDb.Open()`**

- [ ] **Step 3: Register DI in `MauiProgram.cs`**

- [ ] **Step 4: Commit**

```bash
git commit -m "$(cat <<'EOF'
feat: rates cache and OHLC market repository

EOF
)"
```

---

### Task 5: SyncJobQueue P1/P2

**Files:**
- Create: `CipherBank-app.Core/Persist/ISyncJobQueue.cs`, `SyncJobQueue.cs`
- Create: `CipherBank-app.Tests/Persist/SyncJobQueueTests.cs`
- Modify: `MauiProgram.cs`

**Interfaces:**

```csharp
public enum SyncPriority { P1 = 1, P2 = 2 }

public interface ISyncJobQueue
{
    void Enqueue(string key, SyncPriority priority, Func<CancellationToken, Task> work);
    Task DrainAsync(CancellationToken ct = default); // optional for tests
}
```

Rules: concurrency **2**; at most one in-flight job per `key`; higher priority (lower enum value) first; P2 = cold rates hydrate; P1 = OHLC write-through.

- [ ] **Step 1: Test** — enqueue P2 then P1; assert P1 runs before waiting P2 when concurrency allows; duplicate key coalesces or skips second while in-flight.

- [ ] **Step 2: Implement + DI singleton**

- [ ] **Step 3: Commit**

```bash
git commit -m "$(cat <<'EOF'
feat: P1/P2 sync job queue for market persist

EOF
)"
```

---

### Task 6: Wire P2 hydrate + P1 chart write-through

**Files:**
- Modify: `CipherBank-app/ViewModels/HomeViewModel.cs`
- Modify: `CipherBank-app/MauiProgram.cs` (ensure `IPublicQuoteService` available to hydrate helper)
- Create: `CipherBank-app.Core/Persist/MarketBootstrap.cs` (optional helper)

**Logic:**
- On Home appearing / session unlock path: enqueue P2 `p2-rates` — `IRatesCache.GetAsync(held∩enabled)`; if empty/stale (&gt;15 min), call `IPublicQuoteService.GetInverseQuoteAsync(sym, 1m, "USD")` per symbol and `UpsertAsync`.
- On `SetRangeAsync` / chart load: after `GetHistoryAsync`, enqueue P1 `p1-ohlc-{symbol}` → `UpsertOhlcAsync`.

- [ ] **Step 1: Implement wiring (manual smoke later)**

- [ ] **Step 2: Unit-test helper mapping PublicQuote → RateRow if extracted**

- [ ] **Step 3: Commit**

```bash
git commit -m "$(cat <<'EOF'
feat: hydrate rates from SQLite and persist chart OHLC

EOF
)"
```

---

### Task 7: Home EnabledCurrencies filter + Other assets

**Files:**
- Modify: `CipherBank-app/ViewModels/HomeViewModel.cs`
- Modify: `CipherBank-app/Views/HomePage.xaml`

**Interfaces:**
- Produces: `VisibleHoldings` / `OtherHoldings` (or filtered lists bound in XAML)

- [ ] **Step 1: Split portfolio holdings** — `EnabledCurrencies` (case-insensitive) → visible; rest → other; empty enabled falls back to defaults via prefs normalize.

- [ ] **Step 2: XAML** — primary list + expandable “Other assets (N)” section.

- [ ] **Step 3: Commit**

```bash
git commit -m "$(cat <<'EOF'
feat: filter Home holdings by enabled currencies

EOF
)"
```

---

### Task 8: Vault card add/delete API + Profile UI

**Files:**
- Modify: `CipherBank-app.Core/V1/IProductApi.cs`, `WireModels.cs` (if needed), `MockProductApi.cs`
- Modify: `CipherBank-app/Services/HttpProductApi.cs`
- Modify: `CipherBank-app/ViewModels/ProfileViewModel.cs`, `Views/ProfilePage.xaml`

**Interfaces:**

```csharp
Task<VaultCardDto> AddVaultCardAsync(VaultCardDto card, string idempotencyKey, CancellationToken ct = default);
Task DeleteVaultCardAsync(string cardId, CancellationToken ct = default);
```

Mock: in-memory list mutate. Http: `POST v1/vault/cards`, `POST v1/vault/cards/{id}/delete` per API contract.

- [ ] **Step 1: Extend interface + mock tests**

- [ ] **Step 2: Http + Profile RelayCommands** (`AddDemoCardAsync`, `RemoveCardAsync`) with step-up if touching POS-active card

- [ ] **Step 3: Commit**

```bash
git commit -m "$(cat <<'EOF'
feat: vault card add/remove on Profile

EOF
)"
```

---

### Task 9: Wallet + recipient delete management

**Files:**
- Modify: `CipherBank-app.Core/Persist/RecipientRepository.cs` (+ interface)
- Modify: `CipherBank-app/ViewModels/SendViewModel.cs`, `SendPage.xaml`
- Modify: `CipherBank-app/ViewModels/HomeViewModel.cs` or `AddWalletViewModel.cs` (delete wallet confirm)

- [ ] **Step 1: `RecipientRepository.DeleteAsync(string id)` + test**

- [ ] **Step 2: Send UI remove button/swipe → confirm → delete → refresh list**

- [ ] **Step 3: Home/AddWallet delete local wallet via existing `WalletRepository.DeleteAsync` + confirm**

- [ ] **Step 4: Commit**

```bash
git commit -m "$(cat <<'EOF'
feat: delete local wallets and ACH recipients from UI

EOF
)"
```

---

### Task 10: Backup export / restore UI

**Files:**
- Modify: `AuthReason.cs` — add `BackupExport = …`
- Modify: `MauiProgram.cs` — register `IMnemonicBackupService`
- Modify: `ProfileViewModel.cs` / `ProfilePage.xaml` — export flow
- Modify: `WelcomeViewModel.cs` (+ optional Restore page) — pick file + password → SetPin
- Create: platform file save/open helpers under `CipherBank-app/Services/` as needed

**Flow export:**
1. `RequireAsync(BackupExport)` → prompt password×2 (≥12) → optional hint → `CreateBackupFileAsync(ExportMnemonic()!, …)` → save/share bytes → clear password props.

**Flow restore (no sealed wallet):**
1. Welcome “Restore from backup file” → pick bytes → password → `OpenBackupFileAsync` → navigate `SetPin?mnemonic=` (or secure handoff) → existing seal path.

**Flow forgotten PIN (sealed present):**
1. Unlock “Recover with backup file” → open → confirm replace → SetPin → `SealAsync` overwrites blob.

- [ ] **Step 1: DI + Profile export command**

- [ ] **Step 2: Welcome/Unlock restore entry points**

- [ ] **Step 3: Manual checklist note in PR body**

- [ ] **Step 4: Commit**

```bash
git commit -m "$(cat <<'EOF'
feat: export and restore mnemonic via recovery file

EOF
)"
```

---

### Task 11: Emulator smoke + final scorecard

**Files:**
- Modify: canvas + `2026-07-19-cora-maui-f6-scorecard.md`
- Modify: `.superpowers/sdd/progress.md` (branch tip note)

- [ ] **Step 1: Build/install Android Debug with EmbedAssembliesIntoApk**

```bash
dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true
adb install -r <Signed.apk>
```

- [ ] **Step 2: Manual smoke**

Splash → Welcome → (optional restore) → Keys → Quiz → SetPin → Home (Other assets) → Convert → Send delete recipient → Profile vault add/remove → Backup export → clear app data → restore → Home.

- [ ] **Step 3: Re-score persistence rows to parity; compute % vs long-term-excluded denominator**

- [ ] **Step 4: Commit + push**

```bash
git commit -m "$(cat <<'EOF'
docs: persistence-first scorecard closeout after emulator smoke

EOF
)"
git push -u origin HEAD
```

---

## Spec coverage check

| Spec section | Task(s) |
|--------------|---------|
| Part A mnemonic backup file/format/API | 2, 10 |
| Wave 1 rescore + Home filter | 1, 7 |
| Wave 2 rates/OHLC/sync_meta | 3, 4, 6 |
| Wave 3 P1/P2 queue | 5, 6 |
| Wave 4 vault/wallet/recipient + backup UI | 8, 9, 10 |
| Wave 5 emulator + re-score | 11 |
| Long-term (P3, bell, HCE, Activity) | Not tasked |

## Placeholder scan

No TBD/TODO steps; commands and interfaces specified.
