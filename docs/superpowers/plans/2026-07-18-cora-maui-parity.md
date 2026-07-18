# Cora ↔ MAUI Behavioral Parity Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring the MAUI Cora port to behavioral parity with `design_handoff_cipherbank/starter`, closing the gaps from the feature comparison (53% full parity → ~90%+ on shipped Expo capabilities).

**Architecture:** Keep MAUI + `CipherBank-app.Core` as the product. Treat Expo as the behavioral/visual spec. Extend existing patterns (`IAppSession`, `IProductApi`, `IPrefsStore`, `IStreamService`, Shell routes) rather than inventing parallel systems. Prefer Core for pure logic + unit tests; app layer for platform (biometrics, NFC, UI).

**Tech Stack:** .NET 10 MAUI, NBitcoin/Nethereum custody, Microsoft.Data.Sqlite, Plugin.Maui.Biometric (or platform APIs), existing `HttpProductApi` / mock DI, Appium E2E.

**Baseline:** Branch `feat/cora-maui-port`; comparison canvas `cora-maui-feature-compare.canvas.tsx` (86 scored features: 46 parity / 24 partial / 14 Cora-only / 2 MAUI-only).

## Global Constraints

- Do **not** implement shared non-goals: production HCE, VTS/MDES, cloud seed backup, live chain indexers, securities-as-payment.
- Activity tab stays deferred (stub in Expo too) unless explicitly pulled into scope later.
- Dark is default chrome; Cora tokens in `Resources/Styles/Colors.xaml` stay source of truth for color.
- Custody mnemonic never leaves device; never POST seed/PAN.
- Mutations keep `Idempotency-Key`; wire stays SCREAMING_SNAKE.
- Linux builds: `-f net10.0-android` + `-p:EmbedAssembliesIntoApk=true` for emulator install.
- Unit tests: `dotnet test CipherBank-app.Tests` must stay green after each phase.
- Spec references: Expo `docs/PROTOTYPE_MAP.md`, `docs/CUSTODY.md`, `docs/USER_CONFIG.md`, `src/theme/tokens.ts`.

## Out of scope (both sides / later)

| Item | Reason |
|------|--------|
| HCE HostApduService | Documented next-phase in Expo |
| Activity tab UI | Stub in Expo; API fixture only |
| Securities / AAPL pay | Disabled in Expo catalog |
| Pixel-perfect animation parity | Prefer behavior + token fidelity |

## File map (new / primary touchpoints)

| Area | Create / extend |
|------|-----------------|
| Auth step-up | Create `CipherBank-app.Core/Custody/IStepUpAuth.cs`, `CipherBank-app/Services/StepUpAuthService.cs` |
| Biometrics | Modify `CipherBank-app/Services/BiometricService.cs` (+ Android/iOS platform bits) |
| Home layout | Modify `HomeViewModel.cs`, `HomePage.xaml`, `PrefsStore.cs` |
| Stream UI | Create `CipherBank-app/Services/StreamHub.cs`; modify `HomeViewModel`, `ConvertViewModel` |
| Convert UX | Modify `ConvertPage.xaml`, `ConvertViewModel.cs`; optional `Controls/AssetPickerSheet` |
| ACH | Extend `RecipientRepository`, `SendPage` / `SendViewModel` |
| Prefs API | Extend `IProductApi` + mock/http; `PrefsStore` sync |
| Cora chrome | Create `Controls/CoraFab.cs`; modify Shell pages |
| Bootstrap | Create `Services/AccountBootstrapService.cs` |
| Quiz | Modify `BackupQuizViewModel.cs` |

---

## Phase F0 — Baseline & tracking (½ day)

### Task F0.1: Lock acceptance criteria from comparison

**Files:**
- Create: `docs/superpowers/plans/2026-07-18-cora-maui-parity.md` (this file)
- Modify: none required for code

- [ ] **Step 1: Confirm scored gaps**

Parity target = all rows marked `partial` or `cora-only` in the comparison canvas **except** Out-of-scope table above.

Must-close list (copy for PR checklists):

1. Real OS biometrics  
2. Step-up auth before pay / convert / POS / reveal  
3. Backup quiz = 3 random words  
4. Home section visibility + order applied  
5. Values hidden on launch + eye toggle  
6. Chart ranges aligned (at least 1D/1W/1M/1Y)  
7. Stream `RATE.TICK` / settle events refresh Home+Convert  
8. Convert asset pickers + fee/privacy rows  
9. Full ACH recipient fields  
10. Receive asset chips + derivation path  
11. Prefs GET/PUT sync when not mocking  
12. Account bootstrap after seal (returning)  
13. Cora FAB chrome  
14. XMR managed path beyond placeholder (minimal: toast “server wallet” + API call stub wired)

- [ ] **Step 2: Open tracking issue / PR comment**

Comment on PR #15 with the must-close list and link this plan.

- [ ] **Step 3: Commit plan only**

```bash
git add docs/superpowers/plans/2026-07-18-cora-maui-parity.md
git commit -m "docs: add Cora↔MAUI behavioral parity plan"
```

---

## Phase F1 — Session security parity (1–2 days)

### Task F1.1: Real biometrics behind `IBiometricService`

**Decision (locked 2026-07-18):** Full Expo parity — successful OS auth unlocks custody **without** re-entering PIN. Never store PIN plaintext. Implement via **device-secret re-seal** (match Expo `custody.ts`: AES key from random per-install secret in SecureStorage; PIN remains a separate verify gate only).

**Why not “just wire the plugin”:** MAUI today PBKDF2s the AES key **from the PIN** (`CryptoBox.DeriveKey` + `CustodyService.UnlockAsync`). Expo encrypts with a `deviceSecret` and treats PIN/biometrics as boolean gates. Parity requires a custody migration, not only `IBiometricService`.

**Trust model (document in commit/PR):** Logical gate + OS keystore-backed SecureStorage (same as Expo), **not** TEE-bound biometric key release.

**Files:**
- Modify: `CipherBank-app.Core/Custody/CustodyService.cs`, `CryptoBox.cs` (reuse as-is; swap password input to device secret)
- Modify: `CipherBank-app.Core/Session/AppSession.cs` — add `UnlockWithDeviceOwnerAsync()`
- Modify / create: `CipherBank-app/Services/BiometricService.cs` (+ Android/iOS platform partials if Plugin.Maui.Biometric is not net10-ready)
- Modify: `CipherBank-app/ViewModels/UnlockViewModel.cs`
- Modify: `CipherBank-app/Platforms/Android/AndroidManifest.xml` (`USE_BIOMETRIC`)
- SecureStorage key: e.g. `cb_device_secret_v1` via existing `ISecureStore` / `MauiSecureStore`
- Test: `CipherBank-app.Tests/Session/BiometricUnlockContractTests.cs` (+ custody migration tests)

**APIs:**
```csharp
// ICustodyService / IAppSession
Task<bool> UnlockWithDeviceOwnerAsync(); // after IBiometricService success → read device secret → Open blob
// Migration: on next successful PIN unlock of a PIN-derived blob, re-Seal under device secret + persist secret
```

Expo refs: `UnlockScreen.tsx`, `features/vault/custody.ts` (`unlockLocalCustody`, `deviceSecret`).

- [ ] **Step 1: Failing tests** — fake biometric true → `UnlockWithDeviceOwnerAsync` unlocks; cancel/false stays locked; PIN path still works; optional: legacy PIN-blob migrates on one PIN unlock
- [ ] **Step 2: Custody device-secret seal/unlock + migration**
- [ ] **Step 3: Real `IBiometricService`** (plugin or platform BiometricPrompt / LAContext)
- [ ] **Step 4: UnlockViewModel auto-prompt when biometrics available + prefs enabled; success → `UnlockWithDeviceOwnerAsync` → Home; failure/unavailable → PIN
- [ ] **Step 5: `dotnet test CipherBank-app.Tests` + commit**

```bash
git commit -m "feat: real biometrics for unlock parity with Cora"
```

### Task F1.2: Step-up auth (`requireAuth` equivalent)

**Files:**
- Create: `CipherBank-app.Core/Custody/AuthReason.cs`
- Create: `CipherBank-app/Services/IStepUpAuth.cs`, `StepUpAuthService.cs`
- Modify: `PayViewModel`, `ConvertViewModel`, `PosLabViewModel`, `ProfileViewModel.RevealMnemonicAsync`
- Test: `CipherBank-app.Tests/Custody/StepUpAuthTests.cs` (fake biometric/PIN)

**Interfaces:**
```csharp
public enum AuthReason { Payment, Convert, PosAuthorize, PosPresent, RevealKeys, Derive }

public interface IStepUpAuth
{
    Task<bool> RequireAsync(AuthReason reason, CancellationToken ct = default);
}
```

- [ ] **Step 1: Failing tests — RequireAsync false when PIN cancel**
- [ ] **Step 2: Implement dialog + biometric preference**
- [ ] **Step 3: Gate Pay/Convert/POS/Reveal**
- [ ] **Step 4: Tests green + commit**

```bash
git commit -m "feat: step-up auth before sensitive money and vault actions"
```

### Task F1.3: Backup quiz = 3 random words

**Files:**
- Modify: `CipherBank-app/ViewModels/BackupQuizViewModel.cs`, `Views/BackupQuizPage.xaml`
- Test: `CipherBank-app.Tests/Custody/` or new `BackupQuizTests.cs` with pure word-picker helper in Core

**Interfaces:**
```csharp
// CipherBank-app.Core/Custody/BackupQuiz.cs
public static class BackupQuiz
{
    public static IReadOnlyList<(int Index, string Word)> PickRandom(string[] words, int count, Random rng);
}
```

- [ ] **Step 1: Unit test PickRandom uniqueness + indices**
- [ ] **Step 2: Move quiz UI to 3 entries; validate all**
- [ ] **Step 3: Commit**

```bash
git commit -m "feat: three-word backup quiz matching Cora"
```

---

## Phase F2 — Home fidelity (1–2 days)

### Task F2.1: Apply `HomeOrder` / `HomeVisible` (+ holdings vs local)

**Decision (locked 2026-07-18):** Holdings and Local wallets are **first-class home sections**, not a single opaque `assets` blob.

| Concern | Rule |
|--------|------|
| Default layout | Two tables: `holdings` then `localWallets` (each own section / GlassCard) |
| Color coding (always) | **Holdings** = green (`Success` / `#3FA46A`); **Local wallets** = gold (`Gold` / `#F2C14E`) — accent bar, section header tint, or leading rail so identity survives layout changes |
| View option | Pref `AssetsLayout`: `separate` (default) \| `combined` — combined merges into one list/section **but keeps per-row color** so users can still tell local vs holdings |
| Prefs keys | Replace Expo’s single `assets` with `holdings` + `localWallets` in `HomeOrder` / `HomeVisible` (migrate legacy `assets` → both visible at old position) |

**Files:**
- Modify: `Persist/PrefsStore.cs` (`UserPrefs` defaults + migrate `assets`)
- Modify: `ViewModels/HomeViewModel.cs`, `Views/HomePage.xaml`
- Modify: Profile home-layout toggles (if present) for the two keys + AssetsLayout
- Colors: reuse `Success` / `Gold` from `Resources/Styles/Colors.xaml` (do not invent new greens)

**Section keys (default order):**
`cora | balance | quickActions | performance | holdings | localWallets`

- [ ] **Step 1: Prefs defaults + legacy `assets` migration; add `AssetsLayout`**
- [ ] **Step 2: HomeViewModel exposes ordered visible sections; separate tables by default**
- [ ] **Step 3: Color rails — holdings green, local gold — on section chrome and on each row (esp. combined mode)**
- [ ] **Step 4: Combined layout option groups rows into one table without dropping color**
- [ ] **Step 5: Toggle/reorder in Profile → Home reflects; commit**

```bash
git commit -m "feat: apply home section visibility and order from prefs"
```

### Task F2.2: Hide balances + launch hide

**Files:**
- Modify: `HomeViewModel`, `HomePage.xaml`, `PrefsStore` (`ValuesHiddenOnLaunch`)
- [ ] **Step 1: `BalancesHidden` property + toggle command (eye)**
- [ ] **Step 2: On Appearing, if prefs.ValuesHiddenOnLaunch → start hidden**
- [ ] **Step 3: Mask TotalUsd / holdings / local balances with `••••`**
- [ ] **Step 4: Commit**

```bash
git commit -m "feat: hide balances toggle and values-hidden-on-launch"
```

### Task F2.3: Chart ranges 1D / 1W / 1M / 1Y (+ keep 90d optional)

**Files:**
- Modify: `HomePage.xaml`, `HomeViewModel.SetRangeAsync`, `MockProductApi.GetHistoryAsync` range parsing
- [ ] **Step 1: Map UI `1d|1w|1m|1y` to API `range` query**
- [ ] **Step 2: Mock returns appropriate point counts**
- [ ] **Step 3: Commit**

```bash
git commit -m "feat: align home chart ranges with Cora 1D–1Y"
```

### Task F2.4: Stale badge while refreshing

**Files:**
- Modify: `HomeViewModel` (`IsRefreshing` / `IsStale`), `HomePage.xaml`
- [ ] **Step 1: Keep previous totals visible during reload; show small “Updating…” label**
- [ ] **Step 2: Commit**

```bash
git commit -m "feat: stale/updating indicator on home portfolio refresh"
```

---

## Phase F3 — Money UX parity (2–3 days)

### Task F3.1: Convert asset pickers + swap

**Files:**
- Create: `ViewModels/AssetPickItem.cs` (or reuse holdings symbols)
- Modify: `ConvertViewModel`, `ConvertPage.xaml`
- Spec: Expo `AssetSelector.tsx`

- [ ] **Step 1: Expose `FromAssets` / `ToAssets` from portfolio + registry symbols**
- [ ] **Step 2: UI Pickers + Swap command**
- [ ] **Step 3: Commit**

```bash
git commit -m "feat: convert asset pickers and swap control"
```

### Task F3.2: Quote countdown + fee/privacy/settlement rows

**Files:**
- Modify: `ConvertViewModel` (DispatcherTimer or `IDispatcher` tick while locked), `ConvertPage.xaml`
- Spec: Expo `RateLockStrip.tsx`, ConvertScreen info rows

- [ ] **Step 1: Tick `LockCountdown` every 1s while quote valid; clear lock on expiry**
- [ ] **Step 2: Static/info rows: Fee, Privacy (XMR note), Settlement — copy from Expo**
- [ ] **Step 3: Commit**

```bash
git commit -m "feat: convert quote countdown and info rows"
```

### Task F3.3: Full ACH recipient form

**Files:**
- Modify: `Persist/RecipientRepository.cs` / row model if fields missing
- Modify: `SendViewModel`, `SendPage.xaml`
- Spec: Expo `RecipientPickerModal.tsx`, `ach.types.ts`

**Required fields:** name, holder, bank, routing (9), account, type (checking/savings), memo.

- [ ] **Step 1: Extend SQLite schema + migration-safe `CREATE TABLE IF NOT EXISTS` / alter strategy**
- [ ] **Step 2: Add-recipient UI validation (routing length 9)**
- [ ] **Step 3: Unit tests for validation helper**
- [ ] **Step 4: Commit**

```bash
git commit -m "feat: full ACH recipient fields for send parity"
```

### Task F3.4: Receive asset chips + derivation path

**Files:**
- Modify: `ReceiveViewModel`, `ReceivePage.xaml`
- [ ] **Step 1: Chip list BTC/ETH/USD + More from registry**
- [ ] **Step 2: Show `DerivationPath` when local derived wallet selected**
- [ ] **Step 3: Commit**

```bash
git commit -m "feat: receive asset chips and derivation path display"
```

### Task F3.5: Pay privacy callout + recipient chrome

**Files:**
- Modify: `PayPage.xaml`, `PayViewModel` (optional fixed demo recipient label)
- [ ] **Step 1: Violet-tint privacy callout matching Cora copy**
- [ ] **Step 2: Commit**

```bash
git commit -m "feat: pay privacy callout for Cora parity"
```

---

## Phase F4 — Live data & sync (2 days)

### Task F4.1: Stream hub → Home + Convert

**Files:**
- Create: `CipherBank-app/Services/IStreamHub.cs`, `StreamHub.cs` (subscribe once at start)
- Modify: `MauiProgram.cs`, `HomeViewModel`, `ConvertViewModel`
- Modify: `MockStreamService` to occasionally emit richer types if useful

**Interfaces:**
```csharp
public interface IStreamHub
{
    event EventHandler<StreamEvent>? EventReceived;
    void Start();
    void Stop();
}
```

- [ ] **Step 1: StreamHub wraps `IStreamService.EventReceived` singleton fan-out**
- [ ] **Step 2: Home on `RATE.TICK` / `balance.update` → soft refresh portfolio (debounce 1s)**
- [ ] **Step 3: Convert on `RATE.TICK` → refresh quote if unlocked lock expired policy matches Expo**
- [ ] **Step 4: Commit**

```bash
git commit -m "feat: wire product stream events into home and convert"
```

### Task F4.2: Prefs API sync

**Files:**
- Modify: `V1/IProductApi.cs` (+ `GetPrefsAsync` / `PutPrefsAsync` if missing)
- Modify: `MockProductApi`, `HttpProductApi`, `PrefsStore` or `ProfileViewModel`
- Spec: Expo `prefs.store.ts`, fixtures `prefs.json`

- [ ] **Step 1: Add DTOs + mock round-trip tests**
- [ ] **Step 2: On Profile save: local SQLite then PUT when live**
- [ ] **Step 3: On boot after session: GET merge into local**
- [ ] **Step 4: Commit**

```bash
git commit -m "feat: sync prefs with GET/PUT /prefs when live"
```

### Task F4.3: Account bootstrap (returning user)

**Files:**
- Create: `Services/AccountBootstrapService.cs`
- Modify: `AppSession.FinishCustodySetupAsync` / Unlock path for returning
- Spec: Expo `bootstrapAccount.ts`, fixture `account-bootstrap.json`

- [ ] **Step 1: `IProductApi.GetAccountBootstrapAsync` + mock**
- [ ] **Step 2: Import contacts → recipients; prefs merge; never touch mnemonic**
- [ ] **Step 3: Commit**

```bash
git commit -m "feat: account bootstrap pull for returning users"
```

---

## Phase F5 — Chrome & assistant (1 day)

### Task F5.1: Cora FAB

**Files:**
- Create: `Controls/CoraFab.cs` or XAML overlay
- Modify: Home/Convert/Pay/Send/Receive/Profile pages (or single Shell overlay if feasible)
- Spec: Expo `CoraAssistant.tsx`

- [ ] **Step 1: FAB shows `CoraLines.For(screen)` when `CoraEnabled`**
- [ ] **Step 2: Tap expands bubble; respects prefs**
- [ ] **Step 3: Commit**

```bash
git commit -m "feat: Cora FAB assistant chrome"
```

### Task F5.2: Connection chip (optional light)

**Files:**
- Modify: `HomePage.xaml` header row
- [ ] **Step 1: Show Online when `IStreamService.IsConnected` or last portfolio success**
- [ ] **Step 2: Commit**

```bash
git commit -m "feat: home connection chip"
```

---

## Phase F6 — Hardening & verification (1–2 days)

### Task F6.1: XMR managed minimal wiring

**Files:**
- Modify: `AddWalletViewModel`, optional `IProductApi` wallet endpoints
- [ ] **Step 1: Managed mode calls API or clear “lab stub” status — no spend key stored**
- [ ] **Step 2: Commit**

```bash
git commit -m "feat: minimal XMR managed wallet wiring"
```

### Task F6.2: Expand E2E smoke

**Files:**
- Modify: `CipherBank-app.E2ETests/Tests/CoraShellSmokeTests.cs` + page objects
- [ ] **Step 1: Assert chart range chips, hide balances, convert picker, ACH form fields exist**
- [ ] **Step 2: Commit**

```bash
git commit -m "test: expand Cora shell E2E for parity surfaces"
```

### Task F6.3: Parity checklist + PR update

- [ ] **Step 1: Re-score comparison canvas statuses (update canvas data)**
- [ ] **Step 2: Run `dotnet test CipherBank-app.Tests`**
- [ ] **Step 3: Emulator manual: onboard → Home sections → Convert → Send ACH → Receive → Pay step-up → PosLab → Profile reveal**
- [ ] **Step 4: Push commits; update PR #15 body with Phase F0–F6 checklist**

```bash
git push -u origin HEAD
```

---

## Suggested schedule

| Phase | Focus | Est. |
|-------|--------|------|
| F0 | Tracking | 0.5d |
| F1 | Biometrics, step-up, 3-word quiz | 1–2d |
| F2 | Home layout / hide / charts / stale | 1–2d |
| F3 | Convert / ACH / Receive / Pay chrome | 2–3d |
| F4 | Stream + prefs API + bootstrap | 2d |
| F5 | Cora FAB + connection chip | 1d |
| F6 | XMR minimal + E2E + PR | 1–2d |
| **Total** | | **~9–12d** |

## Definition of done

- Comparison must-close list checked (except Out-of-scope).
- `dotnet test CipherBank-app.Tests` green.
- Emulator dark theme still matches Cora tokens.
- PR #15 updated; no HCE/cloud-seed scope creep.

## Self-review (coverage)

| Must-close item | Phase |
|-----------------|-------|
| Biometrics | F1.1 |
| Step-up auth | F1.2 |
| 3-word quiz | F1.3 |
| Home sections | F2.1 |
| Hide balances | F2.2 |
| Chart ranges | F2.3 |
| Stale badge | F2.4 |
| Convert pickers / countdown / rows | F3.1–F3.2 |
| ACH form | F3.3 |
| Receive chips / path | F3.4 |
| Pay privacy | F3.5 |
| Stream UI | F4.1 |
| Prefs API | F4.2 |
| Bootstrap | F4.3 |
| Cora FAB | F5.1 |
| XMR managed minimal | F6.1 |
| E2E / PR | F6.2–F6.3 |
