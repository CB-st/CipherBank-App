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
scripts/e2e-android.sh ──► AVD + Appium (full Story Facts on M4)
         │
         ▼
CipherBank-app.E2ETests (scaffold here; account wave on M4)
         │
         ▼
CipherBank-app (Shell) ──► Core + ChallengePass
```

**Package wipe (MAUI):** `adb shell pm clear com.companyname.cipherbankapp`  
**E2E harness credentials (M4 Story Facts):** env or gitignored `artifacts/e2e-local.env` — template: [`docs/tests/e2e-local.env.example`](tests/e2e-local.env.example).

---

## M1 — Core foundations

**Lives:** `CipherBank-app.Core/`, matching tests under `CipherBank-app.Tests/` (excluding ChallengePass).

| Area | Path | Connects to |
|------|------|-------------|
| Custody (PIN, mnemonic, step-up, crypto box) | `Core/Custody/*` | Shell unlock / onboarding / backup |
| Session | `Core/Session/AppSession.cs` | Shell idle lock + unlock |
| Local persist / market / wallets DB | `Core/Persist/*` | Home, Convert, recipients |
| Product V1 mock/API contracts | `Core/V1/*` | Shell HTTP product API + streams |
| Encrypted userdata pack (design + Core crypto + wire + prefs sync) | [`docs/USER_DATA_ENCRYPTION.md`](USER_DATA_ENCRYPTION.md), `Core/UserData/*` | Suites + client wire (53809) + `UserDataPrefsSyncService` dual-write; Shell: `AddUserDataPrefsSync()` on M3 |
| Wallets / QR / address derive | `Core/Wallets/*` | Receive, Send, AddWallet |
| Charts / Cora lines / POS NFC payload | `Core/Charts`, `Core/Cora`, `Core/Pos` | Shell charts + PosLab |
| Unit tests | `CipherBank-app.Tests/**` | CI gate without device |

Analyzer note: Core demotes dense StyleCop / Nethereum `NU1608` via `WarningsNotAsErrors` so net10 builds against main’s `TreatWarningsAsErrors`.

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

**Lives:** `CipherBank-app/**`, `AGENTS.md`, `docs/MAUI_FUNCTION_REF.md`.

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

## M4 — Appium E2E + harness (ships on `prototype/maui-m4`)

**Not present in the M1b docs slice.** Full account-wave Facts, `STORY_ID_MAP.md`,
`AppiumFixture` / `StoryJournal` / `GapNotes`, and the trait-bearing Stories tree
land on M4. This branch carries only the early harness skeleton
(`scripts/e2e-android.sh`, `scripts/lib/android-env.sh`) plus
`docs/tests/e2e-local.env.example` so operators can prepare credentials before M4.

Until `[Trait("Story", …)]` Facts exist under `CipherBank-app.E2ETests`, the harness
defers `--story` / `--wave` and does not require PIN env vars for `--all`.

See `prototype/maui-m4` / PR #23 for the live E2E map and wave status.

---

## Docs kept vs removed

**Kept (operational):** `docs/README.md`, `architecture.md`, `app/`, `core/`, `config/`, `tests/`, `MAUI_FUNCTION_REF.md`, `USER_DATA_ENCRYPTION.md` (design), this `BUILD_LOG.md`, `AGENTS.md`, Sonar/lint ops docs (`SONAR_GATE.md`, `SONAR_STRUCTURAL_PLAN.md`, `LOCAL_LINT.md`, `LOCAL_SONAR_LINT.md`).

**Removed when implemented:** spent `docs/superpowers/plans|specs` trees, generated `CB_MauiFunctionRef.html` twin, and other one-shot planning notes once their checklist landed in code. Operational truth stays in this BUILD_LOG + `AGENTS.md`. Expo handoff remains out of the MAUI merge path.

---

## Local verify (quick)

```bash
source scripts/lib/android-env.sh   # after M4
dotnet test CipherBank-app.Tests/CipherBank-app.Tests.csproj
dotnet build CipherBank-app/CipherBank-app.csproj -f net10.0-android \
  -p:AndroidSdkDirectory="$ANDROID_HOME" -p:JavaSdkDirectory="$JAVA_HOME"
./scripts/e2e-android.sh --wave account   # device + Appium required
```
