#!/usr/bin/env node
/**
 * Generates CB_MauiFunctionRef.html (CB_FullAPIRef visual style) for on-device
 * MAUI / Core / ChallengePass INVOKEs. Writes repo root + docs/.
 *
 * Run: node docs/scripts/generate-maui-function-ref.mjs
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
  // ── Boot & shell ──
  {
    id: 'shellBootstrap',
    method: 'INVOKE',
    path: 'AppShell.BootstrapAsync',
    host: 'CipherBank-app · Shell',
    section: 'Boot & shell',
    summary:
      'Cold-start: show Splash (≥900ms), initialize SQLite + AppSession, start idle lock, route Welcome or Unlock.',
    requestEmpty: true,
    requestExample: '{}',
    responseFields: [
      { name: 'route', type: 'string', desc: '//UnlockPage if HasWallet else //WelcomePage (or Welcome on error).' },
    ],
    responseExample: `{
  "route": "//UnlockPage",
  "hasWallet": true,
  "minSplashMs": 900
}`,
    logic:
      'GoTo Splash → parallel BootSessionAsync(db.Initialize + session.Boot) with MinSplashDuration → idleLock.Start → GoTo Unlock|Welcome.',
    statuses: [
      { code: 'ok', behavior: 'Navigates past splash to Welcome or Unlock.' },
      { code: 'catch', behavior: 'Any boot exception → //WelcomePage.' },
    ],
  },
  {
    id: 'splashSetStatus',
    method: 'INVOKE',
    path: 'SplashPage.SetStatus',
    host: 'CipherBank-app · Views',
    section: 'Boot & shell',
    summary: 'Optional status caption on the Expo-parity ink splash while shell boots.',
    requestFields: [{ name: 'label', type: 'string', desc: 'Status line text.' }],
    requestExample: `{ "label": "Opening session…" }`,
    responseEmpty: true,
    responseExample: '{}',
    statuses: [{ code: 'ok', behavior: 'Label updated on main thread.' }],
  },
  {
    id: 'sessionBoot',
    method: 'INVOKE',
    path: 'AppSession.BootAsync',
    host: 'CipherBank-app.Core · Session',
    section: 'Boot & shell',
    summary: 'Sets HasWallet from sealed custody blob; loads IdleMs from UserPrefs.',
    requestEmpty: true,
    requestExample: '{}',
    responseExample: `{
  "hasWallet": true,
  "idleMs": 120000
}`,
    statuses: [{ code: 'ok', behavior: 'Boot flags ready for shell routing.' }],
  },

  // ── Custody & PIN ──
  {
    id: 'pinSet',
    method: 'INVOKE',
    path: 'PinService.SetPinAsync',
    host: 'CipherBank-app.Core · Custody',
    section: 'Custody & PIN',
    summary: 'Store PBKDF2-SHA256 hash + salt in secure storage; clear lockout. PIN plaintext never persisted.',
    requestFields: [{ name: 'pin', type: 'string', desc: 'Wallet PIN (≥6 in UI). Never on wire.' }],
    requestExample: `{ "pin": "••••••" }`,
    responseEmpty: true,
    responseExample: '{}',
    statuses: [
      { code: 'ok', behavior: 'cb_pin_hash / cb_pin_salt written.' },
    ],
  },
  {
    id: 'pinVerify',
    method: 'INVOKE',
    path: 'PinService.VerifyPinAsync',
    host: 'CipherBank-app.Core · Custody',
    section: 'Custody & PIN',
    summary: 'Refresh lockout from store, fixed-time hash compare; 5 fails → 5 min lockout.',
    requestFields: [{ name: 'pin', type: 'string', desc: 'Candidate PIN.' }],
    requestExample: `{ "pin": "••••••" }`,
    responseFields: [{ name: 'ok', type: 'boolean', desc: 'True when hash matches and not locked out.' }],
    responseExample: `{ "ok": true }`,
    statuses: [
      { code: 'true', behavior: 'PIN accepted; fail counters cleared.' },
      { code: 'false', behavior: 'Mismatch or lockout active.' },
    ],
  },
  {
    id: 'pinRefresh',
    method: 'INVOKE',
    path: 'PinService.RefreshAsync',
    host: 'CipherBank-app.Core · Custody',
    section: 'Custody & PIN',
    summary: 'Reload fail/lockout counters from secure storage into memory (Unlock appear).',
    requestEmpty: true,
    requestExample: '{}',
    responseEmpty: true,
    responseExample: '{}',
    statuses: [{ code: 'ok', behavior: 'In-memory lockout state matches store.' }],
  },
  {
    id: 'custodySeal',
    method: 'INVOKE',
    path: 'CustodyService.SealAsync',
    host: 'CipherBank-app.Core · Custody',
    section: 'Custody & PIN',
    summary: 'Validate BIP39 → set PIN → generate device secret → AES-GCM seal mnemonic → RAM unlock TTL 5m.',
    requestFields: [
      { name: 'mnemonic', type: 'string', desc: '12-word BIP39. Never on wire.' },
      { name: 'pin', type: 'string', desc: 'Wallet PIN.' },
    ],
    requestExample: `{
  "mnemonic": "<12 words>",
  "pin": "••••••"
}`,
    responseExample: `{
  "sealed": true,
  "sessionExpiresAt": "2026-07-20T18:05:00Z"
}`,
    logic: 'Blob key cb_custody_blob; device secret cb_device_secret_v1. Legacy PIN-as-passphrase migrates on unlock.',
    statuses: [
      { code: 'ok', behavior: 'Sealed at rest; unlocked in RAM.' },
      { code: 'throw', behavior: 'Invalid mnemonic or crypto failure.' },
    ],
  },
  {
    id: 'custodyUnlock',
    method: 'INVOKE',
    path: 'CustodyService.UnlockAsync',
    host: 'CipherBank-app.Core · Custody',
    section: 'Custody & PIN',
    summary: 'Verify PIN then open sealed blob with device secret (or legacy PIN).',
    requestFields: [{ name: 'pin', type: 'string', desc: 'Wallet PIN.' }],
    requestExample: `{ "pin": "••••••" }`,
    responseFields: [{ name: 'ok', type: 'boolean', desc: 'Unlocked with 5m TTL.' }],
    responseExample: `{ "ok": true }`,
    statuses: [
      { code: 'true', behavior: 'Mnemonic in RAM.' },
      { code: 'false', behavior: 'Bad PIN / lockout.' },
    ],
  },
  {
    id: 'custodyIsUnlocked',
    method: 'INVOKE',
    path: 'CustodyService.IsUnlocked',
    host: 'CipherBank-app.Core · Custody',
    section: 'Custody & PIN',
    summary: 'Getter: if TTL expired, calls Lock() (wipes RAM mnemonic) and returns false.',
    requestEmpty: true,
    requestExample: '{}',
    responseExample: `{ "isUnlocked": false }`,
    statuses: [
      { code: 'true', behavior: 'Mnemonic present and TTL valid.' },
      { code: 'false', behavior: 'Locked or auto-wiped on TTL.' },
    ],
  },

  // ── Session ──
  {
    id: 'sessionFinishCustody',
    method: 'INVOKE',
    path: 'AppSession.FinishCustodySetupAsync',
    host: 'CipherBank-app.Core · Session',
    section: 'Session',
    summary: 'Seal → seed derived wallets → CompleteUnlock without account bootstrap (Set PIN path).',
    requestFields: [
      { name: 'mnemonic', type: 'string', desc: 'BIP39 phrase.' },
      { name: 'pin', type: 'string', desc: 'New PIN.' },
    ],
    requestExample: `{ "mnemonic": "<12 words>", "pin": "••••••" }`,
    responseExample: `{
  "hasWallet": true,
  "accessToken": "<bearer>",
  "streamConnected": true
}`,
    logic: 'SealAsync → LocalWalletSeeder.EnsureDerivedAsync → CompleteUnlockAsync(applyBootstrap:false).',
    statuses: [{ code: 'ok', behavior: 'Ready for //HomePage.' }],
  },
  {
    id: 'sessionUnlock',
    method: 'INVOKE',
    path: 'AppSession.UnlockAsync',
    host: 'CipherBank-app.Core · Session',
    section: 'Session',
    summary: 'PIN unlock custody then CompleteUnlock with prefs pull + account bootstrap.',
    requestFields: [{ name: 'pin', type: 'string', desc: 'Wallet PIN.' }],
    requestExample: `{ "pin": "••••••" }`,
    responseExample: `{ "ok": true, "accessToken": "<bearer>" }`,
    statuses: [
      { code: 'true', behavior: 'Session + stream live.' },
      { code: 'false', behavior: 'Custody unlock failed.' },
    ],
  },
  {
    id: 'sessionUnlockDevice',
    method: 'INVOKE',
    path: 'AppSession.UnlockWithDeviceOwnerAsync',
    host: 'CipherBank-app.Core · Session',
    section: 'Session',
    summary: 'After OS biometrics: open blob with device secret → CompleteUnlock(bootstrap:true).',
    requestEmpty: true,
    requestExample: '{}',
    responseExample: `{ "ok": true }`,
    statuses: [
      { code: 'true', behavior: 'Biometric path unlocked.' },
      { code: 'false', behavior: 'No device secret or open failed.' },
    ],
  },
  {
    id: 'sessionLock',
    method: 'INVOKE',
    path: 'AppSession.Lock',
    host: 'CipherBank-app.Core · Session',
    section: 'Session',
    summary: 'Stop stream hub, wipe custody RAM, clear tokens/session store, disconnect WSS, raise Locked.',
    requestEmpty: true,
    requestExample: '{}',
    responseEmpty: true,
    responseExample: '{}',
    statuses: [{ code: 'ok', behavior: 'Idle lock / Profile.Lock navigate Unlock; PQ channel cleared by AppIdleLockService.' }],
  },

  // ── Challenge / pass ──
  {
    id: 'proofLab',
    method: 'INVOKE',
    path: 'LabSessionProofBuilder.BuildOpenBodyAsync',
    host: 'CipherBank-app.Core · V1',
    section: 'Challenge / pass',
    summary: 'Default Lab session open body (no crypto).',
    requestEmpty: true,
    requestExample: '{}',
    responseExample: `{
  "DEVICE_ATTESTATION": "lab"
}`,
    statuses: [{ code: 'ok', behavior: 'Stub attestation for POST /session.' }],
  },
  {
    id: 'proofA1',
    method: 'INVOKE',
    path: 'TwoStepChallengePassStructure.BuildSessionOpenBodyAsync',
    host: 'CipherBank-app.ChallengePass · A1',
    section: 'Challenge / pass',
    summary: 'X25519 challenge open + pass seal → SessionPassDto for POST /session.',
    requestFields: [
      { name: 'accountPublicKey', type: 'string (wire)', desc: 'URL-safe base64 account pk.' },
    ],
    requestExample: `{ "accountPublicKey": "<wire>" }`,
    responseExample: `{
  "CHALLENGE_ID": "…",
  "PASS_CIPHERTEXT": "…",
  "ACCOUNT_PUBLIC_KEY": "…",
  "ALGORITHM": "x25519-chacha20poly1305"
}`,
    logic: 'RequestChallenge → Open with account sk → SHA-256 pass → Seal to API pk. Private key never on wire.',
    statuses: [{ code: 'ok', behavior: 'SessionPassDto body ready.' }],
  },
  {
    id: 'proofA2',
    method: 'INVOKE',
    path: 'PqChannelChallengePassStructure.BuildSessionOpenBodyAsync',
    host: 'CipherBank-app.ChallengePass · A2',
    section: 'Challenge / pass',
    summary: 'Hybrid ML-KEM+X25519 key-share → PQ channel challenge/pass.',
    requestExample: `{
  "deviceMlKemPublicKey": "…",
  "deviceX25519PublicKey": "…"
}`,
    responseExample: `{
  "CHALLENGE_ID": "…",
  "PASS_CIPHERTEXT": "…",
  "ALGORITHM": "pq-channel-chacha20poly1305-v1"
}`,
    logic: 'Establish key-share if needed → SetChannelKey → channel Open/Seal. Channel cleared on idle lock.',
    statuses: [{ code: 'ok', behavior: 'PQ SessionPassDto body ready.' }],
  },

  // ── Product API ──
  {
    id: 'apiCreateSession',
    method: 'INVOKE',
    path: 'IProductApi.CreateSessionAsync',
    host: 'HttpProductApi | MockProductApi',
    section: 'Product API',
    summary: 'Build proof via ISessionProofBuilder then POST /session; persist SessionDto.',
    requestExample: `{ /* proof body from Lab | A1 | A2 */ }`,
    responseExample: `{
  "TOKEN": "…",
  "REFRESH_TOKEN": "…",
  "EXPIRES_AT": 1720900000000
}`,
    statuses: [
      { code: '200', behavior: 'Session stored; AccessToken set on unlock path.' },
      { code: 'throw', behavior: 'Non-success HTTP or empty body.' },
    ],
  },
  {
    id: 'apiPortfolio',
    method: 'INVOKE',
    path: 'IProductApi.GetPortfolioAsync',
    host: 'HttpProductApi | MockProductApi',
    section: 'Product API',
    summary: 'GET /portfolio for Home totals and holdings.',
    requestEmpty: true,
    requestExample: '{}',
    responseExample: `{
  "TOTAL": 128432.19,
  "HOLDINGS": [ { "SYMBOL": "BTC", "AMOUNT": "1.204" } ]
}`,
    statuses: [{ code: '200', behavior: 'Portfolio DTO returned.' }],
  },
  {
    id: 'apiConvert',
    method: 'INVOKE',
    path: 'IProductApi.ConvertAsync',
    host: 'HttpProductApi | MockProductApi',
    section: 'Product API',
    summary: 'Settle convert on product path (after indicative public quote in UI).',
    requestFields: [
      { name: 'from', type: 'string', desc: 'App ticker.' },
      { name: 'to', type: 'string', desc: 'App ticker.' },
      { name: 'amount', type: 'string', desc: 'Input amount.' },
      { name: 'idempotencyKey', type: 'string', desc: 'Client Guid.' },
    ],
    requestExample: `{
  "from": "BTC",
  "to": "USD",
  "amount": "0.01",
  "idempotencyKey": "…"
}`,
    responseExample: `{ "ID": "tx_…", "STATUS": "accepted" }`,
    statuses: [{ code: '202|200', behavior: 'MoneyMoveDto; settle via stream when live.' }],
  },

  // ── Public quotes ──
  {
    id: 'publicTest',
    method: 'INVOKE',
    path: 'IPublicQuoteService.TestConnectionAsync',
    host: 'PublicApiClient · api.cipherbank.money',
    section: 'Public quotes',
    summary: 'POST /test connectivity probe.',
    requestEmpty: true,
    requestExample: '{}',
    responseExample: `{ "ok": true }`,
    statuses: [
      { code: 'true', behavior: '2xx from public API.' },
      { code: 'false', behavior: 'Non-success.' },
    ],
  },
  {
    id: 'publicCurrencies',
    method: 'INVOKE',
    path: 'IPublicQuoteService.GetCurrenciesAsync',
    host: 'PublicApiClient · api.cipherbank.money',
    section: 'Public quotes',
    summary: 'POST /currencies → map BITCOIN/MONERO/USD to app tickers via CurrencySymbolMap.',
    requestEmpty: true,
    requestExample: '{}',
    responseExample: `{ "currencies": ["BTC", "XMR", "USD"] }`,
    statuses: [{ code: 'ok', behavior: 'Ordered app symbols.' }],
  },
  {
    id: 'publicIquote',
    method: 'INVOKE',
    path: 'IPublicQuoteService.GetInverseQuoteAsync',
    host: 'PublicApiClient · api.cipherbank.money',
    section: 'Public quotes',
    summary: 'POST /iquote — fixed input → output. Used by Convert.LockQuoteAsync (indicative).',
    requestFields: [
      { name: 'inputSymbol', type: 'string', desc: 'App ticker e.g. BTC.' },
      { name: 'inputAmount', type: 'decimal', desc: 'Input amount.' },
      { name: 'outputSymbol', type: 'string', desc: 'App ticker e.g. USD.' },
    ],
    requestExample: `{
  "INPUT_CURRENCY": "BITCOIN",
  "INPUT_AMOUNT": 0.0015,
  "OUTPUT_CURRENCY": "USD"
}`,
    responseExample: `{
  "inputSymbol": "BTC",
  "inputAmount": 0.0015,
  "outputSymbol": "USD",
  "outputAmount": 100.0,
  "rate": 66666.66666667
}`,
    statuses: [
      { code: 'ok', behavior: 'PublicQuote returned.' },
      { code: '422|424', behavior: 'Business / dependency errors from public API.' },
    ],
  },
  {
    id: 'publicQuote',
    method: 'INVOKE',
    path: 'IPublicQuoteService.GetQuoteAsync',
    host: 'PublicApiClient · api.cipherbank.money',
    section: 'Public quotes',
    summary: 'POST /quote — fixed output → required input.',
    requestExample: `{
  "INPUT_CURRENCY": "BITCOIN",
  "OUTPUT_AMOUNT": 100.0,
  "OUTPUT_CURRENCY": "USD"
}`,
    responseExample: `{
  "inputAmount": 0.0015,
  "outputAmount": 100.0,
  "rate": 66666.66666667
}`,
    statuses: [{ code: 'ok', behavior: 'PublicQuote returned.' }],
  },
  {
    id: 'currencyMap',
    method: 'INVOKE',
    path: 'CurrencySymbolMap.ToApiCurrency | ToAppSymbol',
    host: 'CipherBank-app.Core · Services',
    section: 'Public quotes',
    summary: 'BTC↔BITCOIN, XMR↔MONERO, USD↔USD at the public API boundary.',
    requestExample: `{ "appSymbol": "BTC" }`,
    responseExample: `{ "apiCurrency": "BITCOIN" }`,
    statuses: [
      { code: 'ok', behavior: 'Mapped.' },
      { code: 'throw', behavior: 'Unsupported symbol.' },
    ],
  },

  // ── Stream & prefs ──
  {
    id: 'streamConnect',
    method: 'INVOKE',
    path: 'ClientWebSocketStreamService.ConnectAsync',
    host: 'CipherBank-app.Core · V1',
    section: 'Stream & prefs',
    summary: 'Disconnect prior socket (if any), then connect WSS and start receive loop.',
    requestEmpty: true,
    requestExample: '{}',
    responseEmpty: true,
    responseExample: '{}',
    logic: 'Always DisconnectAsync before new ConnectAsync to avoid leaked sockets on reconnect.',
    statuses: [{ code: 'ok', behavior: 'IsConnected; events fan out via StreamHub.' }],
  },
  {
    id: 'prefsLoadSave',
    method: 'INVOKE',
    path: 'PrefsStore.LoadAsync | SaveAsync',
    host: 'CipherBank-app.Core · Persist',
    section: 'Stream & prefs',
    summary: 'Local UserPrefs JSON including EnabledCurrencies, DefaultSendSpeed, Cora, idle, appearance.',
    requestExample: `{
  "enabledCurrencies": ["BTC", "XMR", "USD"],
  "defaultSendSpeed": "instant",
  "lockIdleSeconds": 120,
  "coraEnabled": true
}`,
    responseExample: `{ "saved": true }`,
    statuses: [{ code: 'ok', behavior: 'Normalized prefs in SQLite.' }],
  },
  {
    id: 'bootstrapApply',
    method: 'INVOKE',
    path: 'AccountBootstrapService.ApplyAsync',
    host: 'CipherBank-app.Core · V1',
    section: 'Stream & prefs',
    summary: 'GET /account/bootstrap → merge prefs + upsert recipients with stable SHA256 ids (no Guid churn).',
    requestEmpty: true,
    requestExample: '{}',
    responseExample: `{
  "recipientsUpserted": 2,
  "idExample": "bootstrap_a1b2c3d4e5f67890"
}`,
    logic: 'ResolvedId prefers wire id; else SHA256(name|last4|routing) prefix. Never touches custody.',
    statuses: [{ code: 'ok', behavior: 'Local prefs/recipients updated.' }],
  },

  // ── UI flows ──
  {
    id: 'setPinSeal',
    method: 'INVOKE',
    path: 'SetPinViewModel.SealAsync',
    host: 'CipherBank-app · ViewModels',
    section: 'UI flows',
    summary: 'Validate PIN≥6 + match + BIP39 → FinishCustodySetupAsync → //HomePage.',
    requestExample: `{
  "pin": "••••••",
  "confirmPin": "••••••",
  "mnemonic": "<from query>"
}`,
    responseExample: `{ "navigated": "//HomePage" }`,
    statuses: [
      { code: 'ok', behavior: 'Wallet sealed; Home shown.' },
      { code: 'error', behavior: 'Inline Error = exception message (e.g. Sqlite type-init if natives wrong).' },
    ],
  },
  {
    id: 'convertLockQuote',
    method: 'INVOKE',
    path: 'ConvertViewModel.LockQuoteAsync',
    host: 'CipherBank-app · ViewModels',
    section: 'UI flows',
    summary: 'Indicative lock via public /iquote; maps to QuoteDto with client TTL; starts countdown.',
    requestExample: `{ "fromAsset": "BTC", "toAsset": "USD", "amount": "0.01" }`,
    responseExample: `{
  "isIndicative": true,
  "rateText": "1 BTC = 65000 USD",
  "hasValidLock": true
}`,
    logic: 'GetInverseQuoteAsync → IndicativeQuoteMapper.ToQuoteDto → StartCountdown. Settlement still IProductApi.ConvertAsync.',
    statuses: [
      { code: 'ok', behavior: 'Indicative quote shown.' },
      { code: 'alert', behavior: 'Amount / Quote dialogs on failure.' },
    ],
  },
  {
    id: 'convertSettle',
    method: 'INVOKE',
    path: 'ConvertViewModel.ConvertAsync',
    host: 'CipherBank-app · ViewModels',
    section: 'UI flows',
    summary: 'Require unlocked + step-up Convert + fresh indicative quote → product ConvertAsync.',
    requestExample: `{ "fromAsset": "BTC", "toAsset": "USD", "amount": "0.01" }`,
    responseExample: `{ "status": "Convert tx_…: accepted" }`,
    statuses: [
      { code: 'ok', behavior: 'Alert with move status.' },
      { code: 'blocked', behavior: 'Locked / step-up cancel / stale quote.' },
    ],
  },
  {
    id: 'homeAppearing',
    method: 'INVOKE',
    path: 'HomeViewModel.AppearingAsync',
    host: 'CipherBank-app · ViewModels',
    section: 'UI flows',
    summary: 'Touch idle; load portfolio, local wallets, charts; respect EnabledCurrencies / values-hidden prefs.',
    requestEmpty: true,
    requestExample: '{}',
    responseExample: `{ "totalUsd": 128432.19, "sections": ["holdings", "localWallets"] }`,
    statuses: [{ code: 'ok', behavior: 'Home bound; stream soft-refresh hooked.' }],
  },
  {
    id: 'profileSave',
    method: 'INVOKE',
    path: 'ProfileViewModel.SavePrefsAsync',
    host: 'CipherBank-app · ViewModels',
    section: 'UI flows',
    summary: 'Persist appearance, base currency, send speed, enabled currencies, idle; push prefs sync.',
    requestExample: `{
  "appearance": "dark",
  "baseCurrency": "USD",
  "defaultSendSpeed": "instant",
  "enabledCurrencies": ["BTC", "XMR", "USD"],
  "lockIdleSeconds": 120
}`,
    responseExample: `{ "saved": true, "cloudSynced": true }`,
    statuses: [{ code: 'ok', behavior: 'Alert Saved; IdleMs updated on session.' }],
  },

  // ── Cora UI ──
  {
    id: 'coraFab',
    method: 'INVOKE',
    path: 'CoraFab (ScreenKey)',
    host: 'CipherBank-app · Controls',
    section: 'Cora UI',
    summary: 'Floating assistant; line from CoraLines.For(ScreenKey); visibility from CoraEnabled pref.',
    requestExample: `{ "screenKey": "home" }`,
    responseExample: `{ "line": "…" }`,
    statuses: [{ code: 'ok', behavior: 'Tap toggles speech bubble.' }],
  },
  {
    id: 'coraBar',
    method: 'INVOKE',
    path: 'CoraBar (ScreenKey | Line)',
    host: 'CipherBank-app · Controls',
    section: 'Cora UI',
    summary: 'Inline Expo-parity Cora strip beside FAB on money tabs; same line source / pref gate.',
    requestExample: `{ "screenKey": "convert", "line": null }`,
    responseExample: `{ "visible": true, "text": "…" }`,
    statuses: [{ code: 'ok', behavior: 'Bar shown when CoraEnabled.' }],
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
						<tr><th scope="row">Product API</th><td><code>IProductApi</code> → mock or <code>HttpProductApi</code> (<code>/v1</code>)</td></tr>
						<tr><th scope="row">Public quotes</th><td><code>IPublicQuoteService</code> → <code>PublicApiClient</code> (<code>api.cipherbank.money</code>)</td></tr>
						<tr><th scope="row">Convert lock</th><td>Indicative via <code>POST /iquote</code>; settle via product <code>ConvertAsync</code></td></tr>
						<tr><th scope="row">SQLite Android</th><td>Ship <code>SQLitePCLRaw.lib.e_sqlite3.android</code> only — exclude desktop natives from APK</td></tr>
						<tr><th scope="row">Idle lock</th><td><code>AppIdleLockService</code> → <code>AppSession.Lock</code> + <code>IPqChannel.Clear</code> → Unlock</td></tr>
					</tbody>
				</table>
			</section>

			<section class="endpoint" id="call-graph" aria-labelledby="call-graph-title">
				<h2 id="call-graph-title">Unlock / seal call graph</h2>
				<pre><code>Splash (≥900ms) + Boot
  → Welcome → Keys → BackupQuiz → SetPin.Seal
       → FinishCustodySetup → Seal → SeedWallets → CompleteUnlock(no bootstrap)
  → Unlock.PIN|Biometrics
       → Unlock* → CompleteUnlock(bootstrap)
            → CreateSession (Lab|A1|A2 proof)
            → Stream.Connect (disconnect-first) + Hub.Start
            → PrefsSync.PullMerge [+ AccountBootstrap]
  → Home

Idle / Profile.Lock → AppSession.Lock → PQ Clear → Unlock</code></pre>
			</section>

			<section class="endpoint" id="never-wire" aria-labelledby="never-wire-title">
				<h2 id="never-wire-title">Never on the wire</h2>
				<p>Mnemonic, BIP39 entropy, PIN plaintext, device secret, account private key, spend keys, PAN/CVV, full ACH account numbers. Public/product APIs receive public keys, sealed ciphertexts, handles, and masked last4 only.</p>
			</section>

${articles}

		</main>
	</div>
	<footer>CipherBank MAUI Function Reference · PR #16 feat/cora-redesign-maui · regenerate: <code>node docs/scripts/generate-maui-function-ref.mjs</code></footer>
</body>
</html>
`;

fs.writeFileSync(outRoot, html);
fs.writeFileSync(outDocs, html);
console.log('Wrote', outRoot);
console.log('Wrote', outDocs);
console.log('Invokes:', invokables.length);
