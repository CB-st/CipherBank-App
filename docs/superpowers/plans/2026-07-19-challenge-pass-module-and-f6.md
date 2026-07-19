# Challenge Pass Module + F6 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use `superpowers:subagent-driven-development` (recommended) or `superpowers:executing-plans` to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Finish custody/HTTP wiring for the slotted challenge/pass module, then complete F6 parity hardening and PR #15 update.

**Architecture:** `CipherBank-app.ChallengePass` owns three swappable slots (algorithm, template, structure) composed into named suites via `IChallengePassCatalog`. App keeps `LabSessionProofBuilder` as default `ISessionProofBuilder` until custody key source + HTTP challenge client are ready.

**Tech Stack:** .NET 10 MAUI, NSec.Cryptography, xUnit, existing `/v1` product API patterns.

## Global Constraints

- Mnemonic / seed / PIN never on the wire.
- Default session opener remains Lab until explicit flag/DI swap.
- Managed XMR must not store spend keys.
- Unit suite + coverage thresholds must stay green.

---

### Task 1: Custody-backed IAccountKeySource (done when unlock derives account key)

**Files:**
- Create: `CipherBank-app/Services/CustodyAccountKeySource.cs`
- Modify: `CipherBank-app/MauiProgram.cs` (replace `LockedAccountKeySource`)
- Modify: `CipherBank-app.Core/Custody/ICustodyService.cs` if entropy export helper needed
- Test: `CipherBank-app.Tests/ChallengePass/CustodyAccountKeySourceTests.cs`

**Interfaces:**
- Consumes: `ICustodyService.ExportMnemonic()` / BIP39 entropy, `AccountKeyDerivation.DeriveAccountKey`
- Produces: `IAccountKeySource.RequireUnlockedKeyPair` while unlocked; throws when locked

- [ ] **Step 1: Failing test** — unlocked custody → stable pubkey; locked → throws
- [ ] **Step 2: Implement CustodyAccountKeySource**
- [ ] **Step 3: DI swap Locked → Custody**
- [ ] **Step 4: Tests green + commit**

```bash
git commit -m "feat: custody-backed account key source for challenge/pass"
```

---

### Task 2: HTTP session challenge client

**Files:**
- Create: `CipherBank-app/Services/HttpSessionChallengeClient.cs`
- Modify: `CipherBank-app.Core/V1/IProductApi.cs` — `CreateSessionChallengeAsync`
- Modify: `MockProductApi` — use `InMemorySessionChallengeClient` logic or seal with lab API key
- Modify: `MauiProgram.cs` — mock vs HTTP client selection
- Test: mock challenge + pass verify

- [ ] **Step 1: API method + mock issuer**
- [ ] **Step 2: HttpSessionChallengeClient POST v1/session/challenge**
- [ ] **Step 3: Tests + commit**

```bash
git commit -m "feat: HTTP and mock session challenge client"
```

---

### Task 3: Settings flag to bind ChallengePassSessionProofBuilder

**Files:**
- Modify: settings / prefs for `SessionProofMode`
- Modify: `MauiProgram.cs` DI factory for `ISessionProofBuilder`
- Test: lab default; ChallengePass when flag set (mock)

- [ ] **Step 1–4: flag + DI + test + commit**

```bash
git commit -m "feat: SessionProofMode flag for challenge/pass cutover"
```

---

### Task 4: F6.1 XMR managed wallet API

**Files:**
- Modify: `IProductApi`, `MockProductApi`, `HttpProductApi`, `AddWalletViewModel`
- Test: managed create upserts local row; no spend key in payload

- [ ] **Step 1–4: TDD + commit** `feat: minimal XMR managed wallet wiring`

---

### Task 5: F6.2 E2E AutomationIds + smoke

**Files:**
- Modify: `HomePage.xaml`, `SendPage.xaml`, E2E page objects + `CoraShellSmokeTests.cs`

- [ ] **Step 1–3: IDs + assertions + commit** `test: expand Cora shell E2E for parity surfaces`

---

### Task 6: F6.3 PR + ledger

- [x] Update `.superpowers/sdd/progress.md`
- [x] `gh pr edit 15` with F0–F6 checklist
- [x] Push `feat/cora-maui-port`

---

## Already complete (this session)

- [x] `CipherBank-app.ChallengePass` assembly with 3 slots + catalog
- [x] A1 X25519-ChaCha algorithm, SHA256 template, two-step structure
- [x] In-memory challenge client + module unit tests
- [x] Spec updated for modular install
- [x] Maui DI: `AddChallengePassModule` (Lab still default opener)
