# CipherBank MAUI prototype — build log

**Date:** 2026-07-26  
**Product destination:** .NET 10 MAUI Shell (`CipherBank-app`)  
**Runner of record:** Appium E2E (`CipherBank-app.E2ETests`)  
**Out of merge path:** Expo / `design_handoff_cipherbank/` (no Core or Shell dependency)

This log condenses spent design/plan notes into one map of **what was added**, **where it lives**, and **what it connects to**. Day-to-day coding rules live in root [`AGENTS.md`](../AGENTS.md).

Stacked review PRs (replace mega-PR Application Prototype 1):

| PR | Branch | Layer |
|----|--------|-------|
| M1 | `prototype/maui-m1` | Core library + unit tests (no ChallengePass) |
| M2 | `prototype/maui-m2` | ChallengePass + tests + solution entry |
| M3 | `prototype/maui-m3` | MAUI Shell + AGENTS + function-ref docs |
| M4 | `prototype/maui-m4` | Appium E2E, harness scripts, test docs (this log) |

Source tip before split: `feat/cora-redesign-maui` @ `7034e4b` (backup ref `refs/backup/pre-maui-split-*`).

---

## Connection diagram

```
scripts/e2e-android.sh ──► AVD CipherBank_API34 + Appium UiAutomator2
         │
         ▼
CipherBank-app.E2ETests ── AutomationIds / page objects ──► CipherBank-app (Shell)
         │                                                      │
         │                                                      ├──► CipherBank-app.Core
         │                                                      └──► CipherBank-app.ChallengePass
         ▼
docs/tests/gaps/   ◄── Fact fail under E2E_RUN=1 (no soft-pass)
StoryJournal       ◄── PIN / mnemonic / steps (dev/test OK to log under gitignored artifacts/)
```

**Package wipe (MAUI):** `adb shell pm clear com.companyname.cipherbankapp`  
**E2E harness credentials:** required via env or gitignored `artifacts/e2e-local.env` (not source literals). Suggested lab values: [`docs/tests/e2e-tests.md`](tests/e2e-tests.md) § Harness credentials.

---

## M1 — Core foundations

**Lives:** `CipherBank-app.Core/`, matching tests under `CipherBank-app.Tests/` (excluding ChallengePass).

| Area | Path | Connects to |
|------|------|-------------|
| Custody (PIN, mnemonic, step-up, crypto box) | `Core/Custody/*` | Shell unlock / onboarding / backup |
| Session | `Core/Session/AppSession.cs` | Shell idle lock + unlock |
| Local persist / market / wallets DB | `Core/Persist/*` | Home, Convert, recipients |
| Product V1 mock/API contracts | `Core/V1/*` | Shell HTTP product API + streams |
| Wallets / QR / address derive | `Core/Wallets/*` | Receive, Send, AddWallet |
| Charts / Cora lines / POS NFC payload | `Core/Charts`, `Core/Cora`, `Core/Pos` | Shell charts + PosLab |
| Unit tests | `CipherBank-app.Tests/**` | CI gate without device |

Analyzer note: dense StyleCop IDs stay on a shrinking `WarningsNotAsErrors` allowlist under `TreatWarningsAsErrors`. `NU1608` / `NU1605` / `NU1107` are elevated with `WarningsAsErrors` in `Directory.Build.props` (never demoted) so graph mismatch fails the build.

---

## M2 — ChallengePass

**Lives:** `CipherBank-app.ChallengePass/`, tests `CipherBank-app.Tests/ChallengePass/`, solution entry in `CipherBank-app.sln`.

| Area | Path | Connects to |
|------|------|-------------|
| Session proof builder / slots / catalog | `ChallengePass/*.cs` | Shell `MauiProgram` DI |
| Algorithms / templates / structures | `Algorithms/`, `Templates/`, `Structures/` | HTTP session challenge clients |
| Hybrid PQ channel (ML-KEM + X25519) | `Hybrid/*` | Shell `HttpPq*` services |
| Custody-backed account key source | `CustodyAccountKeySource.cs` | Core custody after unlock |

---

## M3 — MAUI Shell

**Lives:** `CipherBank-app/**`, `AGENTS.md`, `docs/MAUI_FUNCTION_REF.md`, `docs/CB_MauiFunctionRef.html`.

| Area | Path | Connects to |
|------|------|-------------|
| DI / composition | `MauiProgram.cs` | Core + ChallengePass + platform services |
| Onboarding / custody UI | `Views/{Welcome,Keys,BackupQuiz,SetPin,Restore,Unlock,ChangePin}*` | Core custody + E2E account wave |
| Home / Convert / Pay / Receive / Profile | `Views/*`, `ViewModels/*` | Core persist + V1 + wallets |
| Cora chrome | `Controls/Cora*`, fonts, `Resources/Styles` | Visual Shell |
| Android identity | `ApplicationId=com.companyname.cipherbankapp` | E2E wipe / install |
| Secure store / biometrics / backup files | `Services/MauiSecureStore`, `BiometricService`, `BackupFileService` | Core `ISecureStore` / quiz export |
| Session / product HTTP | `HttpProductApi`, `HttpSessionChallengeClient`, `HttpPq*` | ChallengePass + Core V1 |

Expo is **not** shipped here and is not required to build or run MAUI.

---

## M4 — Appium E2E + harness

**Lives:** `CipherBank-app.E2ETests/`, `scripts/e2e-android.sh`, `scripts/lib/android-env.sh`, `docs/tests/*`.

### Harness

| Piece | Path | Role |
|-------|------|------|
| Runner script | `scripts/e2e-android.sh` | Boot AVD, build/install APK, Appium, filter (`--story`, `--wave account`, `--all`) |
| Env | `scripts/lib/android-env.sh` | `ANDROID_HOME`, `JAVA_HOME` (default `$HOME/.local/jdk-17`), `DOTNET` path |
| Fixture / reset / profiles | `E2ETests/Support/{AppiumFixture,EmulatorReset,DeviceState,Adb}.cs` | Fresh / Sealed setup |
| Journal | `Support/StoryJournal.cs` | In-memory + on-disk PIN/mnemonic/steps |
| Gaps | `Support/GapNotes.cs` → `docs/tests/gaps/` | Failures drive next feature work |
| Catalog / procedures | `Stories/{StoryIds,StoryCatalog,StoryProcedures,AutomationIdMap}.cs` | Scaffold `CB-*` / `US-*` |
| Page objects | `PageObjects/*` | Map to Shell `AutomationId`s |
| Account Facts | `Tests/AccountStories.cs` | Wave 0–1 proven on emulator |
| Runbook | `CipherBank-app.E2ETests/README.md`, `docs/tests/e2e-tests.md`, `STORY_ID_MAP.md` | How to run / story map |

### Wave status (account)

Proven under `E2E_RUN=1` / `--wave account` (filter includes `CB_ACCOUNT|US_ONB_03|US_ONB_04`):

- CB-ACCOUNT-001, US-ONB-03, US-ONB-04, CB-ACCOUNT-PIN-CHANGE, CB-ACCOUNT-002

Later waves (market, wallets, fund, pay, cards) remain vertical-slice work: AutomationIds → page objects → Fact → gap notes on fail — **no soft-pass**.

### Policy (locked)

- Stories fail → gap notes, not greenwash.
- Dynamic PIN: journaled PIN + change-PIN flow (not a forever fixed production PIN).
- Dev/test journaling of PINs/mnemonics on emulators is allowed.
- Coding standards: purpose + **Use High|Medium|Low** + **Scope**; dedicated in-memory owners; background process objects for boot/Appium/install; max **2** loop layers; prefer ternary / dictionary dispatch (`AGENTS.md`).

---

## Docs kept vs removed

**Kept (operational):** `docs/README.md`, `architecture.md`, `app/`, `core/`, `config/`, `tests/`, function refs, this `BUILD_LOG.md`, `AGENTS.md`.

**Historical plans retained** under `docs/superpowers/plans/` (Stage 1 mechanical / Phase 4 ratchet notes) for audit; operational truth lives in this BUILD_LOG + `AGENTS.md`. Spent SDD/spec trees and Expo handoff remain out of the MAUI merge path.

---

## Local verify (quick)

```bash
source scripts/lib/android-env.sh
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj
dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-android \
  -p:AndroidSdkDirectory="$ANDROID_HOME" -p:JavaSdkDirectory="$JAVA_HOME"
./scripts/e2e-android.sh --all              # M1b+ scaffold (device + Appium)
./scripts/e2e-android.sh --wave account     # M4-only: Story-trait Facts on prototype/maui-m4
```
