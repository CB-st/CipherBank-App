#!/usr/bin/env node
/**
 * Generates CB_MauiFunctionRef.html (CB_FullAPIRef visual style) for on-device
 * MAUI / Core / ChallengePass INVOKEs. Writes repo root + docs/.
 *
 * Run: node docs/scripts/generate-maui-function-ref.mjs
 *
 * Audited against feat/cora-redesign-maui (PR #16): splash, public quotes,
 * CompleteUnlock breakout, full IProductApi money/POS, ChallengePass builder,
 * idle lock PQ clear, wire field names ACCESS_TOKEN / TOTAL_USD.
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.join(__dirname, '../..');
const outRoot = path.join(repoRoot, 'CB_MauiFunctionRef.html');
const outDocs = path.join(repoRoot, 'docs/CB_MauiFunctionRef.html');
const cssSource = path.join(
  repoRoot,
  'design_handoff_cipherbank/starter/docs/CB_InitialAPIRef.html',
);

/** @typedef {{ name: string, type: string, desc: string }} Field */
/** @typedef {{
 *  id: string, method: string, path: string, host: string, summary: string,
 *  requestFields?: Field[], requestExample?: string, requestEmpty?: boolean,
 *  responseFields?: Field[], responseExample?: string, responseEmpty?: boolean,
 *  responseNote?: string,
 *  statuses: { code: string, behavior: string }[],
 *  section?: string,
 *  logic?: string,
 * }} Invoke */

/** @type {Invoke[]} */
const invokables = [
  {
    id: "shellBootstrap",
    method: "INVOKE",
    path: "AppShell.BootstrapAsync",
    host: "CipherBank-app · Shell",
    section: "Boot & shell",
    summary: "Cold-start: show Splash (≥900ms), init SQLite + AppSession, start idle lock, route Welcome or Unlock.",
    requestEmpty: true,
    requestExample: "{}",
    responseFields: [
      { name: "route", type: "string", desc: "//UnlockPage if HasWallet else //WelcomePage (Welcome on error)." },
    ],
    responseExample: "{\n  \"route\": \"//UnlockPage\",\n  \"hasWallet\": true,\n  \"minSplashMs\": 900\n}",
    logic: "GoTo Splash → parallel BootSessionAsync(db.InitializeAsync + session.BootAsync) with MinSplashDuration(900ms) → idleLock.Start → GoTo Unlock|Welcome. Catch → Welcome.",
    statuses: [
      { code: "ok", behavior: "Past splash to Welcome or Unlock." },
      { code: "catch", behavior: "Any boot exception → //WelcomePage." },
    ],
  },
  {
    id: "splashSetStatus",
    method: "INVOKE",
    path: "SplashPage.SetStatus",
    host: "CipherBank-app · Views",
    section: "Boot & shell",
    summary: "Optional status caption on Expo-parity ink splash while shell boots.",
    requestFields: [
      { name: "label", type: "string", desc: "Status line text." },
    ],
    requestExample: "{ \"label\": \"Opening session…\" }",
    responseEmpty: true,
    responseExample: "{}",
    statuses: [
      { code: "ok", behavior: "Label updated on main thread." },
    ],
  },
  {
    id: "sessionBoot",
    method: "INVOKE",
    path: "AppSession.BootAsync",
    host: "CipherBank-app.Core · Session",
    section: "Boot & shell",
    summary: "Sets HasWallet from sealed custody blob; loads IdleMs from UserPrefs.LockIdleSeconds.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{\n  \"hasWallet\": true,\n  \"idleMs\": 120000\n}",
    logic: "HasWallet = HasSealedWalletAsync; IdleMs = LockIdleSeconds*1000 or DefaultIdleMs(60000). IsBooting true during work.",
    statuses: [
      { code: "ok", behavior: "Boot flags ready for shell routing." },
    ],
  },
  {
    id: "idleStart",
    method: "INVOKE",
    path: "AppIdleLockService.Start",
    host: "CipherBank-app · Services",
    section: "Boot & shell",
    summary: "5s dispatcher timer polls CheckIdleAndMaybeLock; Locked event clears PQ channel and navigates Unlock.",
    requestEmpty: true,
    requestExample: "{}",
    responseEmpty: true,
    responseExample: "{}",
    logic: "CreateTimer(5s) → CheckIdleAndMaybeLock. OnLocked: IPqChannel.Clear → GoToAsync(Unlock). Touch() forwards to AppSession.Touch.",
    statuses: [
      { code: "ok", behavior: "Idle watch running." },
    ],
  },
  {
    id: "sessionTouch",
    method: "INVOKE",
    path: "AppSession.Touch",
    host: "CipherBank-app.Core · Session",
    section: "Boot & shell",
    summary: "Resets idle clock (_lastTouch = UtcNow). Called from tab Appearing/actions and idle service.",
    requestEmpty: true,
    requestExample: "{}",
    responseEmpty: true,
    responseExample: "{}",
    statuses: [
      { code: "ok", behavior: "Idle window restarted." },
    ],
  },
  {
    id: "sessionCheckIdle",
    method: "INVOKE",
    path: "AppSession.CheckIdleAndMaybeLock",
    host: "CipherBank-app.Core · Session",
    section: "Boot & shell",
    summary: "If unlocked and idle ≥ IdleMs → Lock(); returns whether lock was applied.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"locked\": true }",
    statuses: [
      { code: "true", behavior: "Idle exceeded; Lock invoked." },
      { code: "false", behavior: "Still within idle window or already locked." },
    ],
  },
  {
    id: "pinSet",
    method: "INVOKE",
    path: "PinService.SetPinAsync",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "PBKDF2-SHA256 (120k) hash + salt to secure storage; clear lockout. PIN plaintext never persisted.",
    requestFields: [
      { name: "pin", type: "string", desc: "Wallet PIN (≥6 in UI). Never on wire." },
    ],
    requestExample: "{ \"pin\": \"••••••\" }",
    responseEmpty: true,
    responseExample: "{}",
    statuses: [
      { code: "ok", behavior: "cb_pin_hash / cb_pin_salt written; fails cleared." },
    ],
  },
  {
    id: "pinVerify",
    method: "INVOKE",
    path: "PinService.VerifyPinAsync",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "RefreshAsync then fixed-time hash compare; 5 fails → 5 min lockout.",
    requestFields: [
      { name: "pin", type: "string", desc: "Candidate PIN." },
    ],
    requestExample: "{ \"pin\": \"••••••\" }",
    responseFields: [
      { name: "ok", type: "boolean", desc: "True when hash matches and not locked out." },
    ],
    responseExample: "{ \"ok\": true }",
    statuses: [
      { code: "true", behavior: "PIN accepted; fail counters cleared." },
      { code: "false", behavior: "Mismatch or lockout active." },
    ],
  },
  {
    id: "pinRefresh",
    method: "INVOKE",
    path: "PinService.RefreshAsync",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "Reload fail/lockout counters from secure storage (Unlock appear / before verify).",
    requestEmpty: true,
    requestExample: "{}",
    responseEmpty: true,
    responseExample: "{}",
    statuses: [
      { code: "ok", behavior: "In-memory lockout matches store." },
    ],
  },
  {
    id: "pinHas",
    method: "INVOKE",
    path: "PinService.HasPinAsync",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "True when cb_pin_hash is present in secure storage.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"hasPin\": true }",
    statuses: [
      { code: "true/false", behavior: "Hash key presence." },
    ],
  },
  {
    id: "cryptoBox",
    method: "INVOKE",
    path: "CryptoBox.Seal | Open",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "AES-GCM with PBKDF2-SHA256 (210k) key from passphrase (device secret or legacy PIN).",
    requestFields: [
      { name: "plaintext", type: "string", desc: "Mnemonic (Seal) or sealed base64 (Open)." },
      { name: "passphrase", type: "string", desc: "Device secret (preferred) or legacy PIN." },
    ],
    requestExample: "{ \"passphrase\": \"<device-secret>\", \"plaintext\": \"<mnemonic>\" }",
    responseExample: "{ \"sealedB64\": \"…\" }",
    logic: "DeriveKey → AES-GCM pack salt|nonce|tag|cipher (Seal). Open unpacks and zeros key material.",
    statuses: [
      { code: "ok", behavior: "Sealed or opened." },
      { code: "throw", behavior: "Decrypt/auth failure." },
    ],
  },
  {
    id: "custodySeal",
    method: "INVOKE",
    path: "CustodyService.SealAsync",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "Normalize+Validate BIP39 → SetPin → random device secret → CryptoBox.Seal(mnemonic, deviceSecret) → store → RAM unlock 5m TTL.",
    requestFields: [
      { name: "mnemonic", type: "string", desc: "12-word BIP39. Never on wire." },
      { name: "pin", type: "string", desc: "Logical gate only; AES key is device secret." },
    ],
    requestExample: "{\n  \"mnemonic\": \"<12 words>\",\n  \"pin\": \"••••••\"\n}",
    responseExample: "{\n  \"sealed\": true,\n  \"sessionTtlMinutes\": 5\n}",
    logic: "Order: Normalize → Validate → SetPinAsync → CreateDeviceSecret(32B) → Seal(mnemonic, secret) → store cb_device_secret_v1 + cb_custody_blob → _mnemonic + _expires.",
    statuses: [
      { code: "ok", behavior: "Sealed at rest; unlocked in RAM." },
      { code: "throw", behavior: "Invalid mnemonic." },
    ],
  },
  {
    id: "custodyUnlock",
    method: "INVOKE",
    path: "CustodyService.UnlockAsync",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "VerifyPin → Open blob with device secret, or legacy Open(pin) then MigrateToDeviceSecret.",
    requestFields: [
      { name: "pin", type: "string", desc: "Wallet PIN (gate)." },
    ],
    requestExample: "{ \"pin\": \"••••••\" }",
    responseExample: "{ \"ok\": true }",
    logic: "VerifyPinAsync → load blob → if device secret: Open(blob, secret); else Open(blob, pin) + MigrateToDeviceSecretAsync → 5m TTL.",
    statuses: [
      { code: "true", behavior: "Mnemonic in RAM." },
      { code: "false", behavior: "Bad PIN, missing blob, or decrypt fail." },
    ],
  },
  {
    id: "custodyUnlockDevice",
    method: "INVOKE",
    path: "CustodyService.UnlockWithDeviceSecretAsync",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "Open sealed blob with stored device secret (after OS biometrics). No PIN.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"ok\": true }",
    statuses: [
      { code: "true", behavior: "Unlocked 5m TTL." },
      { code: "false", behavior: "Missing secret/blob or decrypt fail." },
    ],
  },
  {
    id: "custodyIsUnlocked",
    method: "INVOKE",
    path: "CustodyService.IsUnlocked",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "If mnemonic present but TTL expired, Lock() wipes RAM and returns false.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"isUnlocked\": false }",
    statuses: [
      { code: "true", behavior: "Mnemonic + valid TTL." },
      { code: "false", behavior: "Locked or auto-wiped on TTL." },
    ],
  },
  {
    id: "custodyLock",
    method: "INVOKE",
    path: "CustodyService.Lock",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "Clear in-memory mnemonic and expiry. Blob stays sealed at rest.",
    requestEmpty: true,
    requestExample: "{}",
    responseEmpty: true,
    responseExample: "{}",
    statuses: [
      { code: "ok", behavior: "RAM wiped." },
    ],
  },
  {
    id: "custodyExport",
    method: "INVOKE",
    path: "CustodyService.ExportMnemonic",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "Returns RAM mnemonic iff IsUnlocked; else null. UI gates with step-up RevealKeys.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"mnemonic\": \"<12 words or null>\" }",
    statuses: [
      { code: "string", behavior: "Unlocked export." },
      { code: "null", behavior: "Locked." },
    ],
  },
  {
    id: "custodyHasSealed",
    method: "INVOKE",
    path: "CustodyService.HasSealedWalletAsync",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "True when cb_custody_blob exists (drives Boot HasWallet / Welcome Returning).",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"hasSealed\": true }",
    statuses: [
      { code: "true/false", behavior: "Blob presence." },
    ],
  },
  {
    id: "mnemonicGenerate",
    method: "INVOKE",
    path: "MnemonicHelper.Generate | Validate | Entropy",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "BIP39 12-word English generate/validate; Entropy recovers bytes for account-key HKDF.",
    requestExample: "{ \"mnemonic\": \"<12 words>\" }",
    responseExample: "{ \"valid\": true, \"entropyBytes\": 16 }",
    logic: "Generate uses NBitcoin. Entropy never leaves device. KeysViewModel ctor calls Generate.",
    statuses: [
      { code: "ok", behavior: "Phrase or entropy ready." },
    ],
  },
  {
    id: "backupQuiz",
    method: "INVOKE",
    path: "BackupQuiz.PickRandom",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "Fisher–Yates pick of distinct word indices for onboarding quiz (typically 3).",
    requestFields: [
      { name: "words", type: "string[]", desc: "Mnemonic words." },
      { name: "count", type: "int", desc: "Quiz size." },
    ],
    requestExample: "{ \"count\": 3 }",
    responseExample: "{ \"indices\": [2, 7, 11] }",
    statuses: [
      { code: "ok", behavior: "Sorted distinct indices." },
    ],
  },
  {
    id: "stepUpRequire",
    method: "INVOKE",
    path: "StepUpAuthService.RequireAsync",
    host: "CipherBank-app.Core · Custody",
    section: "Custody & PIN",
    summary: "Prefer biometrics via IStepUpChallenges; else PIN prompt → PinService.VerifyPinAsync.",
    requestFields: [
      { name: "reason", type: "AuthReason", desc: "Payment | Convert | PosAuthorize | PosPresent | RevealKeys | Derive." },
    ],
    requestExample: "{ \"reason\": \"Convert\" }",
    responseExample: "{ \"ok\": true }",
    logic: "MauiStepUpChallenges.TryBiometricsAsync first; on fail PromptForPinAsync → VerifyPinAsync.",
    statuses: [
      { code: "true", behavior: "Step-up satisfied." },
      { code: "false", behavior: "User cancel or bad PIN." },
    ],
  },
  {
    id: "sessionCompleteUnlock",
    method: "INVOKE",
    path: "AppSession.CompleteUnlockAsync",
    host: "CipherBank-app.Core · Session (private)",
    section: "Session",
    summary: "Shared unlock orchestrator after custody is open. Callers: Unlock*, FinishCustodySetup.",
    requestFields: [
      { name: "applyBootstrap", type: "bool", desc: "true on Unlock paths; false on SetPin seal path." },
    ],
    requestExample: "{ \"applyBootstrap\": true }",
    responseExample: "{ \"ok\": true, \"accessToken\": \"<ACCESS_TOKEN>\" }",
    logic: "REQUIRED: CreateSessionAsync → AccessToken=session.AccessToken → stream.ConnectAsync → streamHub.Start. BEST-EFFORT try: prefsSync.PullMergeAsync; if applyBootstrap bootstrap.ApplyAsync; reload IdleMs from prefs. Catch swallows prefs/bootstrap errors. Touch(); return true.",
    statuses: [
      { code: "true", behavior: "Always true after CreateSession+stream succeed; prefs failures do not fail unlock." },
      { code: "throw", behavior: "CreateSession or Connect failure aborts." },
    ],
  },
  {
    id: "sessionFinishCustody",
    method: "INVOKE",
    path: "AppSession.FinishCustodySetupAsync",
    host: "CipherBank-app.Core · Session",
    section: "Session",
    summary: "Seal → seed BTC+ETH derived wallets → CompleteUnlock(applyBootstrap:false) → HasWallet=true.",
    requestFields: [
      { name: "mnemonic", type: "string", desc: "BIP39." },
      { name: "pin", type: "string", desc: "New PIN." },
    ],
    requestExample: "{ \"mnemonic\": \"<12 words>\", \"pin\": \"••••••\" }",
    responseExample: "{ \"hasWallet\": true }",
    logic: "Custody.SealAsync → LocalWalletSeeder.EnsureDerivedAsync(mnemonic) defaults BTC,ETH → CompleteUnlockAsync(false) → HasWallet=true.",
    statuses: [
      { code: "ok", behavior: "Ready for //HomePage." },
    ],
  },
  {
    id: "sessionUnlock",
    method: "INVOKE",
    path: "AppSession.UnlockAsync",
    host: "CipherBank-app.Core · Session",
    section: "Session",
    summary: "Custody.UnlockAsync(pin) then CompleteUnlockAsync(applyBootstrap:true).",
    requestFields: [
      { name: "pin", type: "string", desc: "Wallet PIN." },
    ],
    requestExample: "{ \"pin\": \"••••••\" }",
    responseExample: "{ \"ok\": true }",
    logic: "If custody unlock fails → false. Else CompleteUnlock (session+stream required; prefs+bootstrap best-effort).",
    statuses: [
      { code: "true", behavior: "Custody open + session/stream up." },
      { code: "false", behavior: "Custody unlock failed." },
    ],
  },
  {
    id: "sessionUnlockDevice",
    method: "INVOKE",
    path: "AppSession.UnlockWithDeviceOwnerAsync",
    host: "CipherBank-app.Core · Session",
    section: "Session",
    summary: "Custody.UnlockWithDeviceSecretAsync then CompleteUnlock(applyBootstrap:true).",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"ok\": true }",
    statuses: [
      { code: "true", behavior: "Biometric path unlocked." },
      { code: "false", behavior: "Device-secret unlock failed." },
    ],
  },
  {
    id: "sessionCanUnlockDevice",
    method: "INVOKE",
    path: "AppSession.CanUnlockWithDeviceOwnerAsync",
    host: "CipherBank-app.Core · Session",
    section: "Session",
    summary: "Delegates to Custody.CanUnlockWithDeviceOwnerAsync (device secret present).",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"ok\": true }",
    statuses: [
      { code: "true/false", behavior: "Whether biometrics unlock is available." },
    ],
  },
  {
    id: "sessionLock",
    method: "INVOKE",
    path: "AppSession.Lock",
    host: "CipherBank-app.Core · Session",
    section: "Session",
    summary: "Stop hub, custody Lock, clear AccessToken + product session store, DisconnectAsync stream, raise Locked.",
    requestEmpty: true,
    requestExample: "{}",
    responseEmpty: true,
    responseExample: "{}",
    logic: "Does NOT clear PQ channel here. AppIdleLockService.OnLocked clears IPqChannel and navigates Unlock.",
    statuses: [
      { code: "ok", behavior: "Locked event raised for idle service." },
    ],
  },
  {
    id: "proofLab",
    method: "INVOKE",
    path: "LabSessionProofBuilder.BuildOpenBodyAsync",
    host: "CipherBank-app.Core · V1",
    section: "Challenge / pass",
    summary: "Default Lab session open body (no crypto). SessionProofMode.Lab.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{\n  \"DEVICE_ATTESTATION\": \"lab\"\n}",
    statuses: [
      { code: "ok", behavior: "Stub for POST /session." },
    ],
  },
  {
    id: "proofBuilder",
    method: "INVOKE",
    path: "ChallengePassSessionProofBuilder.BuildOpenBodyAsync",
    host: "CipherBank-app.ChallengePass",
    section: "Challenge / pass",
    summary: "Active-suite entry: A2 hybrid identity + PQ structure, or A1 keypair + two-step structure.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ /* SessionPassDto SCREAMING fields */ }",
    logic: "catalog.GetActive(). If Structure is PqChannelChallengePassStructure: RequireHybridIdentity → SetDeviceIdentity → BuildSessionOpenBodyAsync. Else: RequireUnlockedKeyPair(algo) → wire pk → BuildSessionOpenBodyAsync.",
    statuses: [
      { code: "ok", behavior: "Proof body for CreateSessionAsync." },
      { code: "throw", behavior: "Custody locked / missing keys." },
    ],
  },
  {
    id: "proofA1",
    method: "INVOKE",
    path: "TwoStepChallengePassStructure.BuildSessionOpenBodyAsync",
    host: "CipherBank-app.ChallengePass · A1",
    section: "Challenge / pass",
    summary: "Suite a1-x25519-chacha-v1: challenge Open → SHA-256 pass → Seal to API pk.",
    requestFields: [
      { name: "accountKeyPair", type: "AccountKeyPair", desc: "From CustodyAccountKeySource (derived, not user-supplied)." },
    ],
    requestExample: "{ \"accountPublicKeyWire\": \"<from derivation>\" }",
    responseExample: "{\n  \"CHALLENGE_ID\": \"…\",\n  \"PASS_CIPHERTEXT\": \"…\",\n  \"ACCOUNT_PUBLIC_KEY\": \"…\",\n  \"ALGORITHM\": \"x25519-chacha20poly1305\"\n}",
    logic: "ISessionChallengeClient.RequestChallengeAsync(wirePk) → algo.Open(accountSk) → ChallengeIdNonceSha256Template.Parse/BuildPassPayload(SHA256 of plaintext) → algo.Seal to API pk → SessionPassDto. Private key never on wire.",
    statuses: [
      { code: "ok", behavior: "SessionPassDto body ready." },
    ],
  },
  {
    id: "proofA2",
    method: "INVOKE",
    path: "PqChannelChallengePassStructure.BuildSessionOpenBodyAsync",
    host: "CipherBank-app.ChallengePass · A2",
    section: "Challenge / pass",
    summary: "Suite a2-hybrid-pq-channel-v1: key-share if needed, then channel challenge/pass.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{\n  \"CHALLENGE_ID\": \"…\",\n  \"PASS_CIPHERTEXT\": \"…\",\n  \"ALGORITHM\": \"pq-channel-chacha20poly1305-v1\"\n}",
    logic: "If no channel: IPqKeyShareClient.EstablishAsync(deviceIdentity) → CompleteAsDevice → IPqChannel.SetChannelKey. Then IPqChannelChallengeSource.RequestChallengeAsync → channel Open → BuildPass → channel Seal. Keys from RequireHybridIdentity (mnemonic entropy), not free-form request.",
    statuses: [
      { code: "ok", behavior: "PQ SessionPassDto ready." },
    ],
  },
  {
    id: "hybridAgree",
    method: "INVOKE",
    path: "HybridMlKemX25519Agreement.DeriveIdentity | CompleteAsDevice",
    host: "CipherBank-app.ChallengePass · Hybrid",
    section: "Challenge / pass",
    summary: "ML-KEM-768 + X25519 → HKDF channel key. Ids: hybrid-mlkem768-x25519-v1.",
    requestExample: "{ \"entropy\": \"<from mnemonic>\" }",
    responseExample: "{ \"channelKeySet\": true }",
    logic: "DeriveIdentity(entropy) for device; CreateShareAsServer / CompleteAsDevice for encapsulate/decapsulate → channel key.",
    statuses: [
      { code: "ok", behavior: "Hybrid identity or channel key." },
    ],
  },
  {
    id: "pqChannel",
    method: "INVOKE",
    path: "PqSymmetricChannel.SetChannelKey | Seal | Open | Clear",
    host: "CipherBank-app.ChallengePass · Hybrid",
    section: "Challenge / pass",
    summary: "In-memory ChaCha20-Poly1305 channel. Cleared on idle lock (AppIdleLockService.OnLocked).",
    requestExample: "{ \"key\": \"<32-byte HKDF>\" }",
    responseExample: "{ \"cleared\": true }",
    logic: "SetChannelKey stores key; Seal/Open AEAD; Clear zeros. Never logged.",
    statuses: [
      { code: "ok", behavior: "Channel op." },
      { code: "throw", behavior: "No key / decrypt fail." },
    ],
  },
  {
    id: "accountKeySource",
    method: "INVOKE",
    path: "CustodyAccountKeySource.RequireUnlockedKeyPair | RequireHybridIdentity",
    host: "CipherBank-app.ChallengePass",
    section: "Challenge / pass",
    summary: "ExportMnemonic → Entropy → AccountKeyDerivation or HybridMlKemX25519Agreement.DeriveIdentity.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"publicKeyWire\": \"…\" }",
    statuses: [
      { code: "ok", behavior: "Keys for active suite." },
      { code: "throw", behavior: "Custody locked." },
    ],
  },
  {
    id: "httpChallengeClient",
    method: "INVOKE",
    path: "HttpSessionChallengeClient.RequestChallengeAsync",
    host: "CipherBank-app · Services",
    section: "Challenge / pass",
    summary: "A1 adapter: IProductApi.CreateSessionChallengeAsync(accountPublicKeyWire).",
    requestFields: [
      { name: "accountPublicKeyWire", type: "string", desc: "URL-safe base64 account pk." },
    ],
    requestExample: "{ \"ACCOUNT_PUBLIC_KEY\": \"…\" }",
    responseExample: "{ \"CHALLENGE_ID\": \"…\", \"CIPHERTEXT\": \"…\" }",
    statuses: [
      { code: "ok", behavior: "SessionChallengeDto." },
    ],
  },
  {
    id: "httpKeyShare",
    method: "INVOKE",
    path: "HttpPqKeyShareClient.EstablishAsync",
    host: "CipherBank-app · Services",
    section: "Challenge / pass",
    summary: "A2 adapter: IProductApi.EstablishKeyShareAsync with device public keys only.",
    requestExample: "{ \"MLKEM_PUBLIC_KEY\": \"…\", \"X25519_PUBLIC_KEY\": \"…\" }",
    responseExample: "{ \"KEY_SHARE_ID\": \"…\", \"MLKEM_CIPHERTEXT\": \"…\" }",
    statuses: [
      { code: "ok", behavior: "KeyShareResponseDto." },
    ],
  },
  {
    id: "apiCreateSession",
    method: "INVOKE",
    path: "IProductApi.CreateSessionAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "Http: BuildOpenBody via ISessionProofBuilder then POST /session. Mock: returns tokens with no proof.",
    requestExample: "{ /* Lab | A1 | A2 proof body */ }",
    responseFields: [
      { name: "ACCESS_TOKEN", type: "string", desc: "Bearer for product routes." },
      { name: "REFRESH_TOKEN", type: "string", desc: "Refresh token." },
      { name: "EXPIRES_AT", type: "number", desc: "Epoch ms expiry." },
    ],
    responseExample: "{\n  \"ACCESS_TOKEN\": \"…\",\n  \"REFRESH_TOKEN\": \"…\",\n  \"EXPIRES_AT\": 1720900000000\n}",
    logic: "HttpProductApi saves SessionDto via IProductSessionStore. Mock skips proof/HTTP.",
    statuses: [
      { code: "200", behavior: "Session stored." },
      { code: "throw", behavior: "Non-success or empty body (Http)." },
    ],
  },
  {
    id: "apiChallenge",
    method: "INVOKE",
    path: "IProductApi.CreateSessionChallengeAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "POST /session/challenge { ACCOUNT_PUBLIC_KEY } → sealed challenge for A1.",
    requestExample: "{ \"ACCOUNT_PUBLIC_KEY\": \"…\" }",
    responseExample: "{ \"CHALLENGE_ID\": \"…\", \"CIPHERTEXT\": \"…\", \"API_PUBLIC_KEY\": \"…\" }",
    statuses: [
      { code: "200", behavior: "SessionChallengeDto." },
    ],
  },
  {
    id: "apiKeyShare",
    method: "INVOKE",
    path: "IProductApi.EstablishKeyShareAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "POST /session/key-share — device public keys only (no private/mnemonic).",
    requestExample: "{ \"MLKEM_PUBLIC_KEY\": \"…\", \"X25519_PUBLIC_KEY\": \"…\" }",
    responseExample: "{ \"KEY_SHARE_ID\": \"…\", \"MLKEM_CIPHERTEXT\": \"…\", \"SERVER_X25519_PUBLIC_KEY\": \"…\" }",
    statuses: [
      { code: "200", behavior: "KeyShareResponseDto." },
    ],
  },
  {
    id: "apiPortfolio",
    method: "INVOKE",
    path: "IProductApi.GetPortfolioAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "GET /portfolio for Home totals and holdings.",
    requestEmpty: true,
    requestExample: "{}",
    responseFields: [
      { name: "TOTAL_USD", type: "string", desc: "Portfolio total USD (decimal string)." },
      { name: "HOLDINGS", type: "array", desc: "SYMBOL, NAME, BALANCE, USD_VALUE, CHANGE_24H_PCT." },
    ],
    responseExample: "{\n  \"TOTAL_USD\": \"128432.19\",\n  \"HOLDINGS\": [ { \"SYMBOL\": \"BTC\", \"BALANCE\": \"1.204\", \"USD_VALUE\": \"76104.22\" } ]\n}",
    statuses: [
      { code: "200", behavior: "PortfolioDto." },
    ],
  },
  {
    id: "apiHistory",
    method: "INVOKE",
    path: "IProductApi.GetHistoryAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "GET /history?symbols=&range= for Home sparklines.",
    requestFields: [
      { name: "symbol", type: "string", desc: "Ticker." },
      { name: "range", type: "string", desc: "Chart range key." },
    ],
    requestExample: "{ \"symbol\": \"BTC\", \"range\": \"1D\" }",
    responseExample: "[ { \"T\": 1720900000000, \"V\": 65000.0 } ]",
    statuses: [
      { code: "200", behavior: "HistoryPointDto[]." },
    ],
  },
  {
    id: "apiProductQuote",
    method: "INVOKE",
    path: "IProductApi.GetQuoteAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "Product /v1 quotes path (distinct from public POST /quote). Convert UI uses public /iquote for lock.",
    requestExample: "{ \"from\": \"BTC\", \"to\": \"USD\" }",
    responseExample: "{ \"FROM\": \"BTC\", \"TO\": \"USD\", \"RATE\": 65000, \"EXPIRES_AT\": 1720900015000 }",
    statuses: [
      { code: "200", behavior: "QuoteDto." },
    ],
  },
  {
    id: "apiConvert",
    method: "INVOKE",
    path: "IProductApi.ConvertAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "POST v1/convert with FROM/TO/AMOUNT + Idempotency-Key. Indicative public quote is NOT sent.",
    requestFields: [
      { name: "from", type: "string", desc: "App ticker." },
      { name: "to", type: "string", desc: "App ticker." },
      { name: "amount", type: "string", desc: "Input amount." },
      { name: "idempotencyKey", type: "string", desc: "Client Guid." },
    ],
    requestExample: "{\n  \"FROM\": \"BTC\",\n  \"TO\": \"USD\",\n  \"AMOUNT\": \"0.01\",\n  \"Idempotency-Key\": \"…\"\n}",
    responseExample: "{ \"ID\": \"tx_…\", \"STATUS\": \"pending\" }",
    logic: "Mock returns Status=pending. Live may accept then settle via stream CONVERT.SETTLED.",
    statuses: [
      { code: "200|202", behavior: "MoneyMoveDto." },
    ],
  },
  {
    id: "apiTransfer",
    method: "INVOKE",
    path: "IProductApi.TransferAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "Send/ACH mutation with speed instant|ach + Idempotency-Key.",
    requestExample: "{ \"to\": \"payee\", \"amount\": \"25.00\", \"speed\": \"instant\", \"idempotencyKey\": \"…\" }",
    responseExample: "{ \"ID\": \"tx_…\", \"STATUS\": \"pending\" }",
    statuses: [
      { code: "200|202", behavior: "MoneyMoveDto." },
    ],
  },
  {
    id: "apiPay",
    method: "INVOKE",
    path: "IProductApi.PayAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "Multi-asset mix payment; server mediates to single currency for recipient.",
    requestExample: "{ \"amount\": \"100\", \"mix\": { \"BTC\": \"0.001\", \"USD\": \"50\" }, \"idempotencyKey\": \"…\" }",
    responseExample: "{ \"ID\": \"pay_…\", \"STATUS\": \"pending\" }",
    statuses: [
      { code: "200|202", behavior: "MoneyMoveDto." },
    ],
  },
  {
    id: "apiReceive",
    method: "INVOKE",
    path: "IProductApi.GetReceiveAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "GET receive payload for asset (handle/address/uri).",
    requestExample: "{ \"asset\": \"BTC\" }",
    responseExample: "{ \"ASSET\": \"BTC\", \"ADDRESS\": \"bc1…\", \"URI\": \"bitcoin:…\" }",
    statuses: [
      { code: "200", behavior: "ReceiveDto." },
    ],
  },
  {
    id: "apiCreateWallet",
    method: "INVOKE",
    path: "IProductApi.CreateWalletAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "Managed/unmanaged/watch wallet. Result never includes spend key.",
    requestExample: "{ \"SYMBOL\": \"XMR\", \"MODE\": \"managed\", \"LABEL\": \"Primary\" }",
    responseExample: "{ \"WALLET_ID\": \"wal_…\", \"ADDRESS\": \"4…\", \"MODE\": \"managed\" }",
    statuses: [
      { code: "200", behavior: "CreateWalletResultDto." },
    ],
  },
  {
    id: "apiPos",
    method: "INVOKE",
    path: "IProductApi.CreatePosSessionAsync | AuthorizePosAsync | ConfirmPosAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "POS lab session lifecycle → PosSessionDto (TOKEN_REF, LAST4 — no PAN).",
    requestExample: "{ \"sessionId\": \"pos_…\" }",
    responseExample: "{ \"SESSION_ID\": \"pos_…\", \"STATUS\": \"ready_to_present\", \"TOKEN_REF\": \"…\" }",
    statuses: [
      { code: "200", behavior: "PosSessionDto." },
    ],
  },
  {
    id: "apiPrefs",
    method: "INVOKE",
    path: "IProductApi.GetPrefsAsync | PutPrefsAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "GET/PUT /prefs wire DTO for PrefsSyncService.",
    requestExample: "{ /* PrefsWireDto */ }",
    responseExample: "{ /* PrefsWireDto or null */ }",
    statuses: [
      { code: "200", behavior: "Prefs read/written." },
    ],
  },
  {
    id: "apiBootstrap",
    method: "INVOKE",
    path: "IProductApi.GetAccountBootstrapAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "GET /account/bootstrap — prefs + ACH contacts. Never mnemonic/PIN.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"PREFS\": { }, \"RECIPIENTS\": [ ] }",
    statuses: [
      { code: "200", behavior: "AccountBootstrapDto." },
    ],
  },
  {
    id: "apiVault",
    method: "INVOKE",
    path: "IProductApi.GetVaultCardsAsync | GetVaultBinariesAsync",
    host: "HttpProductApi | MockProductApi",
    section: "Product API",
    summary: "Vault metadata — last4 / labels only; no PAN, no mnemonic.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "[ { \"CARD_ID\": \"…\", \"LAST4\": \"4242\", \"BRAND\": \"visa\" } ]",
    statuses: [
      { code: "200", behavior: "Vault DTOs." },
    ],
  },
  {
    id: "publicTest",
    method: "INVOKE",
    path: "IPublicQuoteService.TestConnectionAsync",
    host: "PublicApiClient · api.cipherbank.money",
    section: "Public quotes",
    summary: "POST /test connectivity probe.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"ok\": true }",
    statuses: [
      { code: "true", behavior: "2xx." },
      { code: "false", behavior: "Non-success." },
    ],
  },
  {
    id: "publicCurrencies",
    method: "INVOKE",
    path: "IPublicQuoteService.GetCurrenciesAsync",
    host: "PublicApiClient · api.cipherbank.money",
    section: "Public quotes",
    summary: "POST /currencies → map CURRENCIES via CurrencySymbolMap.ToAppSymbol.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"currencies\": [\"BTC\", \"XMR\", \"USD\"] }",
    statuses: [
      { code: "ok", behavior: "Ordered app symbols." },
    ],
  },
  {
    id: "publicIquote",
    method: "INVOKE",
    path: "IPublicQuoteService.GetInverseQuoteAsync",
    host: "PublicApiClient · api.cipherbank.money",
    section: "Public quotes",
    summary: "POST /iquote — fixed input → output. Used by Convert.LockQuoteAsync.",
    requestFields: [
      { name: "INPUT_CURRENCY", type: "string", desc: "API code e.g. BITCOIN (mapped from BTC)." },
      { name: "INPUT_AMOUNT", type: "number (double)", desc: "Input amount." },
      { name: "OUTPUT_CURRENCY", type: "string", desc: "API code e.g. USD." },
    ],
    requestExample: "{\n  \"INPUT_CURRENCY\": \"BITCOIN\",\n  \"INPUT_AMOUNT\": 0.0015,\n  \"OUTPUT_CURRENCY\": \"USD\"\n}",
    responseExample: "{\n  \"inputSymbol\": \"BTC\",\n  \"outputAmount\": 100.0,\n  \"rate\": 66666.66666667\n}",
    statuses: [
      { code: "ok", behavior: "PublicQuote." },
      { code: "422|424", behavior: "Business / dependency errors." },
    ],
  },
  {
    id: "publicQuote",
    method: "INVOKE",
    path: "IPublicQuoteService.GetQuoteAsync",
    host: "PublicApiClient · api.cipherbank.money",
    section: "Public quotes",
    summary: "POST /quote — fixed output → required input (public API; not product GetQuoteAsync).",
    requestExample: "{\n  \"INPUT_CURRENCY\": \"BITCOIN\",\n  \"OUTPUT_AMOUNT\": 100.0,\n  \"OUTPUT_CURRENCY\": \"USD\"\n}",
    responseExample: "{ \"inputAmount\": 0.0015, \"rate\": 66666.66666667 }",
    statuses: [
      { code: "ok", behavior: "PublicQuote." },
    ],
  },
  {
    id: "currencyMap",
    method: "INVOKE",
    path: "CurrencySymbolMap.ToApiCurrency | ToAppSymbol",
    host: "CipherBank-app.Core · Services",
    section: "Public quotes",
    summary: "BTC↔BITCOIN, XMR↔MONERO, USD↔USD at public API boundary.",
    requestExample: "{ \"appSymbol\": \"BTC\" }",
    responseExample: "{ \"apiCurrency\": \"BITCOIN\" }",
    logic: "ToApiCurrency throws on unsupported. ToAppSymbol returns uppercase key as-is when unmapped (no throw). IsSupported checks AppToApi map.",
    statuses: [
      { code: "ok", behavior: "Mapped." },
      { code: "throw", behavior: "ToApiCurrency only — unsupported symbol." },
    ],
  },
  {
    id: "indicativeMapper",
    method: "INVOKE",
    path: "IndicativeQuoteMapper.ToQuoteDto",
    host: "CipherBank-app.Core · Services",
    section: "Public quotes",
    summary: "Maps PublicQuote → QuoteDto with client TTL (default 15_000 ms) for Convert countdown.",
    requestExample: "{ \"publicQuote\": { }, \"nowMs\": 1720900000000, \"ttlMs\": 15000 }",
    responseExample: "{ \"FROM\": \"BTC\", \"TO\": \"USD\", \"RATE\": 65000, \"EXPIRES_AT\": 1720900015000 }",
    statuses: [
      { code: "ok", behavior: "Indicative QuoteDto (not a server lock)." },
    ],
  },
  {
    id: "streamConnect",
    method: "INVOKE",
    path: "ClientWebSocketStreamService.ConnectAsync",
    host: "CipherBank-app.Core · V1",
    section: "Stream & prefs",
    summary: "Always DisconnectAsync first (tear down prior socket), then WSS connect + receive loop.",
    requestEmpty: true,
    requestExample: "{}",
    responseEmpty: true,
    responseExample: "{}",
    logic: "Prevents leaked sockets on reconnect. Parses { TYPE, PAYLOAD } SCREAMING events.",
    statuses: [
      { code: "ok", behavior: "IsConnected; events to StreamHub." },
    ],
  },
  {
    id: "streamDisconnect",
    method: "INVOKE",
    path: "IStreamService.DisconnectAsync",
    host: "CipherBank-app.Core · V1",
    section: "Stream & prefs",
    summary: "Close WebSocket / stop mock ticks. Called from Connect (preflight) and AppSession.Lock.",
    requestEmpty: true,
    requestExample: "{}",
    responseEmpty: true,
    responseExample: "{}",
    statuses: [
      { code: "ok", behavior: "Disconnected." },
    ],
  },
  {
    id: "streamHub",
    method: "INVOKE",
    path: "StreamHub.Start | Stop",
    host: "CipherBank-app.Core · V1",
    section: "Stream & prefs",
    summary: "Single fan-out of EventReceived. Start on unlock; Stop on Lock.",
    requestEmpty: true,
    requestExample: "{}",
    responseEmpty: true,
    responseExample: "{}",
    statuses: [
      { code: "ok", behavior: "Hub running or stopped." },
    ],
  },
  {
    id: "prefsLoadSave",
    method: "INVOKE",
    path: "PrefsStore.LoadAsync | SaveAsync",
    host: "CipherBank-app.Core · Persist",
    section: "Stream & prefs",
    summary: "Local UserPrefs JSON: EnabledCurrencies, DefaultSendSpeed, Cora, idle, appearance, home sections.",
    requestExample: "{\n  \"enabledCurrencies\": [\"BTC\", \"XMR\", \"USD\"],\n  \"defaultSendSpeed\": \"instant\",\n  \"lockIdleSeconds\": 120\n}",
    responseExample: "{ \"saved\": true }",
    logic: "Empty EnabledCurrencies normalizes to BTC,XMR,USD. DefaultSendSpeed must be instant|ach.",
    statuses: [
      { code: "ok", behavior: "Normalized prefs in SQLite." },
    ],
  },
  {
    id: "prefsSync",
    method: "INVOKE",
    path: "PrefsSyncService.PullMergeAsync | SaveAndPushAsync",
    host: "CipherBank-app.Core · V1",
    section: "Stream & prefs",
    summary: "Local ↔ GET/PUT /prefs. PullMerge on unlock; SaveAndPush from Profile.",
    requestExample: "{ /* UserPrefs */ }",
    responseExample: "{ \"cloudSynced\": true }",
    logic: "PrefsMerge.Merge preserves local AssetsLayout if remote omits. SaveAndPush always saves local; returns false if PUT fails.",
    statuses: [
      { code: "ok", behavior: "Merged or pushed." },
      { code: "false", behavior: "Local saved; cloud PUT failed." },
    ],
  },
  {
    id: "bootstrapApply",
    method: "INVOKE",
    path: "AccountBootstrapService.ApplyAsync",
    host: "CipherBank-app.Core · V1",
    section: "Stream & prefs",
    summary: "GET bootstrap → merge prefs → upsert recipients. Never touches custody.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{\n  \"recipientsUpserted\": 2,\n  \"idExample\": \"bootstrap_a1b2c3d4e5f67890\"\n}",
    logic: "ResolvedId: wire Id/IdCamel first; else SHA256(lowercase name) → bootstrap_+16 hex; if name empty seed=last4|routing; if still empty recipient_unknown. Skips recipients without 9-digit routing / empty name. Account stored masked ****+last4.",
    statuses: [
      { code: "ok", behavior: "Local prefs/recipients updated." },
    ],
  },
  {
    id: "localDbInit",
    method: "INVOKE",
    path: "LocalDb.InitializeAsync",
    host: "CipherBank-app.Core · Persist",
    section: "Stream & prefs",
    summary: "Create cipherbank.db + DDL (wallets, prefs, recipients, ohlc). Android uses Bionic e_sqlite3 only.",
    requestEmpty: true,
    requestExample: "{}",
    responseEmpty: true,
    responseExample: "{}",
    statuses: [
      { code: "ok", behavior: "Schema ready." },
    ],
  },
  {
    id: "walletRepo",
    method: "INVOKE",
    path: "WalletRepository.ListAsync | UpsertAsync | DeleteAsync",
    host: "CipherBank-app.Core · Persist",
    section: "Stream & prefs",
    summary: "Local wallet rows: address/path only — no private keys.",
    requestExample: "{ \"id\": \"wal_btc_0\", \"symbol\": \"BTC\", \"address\": \"bc1…\" }",
    responseExample: "{ \"count\": 2 }",
    statuses: [
      { code: "ok", behavior: "SQLite row ops." },
    ],
  },
  {
    id: "recipientRepo",
    method: "INVOKE",
    path: "RecipientRepository.ListAsync | UpsertAsync | SeedDefaultsIfEmptyAsync",
    host: "CipherBank-app.Core · Persist",
    section: "Stream & prefs",
    summary: "Local ACH payees + demo seed. Validate/Mask via AchRecipientValidation.",
    requestExample: "{ \"name\": \"Alex\", \"routing\": \"021000021\", \"account\": \"••••1234\" }",
    responseExample: "{ \"seeded\": false }",
    statuses: [
      { code: "ok", behavior: "Recipients ready." },
    ],
  },
  {
    id: "walletSeeder",
    method: "INVOKE",
    path: "LocalWalletSeeder.EnsureDerivedAsync",
    host: "CipherBank-app.Core · Wallets",
    section: "Wallets",
    summary: "For each missing derivable symbol (default BTC, ETH): AddressDerive → Upsert LocalWalletRow.",
    requestFields: [
      { name: "mnemonic", type: "string", desc: "In-process only." },
      { name: "symbols", type: "string[]?", desc: "Defaults BTC,ETH." },
    ],
    requestExample: "{ \"symbols\": [\"BTC\", \"ETH\"] }",
    responseExample: "{ \"upserted\": [\"BTC\", \"ETH\"] }",
    statuses: [
      { code: "ok", behavior: "Derived addresses in SQLite." },
    ],
  },
  {
    id: "addressDerive",
    method: "INVOKE",
    path: "AddressDerive.Derive",
    host: "CipherBank-app.Core · Wallets",
    section: "Wallets",
    summary: "BIP84 BTC/LTC, BIP44 DOGE, BIP44 ETH (Nethereum). Public address + path only.",
    requestExample: "{ \"symbol\": \"BTC\", \"accountIndex\": 0 }",
    responseExample: "{ \"address\": \"bc1…\", \"path\": \"m/84'/0'/0'/0/0\" }",
    statuses: [
      { code: "ok", behavior: "DerivedAddress." },
      { code: "throw", behavior: "Unsupported symbol." },
    ],
  },
  {
    id: "nfcPresent",
    method: "INVOKE",
    path: "INfcPresentmentService.PresentAsync",
    host: "CipherBank-app · Pos",
    section: "Wallets",
    summary: "Present NfcPresentmentPayload (tokenRef only — never PAN). Null impl on non-Android.",
    requestExample: "{ \"tokenRef\": \"…\", \"last4\": \"4242\" }",
    responseExample: "{ \"ok\": true }",
    statuses: [
      { code: "ok", behavior: "NDEF presented." },
      { code: "fail", behavior: "Unsupported / LastError set." },
    ],
  },
  {
    id: "welcomeCreate",
    method: "INVOKE",
    path: "WelcomeViewModel.CreateWalletAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Navigate //KeysPage to begin custody onboarding.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"navigated\": \"//KeysPage\" }",
    statuses: [
      { code: "ok", behavior: "Keys shown." },
    ],
  },
  {
    id: "welcomeReturning",
    method: "INVOKE",
    path: "WelcomeViewModel.ReturningAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "HasSealedWalletAsync → Unlock if sealed else Keys.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"navigated\": \"//UnlockPage\" }",
    statuses: [
      { code: "ok", behavior: "Unlock or Keys." },
    ],
  },
  {
    id: "keysContinue",
    method: "INVOKE",
    path: "KeysViewModel.ContinueAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Ctor generates BIP39 via MnemonicHelper.Generate; Continue → BackupQuiz?mnemonic=.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"navigated\": \"//BackupQuizPage?mnemonic=…\" }",
    logic: "CopyAsync optional clipboard. Continue passes mnemonic in query string (on-device navigation only).",
    statuses: [
      { code: "ok", behavior: "Quiz next." },
    ],
  },
  {
    id: "backupVerify",
    method: "INVOKE",
    path: "BackupQuizViewModel.VerifyAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "PickRandom 3 words; on match → SetPin?mnemonic=.",
    requestExample: "{ \"answers\": [\"word2\", \"word7\", \"word11\"] }",
    responseExample: "{ \"navigated\": \"//SetPinPage?mnemonic=…\" }",
    statuses: [
      { code: "ok", behavior: "SetPin." },
      { code: "error", behavior: "Inline mismatch Error." },
    ],
  },
  {
    id: "setPinSeal",
    method: "INVOKE",
    path: "SetPinViewModel.SealAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "PIN≥6 + match + BIP39 validate → FinishCustodySetupAsync → //HomePage.",
    requestExample: "{\n  \"pin\": \"••••••\",\n  \"confirmPin\": \"••••••\",\n  \"mnemonic\": \"<from query>\"\n}",
    responseExample: "{ \"navigated\": \"//HomePage\" }",
    logic: "Clear Error → length/match/validate checks → IsBusy → FinishCustodySetupAsync → GoTo Home. Catch → Error=ex.Message.",
    statuses: [
      { code: "ok", behavior: "Wallet sealed; Home." },
      { code: "error", behavior: "Inline Error string." },
    ],
  },
  {
    id: "unlockAppearing",
    method: "INVOKE",
    path: "UnlockViewModel.AppearingAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Pin.RefreshAsync; if device-secret + biometrics available may auto-prompt UnlockWithBiometrics.",
    requestEmpty: true,
    requestExample: "{}",
    responseEmpty: true,
    responseExample: "{}",
    statuses: [
      { code: "ok", behavior: "Lockout UI refreshed; optional biometric prompt." },
    ],
  },
  {
    id: "unlockPin",
    method: "INVOKE",
    path: "UnlockViewModel.UnlockAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "RefreshAsync + lockout check → AppSession.UnlockAsync(Pin) → //HomePage.",
    requestExample: "{ \"pin\": \"••••••\" }",
    responseExample: "{ \"navigated\": \"//HomePage\" }",
    statuses: [
      { code: "ok", behavior: "Home." },
      { code: "error", behavior: "Lockout or incorrect PIN message." },
    ],
  },
  {
    id: "unlockBio",
    method: "INVOKE",
    path: "UnlockViewModel.UnlockWithBiometricsAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "OS AuthenticateAsync → AppSession.UnlockWithDeviceOwnerAsync → Home.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"navigated\": \"//HomePage\" }",
    statuses: [
      { code: "ok", behavior: "Home." },
      { code: "error", behavior: "Biometric fail; fall back to PIN messaging." },
    ],
  },
  {
    id: "homeAppearing",
    method: "INVOKE",
    path: "HomeViewModel.AppearingAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Touch; GetPortfolioAsync; local wallets; history charts; stream soft-refresh; EnabledCurrencies / values-hidden prefs.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"totalUsd\": \"128432.19\" }",
    statuses: [
      { code: "ok", behavior: "Home bound." },
    ],
  },
  {
    id: "homeRange",
    method: "INVOKE",
    path: "HomeViewModel.SetRangeAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Reload sparkline history for selected range via GetHistoryAsync.",
    requestExample: "{ \"range\": \"1W\" }",
    responseExample: "{ \"points\": 168 }",
    statuses: [
      { code: "ok", behavior: "Charts updated." },
    ],
  },
  {
    id: "convertLockQuote",
    method: "INVOKE",
    path: "ConvertViewModel.LockQuoteAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Indicative lock: public GetInverseQuoteAsync → IndicativeQuoteMapper.ToQuoteDto (15s TTL) → countdown.",
    requestExample: "{ \"fromAsset\": \"BTC\", \"toAsset\": \"USD\", \"amount\": \"0.01\" }",
    responseExample: "{\n  \"isIndicative\": true,\n  \"hasValidLock\": true,\n  \"ttlMs\": 15000\n}",
    logic: "Does not call product GetQuoteAsync. Settlement is a separate ConvertAsync that does not send the indicative quote to the server.",
    statuses: [
      { code: "ok", behavior: "Indicative quote + countdown." },
      { code: "alert", behavior: "Amount / Quote dialogs." },
    ],
  },
  {
    id: "convertSettle",
    method: "INVOKE",
    path: "ConvertViewModel.ConvertAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Unlocked + StepUp(Convert) + fresh indicative → IProductApi.ConvertAsync (product settle).",
    requestExample: "{ \"fromAsset\": \"BTC\", \"toAsset\": \"USD\", \"amount\": \"0.01\" }",
    responseExample: "{ \"status\": \"Convert tx_…: pending\" }",
    statuses: [
      { code: "ok", behavior: "Alert with move status." },
      { code: "blocked", behavior: "Locked / step-up cancel / stale quote." },
    ],
  },
  {
    id: "convertSwap",
    method: "INVOKE",
    path: "ConvertViewModel.SwapAssets",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Swap From/To; clear lock, indicative flag, countdown.",
    requestEmpty: true,
    requestExample: "{}",
    responseEmpty: true,
    responseExample: "{}",
    statuses: [
      { code: "ok", behavior: "Pair swapped; lock cleared." },
    ],
  },
  {
    id: "sendAsync",
    method: "INVOKE",
    path: "SendViewModel.SendAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Require unlocked → TransferAsync(to, amount, speed, idempotencyKey).",
    requestExample: "{ \"to\": \"Alex\", \"amount\": \"25\", \"speed\": \"instant\" }",
    responseExample: "{ \"status\": \"Send tx_…: pending\" }",
    statuses: [
      { code: "ok", behavior: "Alert result." },
      { code: "alert", behavior: "Locked / Recipient / Amount." },
    ],
  },
  {
    id: "sendAddRecipient",
    method: "INVOKE",
    path: "SendViewModel.AddRecipientAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "AchRecipientValidation.Validate → RecipientRepository.UpsertAsync.",
    requestExample: "{ \"name\": \"Alex\", \"routing\": \"021000021\", \"account\": \"123456789\" }",
    responseExample: "{ \"saved\": true }",
    statuses: [
      { code: "ok", behavior: "Recipient saved." },
      { code: "alert", behavior: "Validation fail." },
    ],
  },
  {
    id: "payAsync",
    method: "INVOKE",
    path: "PayViewModel.PayAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Unlocked + StepUp(Payment) → PayAsync(amount, mix, idempotencyKey).",
    requestExample: "{ \"amount\": \"100\", \"mix\": { \"BTC\": \"0.001\", \"USD\": \"50\" } }",
    responseExample: "{ \"status\": \"Pay pay_…: pending\" }",
    statuses: [
      { code: "ok", behavior: "Alert result." },
      { code: "blocked", behavior: "Locked / step-up / mix." },
    ],
  },
  {
    id: "receiveLoad",
    method: "INVOKE",
    path: "ReceiveViewModel.LoadAsync | DeriveNewAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Local derive or GetReceiveAsync; QR PNG; DeriveNew upserts new account index.",
    requestExample: "{ \"asset\": \"BTC\" }",
    responseExample: "{ \"address\": \"bc1…\", \"qrPngBytes\": 1234 }",
    statuses: [
      { code: "ok", behavior: "Address/QR ready." },
    ],
  },
  {
    id: "addWalletSave",
    method: "INVOKE",
    path: "AddWalletViewModel.SaveAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Branched: Derive local / Watch address / Managed CreateWalletAsync (XMR).",
    requestExample: "{ \"mode\": \"managed\", \"symbol\": \"XMR\", \"label\": \"Primary\" }",
    responseExample: "{ \"saved\": true }",
    logic: "Derive: AddressDerive + WalletRepository. Watch: validate address upsert. Managed: requires unlocked → CreateWalletAsync (no spend key returned).",
    statuses: [
      { code: "ok", behavior: "Alert Saved." },
      { code: "alert", behavior: "Locked / Invalid / Failed." },
    ],
  },
  {
    id: "posStart",
    method: "INVOKE",
    path: "PosLabViewModel.StartSessionAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Step-up → CreatePosSession → AuthorizePos → ConfirmPos (single command chain).",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"sessionId\": \"pos_…\", \"status\": \"ready_to_present\" }",
    logic: "Three IProductApi calls in sequence after StepUpAuth(PosAuthorize).",
    statuses: [
      { code: "ok", behavior: "Session fields filled." },
      { code: "blocked", behavior: "Step-up cancel / API error." },
    ],
  },
  {
    id: "posNfc",
    method: "INVOKE",
    path: "PosLabViewModel.PresentNfcAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Step-up PosPresent → INfcPresentmentService.PresentAsync(tokenRef payload).",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"presented\": true }",
    statuses: [
      { code: "ok", behavior: "NFC presentment." },
      { code: "alert", behavior: "Unsupported / fail." },
    ],
  },
  {
    id: "profileSave",
    method: "INVOKE",
    path: "ProfileViewModel.SavePrefsAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "Persist appearance, base currency, send speed, enabled currencies, idle; PrefsSync.SaveAndPushAsync.",
    requestExample: "{\n  \"appearance\": \"dark\",\n  \"enabledCurrencies\": [\"BTC\", \"XMR\", \"USD\"],\n  \"lockIdleSeconds\": 120\n}",
    responseExample: "{ \"saved\": true, \"cloudSynced\": true }",
    statuses: [
      { code: "ok", behavior: "Alert Saved; session IdleMs updated." },
    ],
  },
  {
    id: "profileReveal",
    method: "INVOKE",
    path: "ProfileViewModel.RevealMnemonicAsync",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "StepUp(RevealKeys) → ExportMnemonic; auto-clear ~30s.",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"revealedSeconds\": 30 }",
    statuses: [
      { code: "ok", behavior: "Mnemonic shown then cleared." },
      { code: "blocked", behavior: "Step-up cancel / locked." },
    ],
  },
  {
    id: "profileLock",
    method: "INVOKE",
    path: "ProfileViewModel.Lock",
    host: "CipherBank-app · ViewModels",
    section: "UI flows",
    summary: "AppSession.Lock → GoTo Unlock (PQ clear via idle Locked handler).",
    requestEmpty: true,
    requestExample: "{}",
    responseExample: "{ \"navigated\": \"//UnlockPage\" }",
    statuses: [
      { code: "ok", behavior: "Locked + Unlock page." },
    ],
  },
  {
    id: "coraFab",
    method: "INVOKE",
    path: "CoraFab (ScreenKey)",
    host: "CipherBank-app · Controls",
    section: "Cora UI",
    summary: "Floating assistant; CoraLines.For(ScreenKey); visibility from CoraEnabled pref.",
    requestExample: "{ \"screenKey\": \"home\" }",
    responseExample: "{ \"line\": \"…\" }",
    statuses: [
      { code: "ok", behavior: "Tap toggles speech bubble." },
    ],
  },
  {
    id: "coraBar",
    method: "INVOKE",
    path: "CoraBar (ScreenKey | Line)",
    host: "CipherBank-app · Controls",
    section: "Cora UI",
    summary: "Inline Expo-parity Cora strip; same line source / pref gate as FAB.",
    requestExample: "{ \"screenKey\": \"convert\" }",
    responseExample: "{ \"visible\": true }",
    statuses: [
      { code: "ok", behavior: "Bar shown when CoraEnabled." },
    ],
  },
];

const css =
  fs.readFileSync(cssSource, 'utf8').match(/<style>[\s\S]*?<\/style>/)?.[0] ??
  '<style></style>';

function esc(s) {
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function fieldsTable(fields) {
  if (!fields?.length) return '';
  const rows = fields
    .map(
      (f) => `
							<tr>
								<th scope="row"><code>${esc(f.name)}</code></th>
								<td class="type">${esc(f.type)}</td>
								<td>${esc(f.desc)}</td>
							</tr>`,
    )
    .join('');
  return `
					<table>
						<thead><tr><th scope="col">Field</th><th scope="col">Type</th><th scope="col">Description</th></tr></thead>
						<tbody>${rows}
						</tbody>
					</table>`;
}

function renderInvoke(ep) {
  const reqBody = ep.requestEmpty
    ? `<p class="empty">No structured input (void / parameterless).</p>`
    : fieldsTable(ep.requestFields);
  const resBody = ep.responseEmpty
    ? `<p class="empty">No structured return (void / fire-and-forget side effects).</p>`
    : (ep.responseNote ? `<p>${esc(ep.responseNote)}</p>` : '') +
      fieldsTable(ep.responseFields ?? []);
  const statuses = ep.statuses
    .map(
      (s) =>
        `\n							<tr><th scope="row"><code>${esc(s.code)}</code></th><td>${esc(s.behavior)}</td></tr>`,
    )
    .join('');
  const logic = ep.logic
    ? `<p><strong>Logic:</strong> ${esc(ep.logic)}</p>`
    : '';

  return `
			<article class="endpoint" id="${esc(ep.id)}">
				<div class="route">
					<span class="method">${esc(ep.method)}</span>
					<h2><code>${esc(ep.path)}</code></h2>
				</div>
				<p>${esc(ep.summary)}</p>
				<p><strong>Assembly:</strong> <code>${esc(ep.host)}</code> · <strong>Kind:</strong> on-device C# · <strong>Wire:</strong> never sends mnemonic/PIN/spend keys</p>
				${logic}

				<section aria-labelledby="${esc(ep.id)}-request">
					<h3 id="${esc(ep.id)}-request">Input</h3>
					${reqBody}
					<h4>Example</h4>
					<pre><code>${esc(ep.requestExample ?? '{}')}</code></pre>
				</section>

				<section aria-labelledby="${esc(ep.id)}-response">
					<h3 id="${esc(ep.id)}-response">Result / effects</h3>
					${resBody}
					<h4>Example</h4>
					<pre><code>${esc(ep.responseExample ?? '{}')}</code></pre>
				</section>

				<section aria-labelledby="${esc(ep.id)}-statuses">
					<h3 id="${esc(ep.id)}-statuses">Outcomes</h3>
					<table>
						<thead><tr><th scope="col">Status</th><th scope="col">Behavior</th></tr></thead>
						<tbody>${statuses}
						</tbody>
					</table>
				</section>
			</article>`;
}

const sections = [...new Set(invokables.map((i) => i.section).filter(Boolean))];

const navItems = [
  `<li><a href="#runtime">Runtime conventions</a></li>`,
  `<li><a href="#call-graph">Unlock / seal graph</a></li>`,
  `<li><a href="#never-wire">Never on wire</a></li>`,
  ...sections.map((s) => {
    const first = invokables.find((i) => i.section === s);
    return `<li><a href="#${esc(first.id)}"><strong>${esc(s)}</strong></a></li>`;
  }),
  ...invokables.map(
    (ep) =>
      `<li><a href="#${esc(ep.id)}"><span aria-hidden="true">${esc(ep.method)}</span> ${esc(ep.path)}</a></li>`,
  ),
].join('\n\t\t\t\t');

const articles = invokables.map(renderInvoke).join('\n');

const html = `<!doctype html>
<html lang="en">
<head>
	<meta charset="utf-8">
	<meta name="viewport" content="width=device-width, initial-scale=1">
	<title>CipherBank MAUI Function Reference</title>
	${css}
</head>
<body>
	<header>
		<h1>CipherBank MAUI Function Reference</h1>
		<p>On-device C# INVOKEs for the Cora Digital Teller MAUI app — Shell, custody, session, challenge/pass, product + public quotes — in the same navigable style as <code>CB_FullAPIRef.html</code>.</p>
		<p>
			<a href="./docs/MAUI_FUNCTION_REF.md">Markdown companion</a>
			·
			<a href="./design_handoff_cipherbank/starter/docs/CB_FullAPIRef.html">Full HTTP API ref</a>
			·
			<a href="./design_handoff_cipherbank/starter/docs/PUBLIC_API.md">Public API standards</a>
			·
			<a href="https://github.com/CB-st/CipherBank-App/pull/16">PR #16</a>
		</p>
		<p><strong>${invokables.length} functions</strong> · regenerate: <code>node docs/scripts/generate-maui-function-ref.mjs</code></p>
	</header>
	<div class="layout">
		<nav aria-label="Function navigation">
			<h2>Functions</h2>
			<ul>
				${navItems}
			</ul>
		</nav>
		<main id="main">
			<section class="endpoint" id="runtime" aria-labelledby="runtime-title">
				<h2 id="runtime-title">Runtime conventions</h2>
				<table>
					<thead><tr><th scope="col">Rule</th><th scope="col">Value</th></tr></thead>
					<tbody>
						<tr><th scope="row">Badge</th><td><code>INVOKE</code> — C# method / RelayCommand (not HTTP)</td></tr>
						<tr><th scope="row">Projects</th><td><code>CipherBank-app</code> · <code>CipherBank-app.Core</code> · <code>CipherBank-app.ChallengePass</code></td></tr>
						<tr><th scope="row">Session proof</th><td><code>Lab</code> (default) · <code>ChallengePassA1</code> · <code>ChallengePassA2</code></td></tr>
						<tr><th scope="row">Unlock orchestrator</th><td><code>CompleteUnlockAsync</code> — session+stream required; prefs/bootstrap best-effort</td></tr>
						<tr><th scope="row">Product API</th><td><code>IProductApi</code> → mock or <code>HttpProductApi</code> (<code>/v1</code>)</td></tr>
						<tr><th scope="row">Public quotes</th><td><code>IPublicQuoteService</code> → <code>PublicApiClient</code> (<code>api.cipherbank.money</code>)</td></tr>
						<tr><th scope="row">Convert lock vs settle</th><td>Lock = public <code>/iquote</code> + 15s client TTL; settle = product <code>ConvertAsync</code> (quote not sent)</td></tr>
						<tr><th scope="row">Session wire</th><td><code>ACCESS_TOKEN</code> · <code>REFRESH_TOKEN</code> · <code>EXPIRES_AT</code></td></tr>
						<tr><th scope="row">Portfolio wire</th><td><code>TOTAL_USD</code> · holdings <code>BALANCE</code> / <code>USD_VALUE</code></td></tr>
						<tr><th scope="row">SQLite Android</th><td>Ship <code>e_sqlite3.android</code> only — exclude desktop natives from APK</td></tr>
						<tr><th scope="row">Idle lock</th><td><code>AppIdleLockService</code> → <code>AppSession.Lock</code> → <code>IPqChannel.Clear</code> → Unlock</td></tr>
					</tbody>
				</table>
			</section>

			<section class="endpoint" id="call-graph" aria-labelledby="call-graph-title">
				<h2 id="call-graph-title">Unlock / seal call graph</h2>
				<pre><code>Splash (≥900ms) + Boot + IdleLock.Start
  → Welcome → Keys(Generate) → BackupQuiz → SetPin.Seal
       → FinishCustodySetup → Seal → Seed(BTC,ETH) → CompleteUnlock(no bootstrap)
  → Unlock.PIN|Biometrics
       → Unlock* → CompleteUnlock(bootstrap)
            → CreateSession (Lab|A1|A2 via ChallengePassSessionProofBuilder)
            → Stream.Connect (disconnect-first) + Hub.Start
            → PrefsSync.PullMerge [+ AccountBootstrap]  // best-effort
  → Home

Idle / Profile.Lock → AppSession.Lock → Locked → PQ Clear → Unlock</code></pre>
			</section>

			<section class="endpoint" id="never-wire" aria-labelledby="never-wire-title">
				<h2 id="never-wire-title">Never on the wire</h2>
				<p>Mnemonic, BIP39 entropy, PIN plaintext, device secret, account private key, spend keys, PAN/CVV, full ACH account numbers. Public/product APIs receive public keys, sealed ciphertexts, handles, and masked last4 only. CryptoBox passphrase for custody is the <strong>device secret</strong>, not the PIN (PIN is the logical gate).</p>
			</section>

${articles}

		</main>
	</div>
	<footer>CipherBank MAUI Function Reference · ${invokables.length} INVOKEs · PR #16 feat/cora-redesign-maui · regenerate: <code>node docs/scripts/generate-maui-function-ref.mjs</code></footer>
</body>
</html>
`;

fs.writeFileSync(outRoot, html);
fs.writeFileSync(outDocs, html);
console.log('Wrote', outRoot);
console.log('Wrote', outDocs);
console.log('Invokes:', invokables.length);
