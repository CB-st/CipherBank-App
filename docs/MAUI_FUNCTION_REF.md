# MAUI_FUNCTION_REF.md — CipherBank C# application interface

The **on-device** contract: how the .NET 10 MAUI app (`CipherBank-app` + `CipherBank-app.Core` + `CipherBank-app.ChallengePass`) boots, seals custody, opens sessions, moves money, and locks. Shaped like [`design_handoff_cipherbank/starter/API.md`](../design_handoff_cipherbank/starter/API.md) — each entry is an **INVOKE** (not an HTTP path) with inputs → logic → outputs.

Companion wire docs:
- Product `/v1`: [`design_handoff_cipherbank/starter/src/mocks/API_CONTRACT.md`](../design_handoff_cipherbank/starter/src/mocks/API_CONTRACT.md)
- Public market: [`design_handoff_cipherbank/starter/docs/PUBLIC_API.md`](../design_handoff_cipherbank/starter/docs/PUBLIC_API.md)

Conventions:
- **Base types:** `CipherBank_app.*` (Core), `CipherBank_app.ChallengePass.*`, MAUI ViewModels under `CipherBank-app/ViewModels/`.
- **INVOKE:** `Type.Method(args)` — async methods return `Task` / `Task<T>` unless noted.
- **Auth surface:** product Bearer token lives in `IAppSession.AccessToken` after unlock; challenge/pass proof is built *before* that token exists.
- **Money:** Core DTOs follow wire SCREAMING_SNAKE; ViewModels keep UI camelCase.
- **Idempotency:** convert / transfer / pay pass client-generated `Idempotency-Key` through `IProductApi`.
- **Errors:** ViewModels surface `Error` strings or `IDialogService` alerts; Core throws on crypto/custody failure.
- **Never on the wire:** mnemonic, BIP39 entropy, PIN plaintext, device secret, account private key, spend keys, PAN/CVV, full ACH account numbers (bootstrap stores `****`+last4 only).

---

## Services overview

| # | Domain | Purpose | Key INVOKEs |
|---|---|---|---|
| 1 | **Boot & shell** | DB init, route to Welcome/Unlock | `AppShell.Bootstrap`, `AppSession.Boot` |
| 2 | **Custody & PIN** | Seal mnemonic, verify PIN, device secret | `Custody.Seal`, `Pin.Set/Verify`, `CryptoBox.*` |
| 3 | **Session** | Unlock, lock, idle, product token | `AppSession.Unlock*`, `Lock`, `FinishCustodySetup` |
| 4 | **Challenge / pass** | Lab · A1 · A2 session open body | `SessionProof.BuildOpenBody`, suites A1/A2 |
| 5 | **Product API** | Portfolio, quotes, money moves, vault | `IProductApi.*` → HTTP or mock |
| 6 | **Stream** | Live balance / rate ticks | `Stream.Connect`, `StreamHub.Start` |
| 7 | **Persist** | SQLite wallets, prefs, recipients | `LocalDb.*`, repos, prefs sync |
| 8 | **Wallets** | Derive addresses, managed XMR | `LocalWalletSeeder`, `AddressDerive`, `CreateWallet` |
| 9 | **UI flows** | Onboarding → PIN → Home → tabs | ViewModel RelayCommands |
| 10 | **Step-up & NFC** | Biometrics/PIN gates, POS lab | `StepUp.Require`, `Nfc.Present` |

---

## Call graph (unlock / seal)

```
UI (SetPin | Unlock)
  → AppSession.FinishCustodySetup | Unlock*
       → Custody.Seal | Unlock
       → LocalWalletSeeder.EnsureDerived          (seal path only)
       → CompleteUnlock
            → IProductApi.CreateSession
                 → ISessionProofBuilder.BuildOpenBody
                      ├ Lab → { DEVICE_ATTESTATION: "lab" }
                      ├ A1 → TwoStepChallengePass (X25519 seal)
                      └ A2 → PQ key-share + channel challenge/pass
            → IProductSessionStore.Save
            → IStreamService.Connect + StreamHub.Start
            → PrefsSync.PullMerge
            → AccountBootstrap.Apply              (unlock path only)
  → Shell → //HomePage

Idle / Profile.Lock
  → AppSession.Lock → stop hub, clear custody RAM, clear tokens
  → AppIdleLockService → IPqChannel.Clear → //UnlockPage
```

---

## 1 · Boot & shell

Keys and DB live on-device. Shell decides Welcome vs Unlock from sealed-wallet presence.

### `INVOKE AppShell.BootstrapAsync`

**File:** `CipherBank-app/AppShell.xaml.cs`

```
{} → InitializeAsync → BootAsync → IdleLock.Start → route
```

Logic:
1. `ILocalDb.InitializeAsync` — create `cipherbank.db` + DDL (`wallets`, `prefs`, `recipients`, `ohlc`).
2. `IAppSession.BootAsync` — set `HasWallet` from sealed blob; load `IdleMs` from prefs.
3. `AppIdleLockService.Start` — 5s timer calling `CheckIdleAndMaybeLock`.
4. Navigate `//UnlockPage` if `HasWallet`, else `//WelcomePage`. On exception → Welcome.

### `INVOKE AppSession.BootAsync`

**File:** `CipherBank-app.Core/Session/AppSession.cs`

```
{} → { HasWallet, IdleMs }
```

Logic: `ICustodyService.HasSealedWalletAsync` → `IPrefsStore.LoadAsync` → assign `IdleMs` from `LockIdleSeconds` (ms). Sets `IsBooting` around the work.

### Routes (Shell paths)

| Constant | Path | Role |
|---|---|---|
| `Welcome` | `//WelcomePage` | Create / returning |
| `Keys` | `//KeysPage` | Show BIP39 |
| `BackupQuiz` | `//BackupQuizPage` | Confirm 3 words |
| `SetPin` | `//SetPinPage` | Seal wallet |
| `Unlock` | `//UnlockPage` | PIN / biometrics |
| `Home`…`Profile` | tab roots | Product surface |
| `PosLab` / `AddWallet` | push routes | Labs / add account |

---

## 2 · Custody & PIN

Keys live on-device. Server never sees mnemonic or PIN.

### `INVOKE PinService.SetPinAsync(pin)`

**File:** `Custody/PinService.cs`

```
{ pin: string } → store hash+salt; clear lockout
```

Logic: random salt → PBKDF2-SHA256 (120k) → secure store keys `cb_pin_hash` / `cb_pin_salt`. **PIN plaintext never persisted.**

### `INVOKE PinService.VerifyPinAsync(pin)` → `bool`

Logic: if lockout (`cb_pin_lock_until`) → fail; fixed-time compare of PBKDF2 hash; on fail increment `cb_pin_fails` (5 fails → 5 min lockout); on success reset counters.

### `INVOKE CryptoBox.Seal(plaintext, pinOrSecret)` → `string` (base64)

**File:** `Custody/CryptoBox.cs`

Logic: PBKDF2-SHA256 (210k) → AES-GCM → pack `salt|nonce|tag|cipher`. `Open` reverses and zeros key material.

### `INVOKE CustodyService.SealAsync(mnemonic, pin)`

```
{ mnemonic, pin } → sealed blob + device secret; IsUnlocked=true (TTL 5m)
```

Logic:
1. `MnemonicHelper.Validate` (BIP39).
2. `PinService.SetPinAsync(pin)`.
3. Generate random **device secret**; store `cb_device_secret_v1`.
4. `CryptoBox.Seal(mnemonic, deviceSecret)` → `cb_custody_blob`.
5. Hold mnemonic in RAM with `SessionExpiresAt` (+5 min).

Legacy: older seals used PIN as AES passphrase; unlock migrates to device-secret seal.

### `INVOKE CustodyService.UnlockAsync(pin)` → `bool`

Logic: `VerifyPinAsync` → open blob with device secret (or legacy PIN) → RAM mnemonic + TTL.

### `INVOKE CustodyService.UnlockWithDeviceSecretAsync()` → `bool`

Logic: open blob with stored device secret (after OS biometrics). No PIN entry.

### `INVOKE CustodyService.Lock()`

Logic: clear in-memory mnemonic + expiry. Blob stays sealed at rest.

### `INVOKE CustodyService.ExportMnemonic()` → `string?`

Logic: return RAM mnemonic iff unlocked. Gated in UI by step-up (`AuthReason.RevealKeys`). **Never HTTP.**

### `INVOKE MnemonicHelper.Generate | Validate | Entropy`

**File:** `Custody/Mnemonic.cs` — BIP39 12-word English (NBitcoin). `Entropy` recovers bytes for account-key HKDF; never leave device.

### `INVOKE BackupQuiz.PickRandom(words, count)` → quiz indices

Pure Fisher–Yates for onboarding UI.

---

## 3 · Session (AppSession)

### `INVOKE AppSession.FinishCustodySetupAsync(mnemonic, pin)`

```
{ mnemonic, pin } → sealed + seeded + product session (no bootstrap)
```

Logic:
1. `Custody.SealAsync(mnemonic, pin)`.
2. `LocalWalletSeeder.EnsureDerivedAsync(mnemonic)` — BTC/ETH (etc.) addresses → SQLite.
3. `CompleteUnlockAsync(applyBootstrap: false)` — session + stream + prefs pull.
4. Sets `HasWallet = true`.

Called from `SetPinViewModel.SealAsync` then navigate Home.

### `INVOKE AppSession.UnlockAsync(pin)` → `bool`

Logic: `Custody.UnlockAsync(pin)` → if ok `CompleteUnlockAsync(applyBootstrap: true)` (includes account bootstrap).

### `INVOKE AppSession.UnlockWithDeviceOwnerAsync()` → `bool`

Logic: `Custody.UnlockWithDeviceSecretAsync` → same complete unlock with bootstrap.

### `INVOKE AppSession.CompleteUnlockAsync` *(private)*

```
{ applyBootstrap } → AccessToken + live stream + prefs [+ bootstrap]
```

Logic:
1. `IProductApi.CreateSessionAsync` → store `AccessToken` (+ refresh via session store).
2. `IStreamService.ConnectAsync` + `IStreamHub.Start`.
3. `IPrefsSyncService.PullMergeAsync` (failures swallowed).
4. If `applyBootstrap`: `IAccountBootstrapService.ApplyAsync` (failures swallowed).
5. Refresh `IdleMs` from prefs; `Touch()`.

### `INVOKE AppSession.Lock()`

Logic: stop stream hub → custody `Lock` → clear `AccessToken` + `IProductSessionStore.Clear` → disconnect stream → raise `Locked`.

### `INVOKE AppSession.Touch()` / `CheckIdleAndMaybeLock()` → `bool`

`Touch` resets idle clock. `CheckIdleAndMaybeLock`: if unlocked and idle ≥ `IdleMs` → `Lock()`; return whether locked.

### `INVOKE AppIdleLockService.Start | Touch | OnLocked`

**File:** `CipherBank-app/Services/AppIdleLockService.cs`

Logic: dispatcher timer → `CheckIdleAndMaybeLock`. On `Locked`: `IPqChannel.Clear()` then `GoToAsync(Unlock)`.

---

## 4 · Challenge / pass (session open body)

Built by `ISessionProofBuilder` inside `HttpProductApi.CreateSessionAsync` (or mock). Mode from settings: **Lab** | **ChallengePassA1** | **ChallengePassA2**.

### `INVOKE LabSessionProofBuilder.BuildOpenBodyAsync` → `object`

```
{} → { "DEVICE_ATTESTATION": "lab" }
```

Stub; no crypto. Default until live challenge/key-share.

### `INVOKE ChallengePassSessionProofBuilder.BuildOpenBodyAsync` → `object`

**File:** `ChallengePass/…`

Logic:
1. Resolve active suite from `IChallengePassCatalog`.
2. **A1:** `IAccountKeySource.RequireUnlockedKeyPair` → `TwoStepChallengePassStructure.BuildSessionOpenBodyAsync`.
3. **A2:** `RequireHybridIdentity` → `PqChannelChallengePassStructure` (key-share if needed, then channel challenge/pass).
4. Returns body containing `SessionPassDto` fields (SCREAMING wire).

### `INVOKE TwoStepChallengePassStructure.BuildSessionOpenBodyAsync`

```
{ algo, template, accountKeys, wire } → SessionPassDto body
```

Logic:
1. `ISessionChallengeClient.RequestChallengeAsync(accountPkWire)` → ciphertext sealed to account pk.
2. `algo.Open` with account private key → plaintext.
3. `template.Parse` / verify challenge id.
4. `template.BuildPassPayload` (e.g. SHA-256 of plaintext) → `algo.Seal` to API public key.
5. Assemble `SessionPassDto` (`ChallengeId`, `PassCiphertext`, `AccountPublicKey`, `Algorithm`).

**Never on wire:** account private key; clear challenge plaintext after open.

### `INVOKE PqChannelChallengePassStructure.BuildSessionOpenBodyAsync`

Logic:
1. If no channel key: `IPqKeyShareClient.EstablishAsync(deviceIdentity)` → ML-KEM encapsulate + X25519 → HKDF channel key → `IPqChannel.SetChannelKey`.
2. `IPqChannelChallengeSource.RequestChallengeAsync` (bound to key-share id).
3. Channel `Open` challenge → build pass → channel `Seal` pass.
4. Return `SessionPassDto` with PQ algorithm id.

### `INVOKE HybridMlKemX25519Agreement.*`

| INVOKE | Role |
|---|---|
| `DeriveIdentity(entropy)` | Device hybrid public/private from mnemonic entropy |
| `CreateShareAsServer(devicePublic)` | Server encapsulate → response CT + server X25519 |
| `CompleteAsDevice(private, response)` | Decapsulate → shared channel key |

Algorithms: `hybrid-mlkem768-x25519-v1`, channel seal `pq-channel-chacha20poly1305-v1`. Portable X25519 + BCL ChaCha20-Poly1305 (no libsodium on Android).

### `INVOKE CustodyAccountKeySource.RequireUnlockedKeyPair | RequireHybridIdentity`

Logic: `Custody.ExportMnemonic` → `MnemonicHelper.Entropy` → `AccountKeyDerivation.DeriveAccountKey` or hybrid derive. Throws if locked.

### `INVOKE IPqChannel.SetChannelKey | Seal | Open | Clear`

In-memory AEAD channel. Cleared on idle lock. **Channel key never logged.**

### Suite ids

| Suite | Id | Structure |
|---|---|---|
| A1 | `a1-x25519-chacha-v1` | Two-step X25519 |
| A2 | `a2-hybrid-pq-channel-v1` | Hybrid PQ channel |

Registered via `AddChallengePassModule(services, activeSuiteId)`.

---

## 5 · Product API (`IProductApi`)

Implementations: `MockProductApi` (Core, DEBUG) · `HttpProductApi` (MAUI). Paths align with product `/v1` contract.

### `INVOKE IProductApi.CreateSessionAsync` → `SessionDto`

```
{ proofBody from ISessionProofBuilder } → { TOKEN, REFRESH_TOKEN, EXPIRES_AT, … }
```

**Http:** `POST /session` with proof JSON → save via `IProductSessionStore`.

### `INVOKE CreateSessionChallengeAsync(accountPublicKeyWire)` → `SessionChallengeDto`

`POST /session/challenge` `{ ACCOUNT_PUBLIC_KEY }` — A1 client path.

### `INVOKE EstablishKeyShareAsync(KeyShareRequestDto)` → `KeyShareResponseDto`

`POST /session/key-share` — device X25519 + ML-KEM public keys only.

### `INVOKE GetPortfolioAsync` → `PortfolioDto`

`GET /portfolio` — totals, holdings, nested wallets.

### `INVOKE GetHistoryAsync(symbol, range)` → `HistoryPointDto[]`

`GET /history?symbols=&range=` — chart series.

### `INVOKE GetQuoteAsync(from, to)` → `QuoteDto`

`POST /quotes` (or mock) — rate + TTL for convert lock.

### `INVOKE ConvertAsync(from, to, amount, idempotencyKey)` → `MoneyMoveDto`

Mutation + idempotency → accepted / settling; definitive via stream.

### `INVOKE TransferAsync(to, amount, speed, idempotencyKey)` → `MoneyMoveDto`

Send / ACH (`speed`: instant|ach).

### `INVOKE PayAsync(amount, mix, idempotencyKey)` → `MoneyMoveDto`

Multi-asset mix; server mediates; recipient sees single currency.

### `INVOKE GetReceiveAsync(asset)` → `ReceiveDto`

Handle / address / URI / QR payload fields.

### `INVOKE CreateWalletAsync(CreateWalletRequestDto)` → `CreateWalletResultDto`

Managed / unmanaged / watch. **Result never includes spend key.**

### `INVOKE GetVaultCardsAsync` / `GetVaultBinariesAsync`

Last4 / metadata only — **no PAN, no mnemonic.**

### `INVOKE CreatePosSessionAsync` / `AuthorizePosAsync` / `ConfirmPosAsync`

POS lab session lifecycle → `PosSessionDto`.

### `INVOKE GetPrefsAsync` / `PutPrefsAsync` / `GetAccountBootstrapAsync`

Prefs wire + bootstrap (prefs + ACH contacts). Bootstrap **never** carries custody material.

---

## 6 · Stream

### `INVOKE IStreamService.ConnectAsync(token?)` / `DisconnectAsync`

**Live:** `ClientWebSocketStreamService` — WSS, parse `{ TYPE, PAYLOAD }`.  
**Mock:** `MockStreamService` — synthetic `RATE.TICK` / `balance.update`.

### `INVOKE StreamHub.Start` / `Stop`

Single fan-out of `EventReceived` to UI (Home soft-refresh).

### Stream event types (wire)

```
BALANCE.UPDATE | RATE.TICK | CONVERT.SETTLED | TRANSFER.SETTLED | PAYMENT.SETTLED | POS.SETTLED
```

### `INVOKE EventDebouncer.DebounceAsync(action)`

Cancel prior delay; fire once after quiet period (Home rate ticks).

---

## 7 · Persist

### `INVOKE LocalDb.InitializeAsync` / `Open`

SQLite at app data `cipherbank.db`. Android ships **Bionic** `libe_sqlite3` via `SQLitePCLRaw.lib.e_sqlite3.android` only (desktop natives excluded from Core APK assets).

### `INVOKE WalletRepository.ListAsync | UpsertAsync | DeleteAsync`

Rows: id, symbol, label, **address**, path, accountIndex, kind, createdAt — **no private keys.**

### `INVOKE RecipientRepository.*` + `AchRecipientValidation`

Local ACH payees. `Validate` / `MaskAccount` / `MaskRouting`. `SeedDefaultsIfEmptyAsync` inserts demo payees.

### `INVOKE PrefsStore.LoadAsync | SaveAsync`

JSON blob under prefs key `user_prefs` → `UserPrefs` (home order/visibility, Cora, idle seconds, appearance, base currency, …).

### `INVOKE PrefsSyncService.PullMergeAsync | SaveAndPushAsync`

Local SQLite ↔ `GET/PUT /prefs` via `PrefsMerge.Merge` (preserves local `AssetsLayout` if remote omits).

### `INVOKE AccountBootstrapService.ApplyAsync`

`GET /account/bootstrap` → merge prefs → upsert recipients (routing 9 digits; account as masked last4). **Does not touch custody.**

---

## 8 · Wallets

### `INVOKE LocalWalletSeeder.EnsureDerivedAsync(mnemonic, symbols?)`

For each derivable symbol missing a row: `AddressDerive.Derive` → upsert. Mnemonic used only in-process.

### `INVOKE AddressDerive.Derive(symbol, mnemonic, accountIndex)` → `DerivedAddress`

| Symbol | Path |
|---|---|
| BTC | BIP84 `m/84'/0'/0'/0/i` |
| LTC | BIP84 coin 2 |
| DOGE | BIP44 `m/44'/3'/0'/0/i` |
| ETH | BIP44 `m/44'/60'/0'/0/i` (Nethereum) |

XMR: **managed** via `CreateWalletAsync` (server spend key); not locally derived.

### `INVOKE AddressValidate.IsValid` / `PaymentUri.Build` / `QrCodeGenerator.ToPngBytes`

Pure helpers for receive UI.

### `INVOKE WalletRegistry.Get | All`

Module metadata: derive+watch vs managed/server.

---

## 9 · UI flows (ViewModels)

CommunityToolkit `[RelayCommand]` → `XxxCommand`. Query attrs via `IQueryAttributable`.

### Onboarding

| INVOKE | Logic → next |
|---|---|
| `Welcome.CreateWalletAsync` | → `//KeysPage` |
| `Welcome.ReturningAsync` | sealed? → Unlock : Keys |
| `Keys.ContinueAsync` | clipboard optional; → `BackupQuiz?mnemonic=` |
| `BackupQuiz.VerifyAsync` | 3 words match → `SetPin?mnemonic=` |
| **`SetPin.SealAsync`** | validate PIN≥6 + match + BIP39 → `FinishCustodySetupAsync` → `//HomePage`; errors → `Error` string |

### Unlock

| INVOKE | Logic |
|---|---|
| `Unlock.AppearingAsync` | maybe auto biometrics if device secret + OS available |
| `Unlock.UnlockAsync` | lockout check → `AppSession.UnlockAsync(Pin)` → Home |
| `Unlock.UnlockWithBiometricsAsync` | OS auth → `UnlockWithDeviceOwnerAsync` → Home |

### Home

| INVOKE | Logic |
|---|---|
| `Home.AppearingAsync` | `Touch`; portfolio; local wallets; history; subscribe stream |
| Stream ticks | debounced soft `GetPortfolioAsync` |
| `ToggleBalancesHidden` | mask UI values |
| `SetRangeAsync` | reload sparklines |
| `GoConvert/Send/Receive/Pay` / `AddWallet` | `Touch` + Shell navigate |

### Convert / Send / Pay / Receive / AddWallet / PosLab

| INVOKE | Gates | API |
|---|---|---|
| `Convert.LockQuoteAsync` | — | `GetQuoteAsync` + countdown |
| `Convert.ConvertAsync` | unlocked + step-up `Convert` | `ConvertAsync` |
| `Send.SendAsync` | unlocked | `TransferAsync` |
| `Send.AddRecipientAsync` | validation | `RecipientRepository.Upsert` |
| `Pay.PayAsync` | unlocked + step-up `Payment` | `PayAsync` |
| `Receive.LoadAsync` / `DeriveNewAsync` | — | derive or `GetReceiveAsync` + QR |
| `AddWallet.SaveAsync` | mode | derive / watch / `CreateWalletAsync` |
| `PosLab.StartSessionAsync` | step-up | Create → Authorize → Confirm |
| `PosLab.PresentNfcAsync` | step-up | `INfcPresentmentService.PresentAsync` (**tokenRef only**) |

### Profile

| INVOKE | Logic |
|---|---|
| `SavePrefsAsync` | `PrefsSync.SaveAndPushAsync`; apply theme + `IdleMs` |
| `RevealMnemonicAsync` | step-up `RevealKeys` → `ExportMnemonic`; auto-clear ~30s |
| `Lock` | `AppSession.Lock` → Unlock |
| `OpenPosLabAsync` | → PosLab |

### Cora FAB

`CoraFab` control: `CoraLines.For(ScreenKey)`; visibility from `UserPrefs.CoraEnabled`. Copy-only; no network.

---

## 10 · Step-up & platform

### `INVOKE StepUpAuthService.RequireAsync(reason)` → `bool`

Prefer `IStepUpChallenges.TryBiometricsAsync`; else PIN prompt → `PinService.VerifyPinAsync`. Reasons: `Payment`, `Convert`, `PosAuthorize`, `PosPresent`, `RevealKeys`, `Derive`.

### `INVOKE MauiSecureStore.*`

MAUI `SecureStorage` adapter for `ISecureStore`.

### `INVOKE IBiometricService.AuthenticateAsync`

OS biometrics before device-secret unlock / step-up.

### `INVOKE NfcPresentmentPayload.ToJson` / `TryParse`

Payload is **token reference only** — never PAN.

---

## DI map (MauiProgram)

| Port | Default (DEBUG mock off / on) |
|---|---|
| `ISecureStore` | `MauiSecureStore` |
| `IPinService` / `ICustodyService` | Core |
| `ILocalDb` + repos + seeder | Core SQLite |
| `ISessionProofBuilder` | Lab / A1 / A2 from `SessionProofMode` |
| `IProductApi` | `HttpProductApi` / `MockProductApi` |
| `IStreamService` | WSS / `MockStreamService` |
| `IAppSession` | `AppSession` |
| Challenge clients | HTTP or in-memory |
| `IPqChannel` | `PqSymmetricChannel` (cleared on lock) |
| ViewModels / Pages | Transient |

---

## Cross-cutting requirements

- **Custody hygiene:** mnemonic only in sealed blob + short RAM TTL; PIN only as PBKDF2 hash; device secret never leaves secure store.
- **PQ hygiene:** channel key wiped on lock; no key logging.
- **Android natives:** do not package desktop `SQLitePCLRaw.lib.e_sqlite3` assets into the APK (ExcludeAssets on Core pin); use `.android` package.
- **Wire hygiene:** SCREAMING_SNAKE on product/public HTTP; UI stays camelCase.
- **Idempotency + stream:** mutations return accepted; UI may optimistically update; settle via WSS.
- **Stable error surfacing:** ViewModel `Error` / dialogs; machine codes from API when live (`quote_expired`, `insufficient_funds`, …).

---

## Build priority (client)

1. **Boot + Custody + SetPin/Unlock** (unblocks everything) — SQLite + secure store.
2. **Session proof** (Lab → A1 → A2) + stream connect.
3. **Home portfolio/history** + prefs sync.
4. **Convert / Send / Pay / Receive** + step-up.
5. **Managed XMR wallets** + POS lab / NFC.

## Related docs

| Doc | Role |
|---|---|
| [architecture.md](architecture.md) | Layers, HTTP pipeline |
| [app/viewmodels.md](app/viewmodels.md) | Legacy VM notes |
| [core/services.md](core/services.md) | Older service index |
| Plans under `docs/superpowers/plans/` | Cora MAUI / challenge-pass / PQ channel |
