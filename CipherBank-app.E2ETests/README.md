# CipherBank MAUI Appium E2E

End-to-end tests for the **shipping MAUI Shell** (`CipherBank-app`) driven by Appium + UiAutomator2 on Android.

This README is the operator runbook: **emulator boot → build/install → Appium → tests**.  
Story IDs and ownership: [`docs/tests/STORY_ID_MAP.md`](../docs/tests/STORY_ID_MAP.md). Coding standards: repo-root [`AGENTS.md`](../AGENTS.md) plus this project's [`AGENTS.md`](AGENTS.md).

---

## One-command path (recommended)

From the **repo root**:

```bash
./scripts/e2e-android.sh --wave account
```

That script, in order:

1. Loads Android / .NET / Node paths (`scripts/lib/android-env.sh`)
2. Starts AVD `CipherBank_API34` if no emulator is already attached
3. Waits until `sys.boot_completed=1`
4. Builds the MAUI app (`net10.0-android`, Debug, `EmbedAssembliesIntoApk=true`)
5. Installs the signed APK (`adb install -r`)
6. Starts Appium on `:4723` if it is not already up
7. Runs `dotnet test` on this project with `E2E_RUN=1`

Other entry points:

```bash
./scripts/e2e-android.sh --help
./scripts/e2e-android.sh --story CB-ACCOUNT-001
./scripts/e2e-android.sh --wave account    # CB-ACCOUNT-001, US-ONB-03/04, PIN-CHANGE, CB-ACCOUNT-002
./scripts/e2e-android.sh --all             # full E2E assembly (see caveats below)
```

---

## Prerequisites (first-time machine setup)

| Tool | Expected location / notes |
|------|---------------------------|
| .NET 10 SDK | `$HOME/.local/dotnet` (or set `DOTNET_ROOT`) |
| Android SDK | `$HOME/Android/Sdk` (or set `ANDROID_HOME`) |
| AVD | `CipherBank_API34` — `emulator -list-avds` must show it |
| JDK 17 | default `$HOME/.local/jdk-17` via `JAVA_HOME` |
| Node / npx | for `npx appium`; optional `$HOME/.local/nodejs` if system npm is broken |
| Appium UiAutomator2 | installed on first `npx --yes appium` run (or preinstall: `npm i -g appium && appium driver install uiautomator2`) |

Quick sanity check:

```bash
source scripts/lib/android-env.sh
dotnet --version
adb version
emulator -list-avds          # expect CipherBank_API34
which npx
```

MAUI package id (wipe / clear): **`com.companyname.cipherbankapp`**  
(Do **not** use Expo’s `com.cipherbank.app`.)

```bash
adb shell pm clear com.companyname.cipherbankapp
```

---

## Manual walkthrough (same steps as the harness)

Use this when debugging a single phase. Otherwise prefer `./scripts/e2e-android.sh`.

### 1. Environment

```bash
cd /path/to/CipherBank-App
source scripts/lib/android-env.sh
```

### 2. Boot the emulator

Skip if `adb devices` already lists an `emulator-**** device`.

```bash
emulator -avd CipherBank_API34 -netdelay none -netspeed full -gpu auto &
adb wait-for-device
# wait until:
adb shell getprop sys.boot_completed   # prints 1
```

### 3. Build and install the MAUI APK

```bash
dotnet build CipherBank-app/CipherBank-app.csproj \
  -f net10.0-android -c Debug -p:EmbedAssembliesIntoApk=true

# Typical output path (Signed.apk preferred):
APK="$(find CipherBank-app/bin/Debug/net10.0-android -maxdepth 1 -iname '*-Signed.apk' | head -1)"
# Resolve to an absolute path before exporting — test cwd is the test bin dir, not the repo root.
APK="$(pwd)/$APK"
adb install -r "$APK"
```

### 4. Start Appium

```bash
# status check
curl -fsS http://localhost:4723/status && echo OK

# if not running:
npx --yes appium --port 4723
# leave it running in another terminal (UiAutomator2 driver required)
```

### 5. Run tests

**Without `E2E_RUN=1`**, device Facts are **Skipped** (not silent passes).

```bash
# Inventory only
dotnet test CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj --list-tests

# Configure synthetic lab credentials first, either in the shell or by copying
# docs/tests/e2e-local.env.example to artifacts/e2e-local.env and filling it.
# Account wave (five Facts); the harness performs the same trait-filter preflight.
E2E_RUN=1 TEST_PLATFORM=android \
  ANDROID_APK_PATH="$APK" \
  E2E_JOURNAL_DIR=artifacts/e2e-journal \
  dotnet test CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj --nologo \
  --filter "Story=CB-ACCOUNT-001|Story=CB-ACCOUNT-002|Story=CB-ACCOUNT-PIN-CHANGE|Story=US-ONB-03|Story=US-ONB-04"

# Single story by stable trait
E2E_RUN=1 TEST_PLATFORM=android ANDROID_APK_PATH="$APK" \
  dotnet test CipherBank-app.E2ETests/CipherBank-app.E2ETests.csproj \
  --filter "Story=CB-ACCOUNT-001"
```

---

## What each account Fact needs

| Story | Device profile | Notes |
|-------|----------------|-------|
| CB-ACCOUNT-001 | Fresh (`pm clear`) | Welcome → Keys → Quiz → SetPin → Home |
| US-ONB-03 | Fresh | Wrong quiz words → error, stay on quiz |
| US-ONB-04 | Fresh | PIN mismatch → error, stay on SetPin |
| CB-ACCOUNT-PIN-CHANGE | Sealed | Change PIN; old PIN rejected; new PIN unlocks |
| CB-ACCOUNT-002 | Sealed → wipe → restore | Real recovery file + Android document picker |

Harness credentials (PIN / alt PIN / recovery password) are **synthetic lab values for emulator diagnosis only**. They are not product secrets. Configure them via env vars or a gitignored `artifacts/e2e-local.env` (copy from `docs/tests/e2e-local.env.example`). Suggested lab values and the full contract: [`docs/tests/e2e-tests.md`](../docs/tests/e2e-tests.md) § Harness credentials.

---

## Artifacts & logs

| Path | Contents |
|------|----------|
| `artifacts/e2e-journal/` | Per-story journal (PIN, mnemonic, steps) — **gitignored**; debug only |
| `artifacts/e2e-local.env` | Lab harness credentials — **gitignored** (copy from `docs/tests/e2e-local.env.example`) |
| `artifacts/e2e-recovery/` | Host copy of exported recovery file — gitignored |
| `artifacts/e2e-diagnostics/` | Page-source dumps on unexpected screens — gitignored |
| `/tmp/cb-e2e-appium.log` | Appium server log (harness) |
| `/tmp/cb-e2e-emulator.log` | Emulator stdout/stderr (harness cold start) |
| `docs/tests/gaps/CB-*.md` | Gap notes when a story fails under `E2E_RUN=1` |

---

## Project layout

```
CipherBank-app.E2ETests/
├── README.md                 ← this file
├── Support/                  ← AppiumFixture, DeviceState, StoryJournal, GapNotes, …
├── Stories/                  ← StoryIds, StoryCatalog, StoryProcedures, AutomationIdMap
├── PageObjects/              ← Welcome, Keys, Quiz, SetPin, Unlock, Profile, Restore, …
└── Tests/
    ├── AccountStories.cs     ← Wave 0–1 executable CB-ACCOUNT / US-ONB Facts
    ├── CoraShellSmokeTests.cs
    └── StoryBacklogTests.cs  ← remaining CB-* inventory (skipped Theories)
```

---

## Troubleshooting

| Symptom | Check |
|---------|--------|
| All Facts Skipped | `E2E_RUN` must be `1` |
| `no APK found` | Build with `EmbedAssembliesIntoApk=true`; inspect `CipherBank-app/bin/Debug/net10.0-android/` |
| Appium never ready | `/tmp/cb-e2e-appium.log`; install `uiautomator2` driver; fix broken system `npx` via `CB_NODE_HOME` |
| Emulator never boots | `/tmp/cb-e2e-emulator.log`; `emulator -list-avds`; KVM / GPU flags |
| Wrong package wiped | Use `com.companyname.cipherbankapp`, not Expo’s id |
| `--all` flakes on smoke | Smoke Facts expect a sealed wallet; account Facts call `pm clear`. Prefer `--wave account` or run smoke after a sealed setup |

---

## Related docs

- [`docs/tests/e2e-tests.md`](../docs/tests/e2e-tests.md) — AutomationId tables, env vars  
- [`docs/tests/STORY_ID_MAP.md`](../docs/tests/STORY_ID_MAP.md) — Executable vs backlog stories  
- [`docs/BUILD_LOG.md`](../docs/BUILD_LOG.md) — condensed design + layer map  
- [`scripts/e2e-android.sh`](../scripts/e2e-android.sh) — harness source  
