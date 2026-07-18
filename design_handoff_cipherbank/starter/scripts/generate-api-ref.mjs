#!/usr/bin/env node
/**
 * Generates docs/CB_FullAPIRef.html in the CB_InitialAPIRef style,
 * covering public PriceCache + product /v1 + stream.
 */
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const outPath = path.join(__dirname, '../docs/CB_FullAPIRef.html');
const handoffOut = path.join(__dirname, '../../CB_FullAPIRef.html');

/** @typedef {{ name: string, type: string, desc: string }} Field */
/** @typedef {{
 *  id: string, method: string, path: string, host: string, summary: string,
 *  requestFields?: Field[], requestExample?: string, requestEmpty?: boolean,
 *  responseFields?: Field[], responseExample?: string, responseEmpty?: boolean,
 *  responseNote?: string,
 *  statuses: { code: string, behavior: string }[],
 *  section?: string
 * }} Endpoint */

/** @type {Endpoint[]} */
const endpoints = [
  // ── Public PriceCache ──
  {
    id: 'postTest',
    method: 'POST',
    path: '/test',
    host: 'api.cipherbank.money',
    section: 'Public PriceCache',
    summary: 'Confirms that the public HTTP API can accept and return a request.',
    requestEmpty: true,
    requestExample: '{}',
    responseEmpty: true,
    responseExample: '{}',
    statuses: [{ code: '200', behavior: 'The API accepted the connectivity test.' }],
  },
  {
    id: 'postCurrencies',
    method: 'POST',
    path: '/currencies',
    host: 'api.cipherbank.money',
    section: 'Public PriceCache',
    summary: 'Returns the currencies currently supported by CipherBank.',
    requestEmpty: true,
    requestExample: '{}',
    responseFields: [
      { name: 'CURRENCIES', type: 'array', desc: 'Ordered currency codes currently available to public API customers.' },
    ],
    responseExample: `{
  "CURRENCIES": [
    "BITCOIN",
    "MONERO",
    "USD"
  ]
}`,
    statuses: [
      { code: '200', behavior: 'The supported currency list was returned.' },
      { code: '422', behavior: 'The wallet service returned an invalid result.' },
      { code: '424', behavior: 'The wallet dependency was unavailable.' },
    ],
  },
  {
    id: 'postIquote',
    method: 'POST',
    path: '/iquote',
    host: 'api.cipherbank.money',
    section: 'Public PriceCache',
    summary: 'Calculates the output amount produced by a requested input amount.',
    requestFields: [
      { name: 'INPUT_AMOUNT', type: 'number (double)', desc: 'Amount of the input currency the customer will provide.' },
      { name: 'INPUT_CURRENCY', type: 'string (currency-code)', desc: 'Currency the customer will provide.' },
      { name: 'OUTPUT_CURRENCY', type: 'string (currency-code)', desc: 'Currency in which to calculate the result.' },
    ],
    requestExample: `{
  "INPUT_AMOUNT": 0.0015,
  "INPUT_CURRENCY": "BITCOIN",
  "OUTPUT_CURRENCY": "USD"
}`,
    responseFields: [
      { name: 'INPUT_AMOUNT', type: 'number (double)', desc: 'Amount of the input currency required or supplied.' },
      { name: 'INPUT_CURRENCY', type: 'string (currency-code)', desc: 'Currency supplied by the customer.' },
      { name: 'OUTPUT_AMOUNT', type: 'number (double)', desc: 'Amount of the output currency requested or calculated.' },
      { name: 'OUTPUT_CURRENCY', type: 'string (currency-code)', desc: 'Currency returned to the customer.' },
    ],
    responseExample: `{
  "INPUT_AMOUNT": 0.0015,
  "INPUT_CURRENCY": "BITCOIN",
  "OUTPUT_AMOUNT": 100.0,
  "OUTPUT_CURRENCY": "USD"
}`,
    statuses: [
      { code: '200', behavior: 'A quote was calculated.' },
      { code: '406', behavior: 'The Accept header does not permit JSON.' },
      { code: '415', behavior: 'A non-empty body was not sent as application/json.' },
      { code: '417', behavior: 'The JSON body could not be parsed or did not match the field types.' },
      { code: '422', behavior: 'The inverse quote request or downstream quote result was invalid.' },
      { code: '424', behavior: 'The price-cache dependency was unavailable.' },
    ],
  },
  {
    id: 'postQuote',
    method: 'POST',
    path: '/quote',
    host: 'api.cipherbank.money',
    section: 'Public PriceCache',
    summary: 'Calculates the input amount required to produce a requested output amount.',
    requestFields: [
      { name: 'INPUT_CURRENCY', type: 'string (currency-code)', desc: 'Currency the customer will provide.' },
      { name: 'OUTPUT_AMOUNT', type: 'number (double)', desc: 'Amount of the requested output currency to receive.' },
      { name: 'OUTPUT_CURRENCY', type: 'string (currency-code)', desc: 'Currency the customer wants to receive.' },
    ],
    requestExample: `{
  "INPUT_CURRENCY": "BITCOIN",
  "OUTPUT_AMOUNT": 100.0,
  "OUTPUT_CURRENCY": "USD"
}`,
    responseFields: [
      { name: 'INPUT_AMOUNT', type: 'number (double)', desc: 'Amount of the input currency required or supplied.' },
      { name: 'INPUT_CURRENCY', type: 'string (currency-code)', desc: 'Currency supplied by the customer.' },
      { name: 'OUTPUT_AMOUNT', type: 'number (double)', desc: 'Amount of the output currency requested or calculated.' },
      { name: 'OUTPUT_CURRENCY', type: 'string (currency-code)', desc: 'Currency returned to the customer.' },
    ],
    responseExample: `{
  "INPUT_AMOUNT": 0.0015,
  "INPUT_CURRENCY": "BITCOIN",
  "OUTPUT_AMOUNT": 100.0,
  "OUTPUT_CURRENCY": "USD"
}`,
    statuses: [
      { code: '200', behavior: 'A quote was calculated.' },
      { code: '406', behavior: 'The Accept header does not permit JSON.' },
      { code: '415', behavior: 'A non-empty body was not sent as application/json.' },
      { code: '417', behavior: 'The JSON body could not be parsed or did not match the field types.' },
      { code: '422', behavior: 'The quote request or downstream quote result was invalid.' },
      { code: '424', behavior: 'The price-cache dependency was unavailable.' },
    ],
  },

  // ── Session ──
  {
    id: 'postSession',
    method: 'POST',
    path: '/session',
    host: 'api.cipherbank.dev · /v1',
    section: 'Session',
    summary: 'Creates an authenticated device session. Custody keys never leave the device.',
    requestFields: [
      { name: 'DEVICE_BOUND', type: 'boolean', desc: 'True when the session is bound to this device attestation.' },
      { name: 'DEVICE_ID', type: 'string', desc: 'Optional stable device identifier.' },
      { name: 'DEVICE_ATTESTATION', type: 'string', desc: 'Optional OS attestation / CDCVM proof.' },
    ],
    requestExample: `{
  "DEVICE_BOUND": true,
  "DEVICE_ID": "dev_android_lab_01"
}`,
    responseFields: [
      { name: 'TOKEN', type: 'string', desc: 'Bearer access token.' },
      { name: 'REFRESH_TOKEN', type: 'string', desc: 'Refresh token for rotation.' },
      { name: 'EXPIRES_AT', type: 'integer (int64)', desc: 'Access token expiry epoch milliseconds.' },
      { name: 'USER_ID', type: 'string', desc: 'CipherBank user identifier.' },
    ],
    responseExample: `{
  "TOKEN": "eyJ…",
  "REFRESH_TOKEN": "rt_…",
  "EXPIRES_AT": 1720903600000,
  "USER_ID": "user_cora"
}`,
    statuses: [
      { code: '200', behavior: 'Session issued.' },
      { code: '417', behavior: 'Body parse/type failure.' },
      { code: '422', behavior: 'Attestation or device bind rejected.' },
    ],
  },
  {
    id: 'postSessionRefresh',
    method: 'POST',
    path: '/session/refresh',
    host: 'api.cipherbank.dev · /v1',
    section: 'Session',
    summary: 'Rotates access and refresh tokens.',
    requestFields: [{ name: 'REFRESH_TOKEN', type: 'string', desc: 'Previously issued refresh token.' }],
    requestExample: `{ "REFRESH_TOKEN": "rt_…" }`,
    responseFields: [
      { name: 'TOKEN', type: 'string', desc: 'New access token.' },
      { name: 'REFRESH_TOKEN', type: 'string', desc: 'New refresh token.' },
      { name: 'EXPIRES_AT', type: 'integer (int64)', desc: 'Expiry epoch ms.' },
    ],
    responseExample: `{
  "TOKEN": "eyJ…",
  "REFRESH_TOKEN": "rt_…",
  "EXPIRES_AT": 1720907200000
}`,
    statuses: [
      { code: '200', behavior: 'Tokens rotated.' },
      { code: '401', behavior: 'Refresh token invalid or revoked.' },
      { code: '417', behavior: 'Body parse/type failure.' },
    ],
  },

  // ── Portfolio / assets ──
  {
    id: 'getPortfolio',
    method: 'GET',
    path: '/portfolio',
    host: 'api.cipherbank.dev · /v1',
    section: 'Portfolio',
    summary: 'Aggregated holdings for Home. Empty for clean installs until funded.',
    requestEmpty: true,
    responseFields: [
      { name: 'TOTAL', type: 'number (double)', desc: 'Portfolio total in display currency (USD).' },
      { name: 'CHANGE_24H', type: 'object', desc: 'Keys AMOUNT (double), PCT (double).' },
      { name: 'HOLDINGS', type: 'array', desc: 'Holding rows (SYMBOL, NAME, GLYPH, TYPE, AMOUNT string, USD_VALUE, CHANGE_24H, WALLETS?).' },
    ],
    responseExample: `{
  "TOTAL": 0,
  "CHANGE_24H": { "AMOUNT": 0, "pct": 0 },
  "HOLDINGS": []
}`,
    responseNote: 'Nested holding fields also use SCREAMING_SNAKE on the wire (SYMBOL, USD_VALUE, WALLETS, …).',
    statuses: [
      { code: '200', behavior: 'Portfolio returned.' },
      { code: '401', behavior: 'Missing or invalid Bearer token.' },
    ],
  },
  {
    id: 'getAssets',
    method: 'GET',
    path: '/assets',
    host: 'api.cipherbank.dev · /v1',
    section: 'Portfolio',
    summary: 'Asset catalog (crypto, fiat, securities). Securities may be ENABLED false.',
    requestEmpty: true,
    responseFields: [{ name: 'ASSETS', type: 'array', desc: 'Catalog rows: SYMBOL, NAME, GLYPH, TYPE, DECIMALS, ENABLED, BADGE?, NOTE?.' }],
    responseExample: `{
  "ASSETS": [
    { "SYMBOL": "BTC", "NAME": "Bitcoin", "GLYPH": "₿", "TYPE": "crypto", "DECIMALS": 8, "ENABLED": true }
  ]
}`,
    statuses: [{ code: '200', behavior: 'Catalog returned.' }],
  },

  // ── Prefs / bootstrap ──
  {
    id: 'getPrefs',
    method: 'GET',
    path: '/prefs',
    host: 'api.cipherbank.dev · /v1',
    section: 'Profile & sync',
    summary: 'User preferences for Home layout, privacy, Cora, currencies, and app lock.',
    requestEmpty: true,
    responseFields: [
      { name: 'HOME_ORDER', type: 'array', desc: 'Ordered Home section ids.' },
      { name: 'HOME_VISIBLE', type: 'object', desc: 'Section visibility map.' },
      { name: 'VALUES_HIDDEN_ON_LAUNCH', type: 'boolean', desc: 'Hide balances until reveal.' },
      { name: 'CORA_ENABLED', type: 'boolean', desc: 'Show Cora copy.' },
      { name: 'DEFAULT_SEND_SPEED', type: 'string', desc: 'instant | ach.' },
      { name: 'APPEARANCE', type: 'string', desc: 'dark | light.' },
      { name: 'BASE_CURRENCY', type: 'string', desc: 'Display currency ticker.' },
      { name: 'ENABLED_CURRENCIES', type: 'array', desc: 'Enabled asset tickers.' },
      { name: 'APP_LOCK_IDLE_SEC', type: 'integer (int64)', desc: 'Idle lock seconds.' },
    ],
    responseExample: `{
  "CORA_ENABLED": true,
  "DEFAULT_SEND_SPEED": "instant",
  "APPEARANCE": "dark",
  "BASE_CURRENCY": "USD",
  "ENABLED_CURRENCIES": ["BTC", "XMR", "USD"]
}`,
    statuses: [{ code: '200', behavior: 'Prefs returned.' }],
  },
  {
    id: 'putPrefs',
    method: 'PUT',
    path: '/prefs',
    host: 'api.cipherbank.dev · /v1',
    section: 'Profile & sync',
    summary: 'Merges partial or full preference document.',
    requestFields: [{ name: '(any pref key)', type: 'mixed', desc: 'Partial SCREAMING_SNAKE prefs body.' }],
    requestExample: `{ "CORA_ENABLED": false, "BASE_CURRENCY": "USD" }`,
    responseNote: 'Returns the full merged prefs document.',
    responseExample: `{ "CORA_ENABLED": false, "BASE_CURRENCY": "USD", "APPEARANCE": "dark" }`,
    statuses: [
      { code: '200', behavior: 'Prefs merged.' },
      { code: '417', behavior: 'Body parse/type failure.' },
      { code: '422', behavior: 'Invalid enum or currency.' },
    ],
  },
  {
    id: 'getAccountBootstrap',
    method: 'GET',
    path: '/account/bootstrap',
    host: 'api.cipherbank.dev · /v1',
    section: 'Profile & sync',
    summary: 'Returning-user metadata pull: prefs + public ACH recipients (never seed or full account numbers).',
    requestEmpty: true,
    responseFields: [
      { name: 'PREFS', type: 'object', desc: 'Optional partial prefs merge.' },
      { name: 'RECIPIENTS', type: 'array', desc: 'Public payees: ID, DISPLAY_NAME, ACCOUNT_HOLDER_NAME, BANK_NAME, ACCOUNT_LAST4, ACCOUNT_TYPE, ROUTING_NUMBER, RAIL, HANDLE, INITIALS.' },
      { name: 'SYNCED_AT', type: 'integer (int64)', desc: 'Server sync timestamp ms.' },
    ],
    responseExample: `{
  "PREFS": { "DEFAULT_SEND_SPEED": "instant" },
  "RECIPIENTS": [
    {
      "ID": "maya",
      "DISPLAY_NAME": "Maya Chen",
      "ACCOUNT_HOLDER_NAME": "Maya Chen",
      "BANK_NAME": "Chase",
      "ACCOUNT_LAST4": "4021",
      "ACCOUNT_TYPE": "checking",
      "ROUTING_NUMBER": "021000021",
      "RAIL": "ACH",
      "HANDLE": "maya@cipherbank.id",
      "INITIALS": "MC"
    }
  ],
  "SYNCED_AT": 1720900000000
}`,
    statuses: [{ code: '200', behavior: 'Bootstrap payload returned (may be empty recipients).' }],
  },
  {
    id: 'getRecipients',
    method: 'GET',
    path: '/recipients',
    host: 'api.cipherbank.dev · /v1',
    section: 'Profile & sync',
    summary: 'Cloud recipient directory (public fields). On-device SQLite may hold full account numbers separately.',
    requestEmpty: true,
    responseFields: [{ name: 'RECIPIENTS', type: 'array', desc: 'Public recipient rows.' }],
    responseExample: `{ "RECIPIENTS": [] }`,
    statuses: [{ code: '200', behavior: 'List returned.' }],
  },
  {
    id: 'postRecipients',
    method: 'POST',
    path: '/recipients',
    host: 'api.cipherbank.dev · /v1',
    section: 'Profile & sync',
    summary: 'Creates or updates a cloud recipient (public fields only on wire).',
    requestFields: [
      { name: 'DISPLAY_NAME', type: 'string', desc: 'Payee display name.' },
      { name: 'ACCOUNT_HOLDER_NAME', type: 'string', desc: 'Legal account name.' },
      { name: 'ROUTING_NUMBER', type: 'string', desc: 'ABA routing (9 digits).' },
      { name: 'ACCOUNT_LAST4', type: 'string', desc: 'Last four only — never full account number.' },
      { name: 'ACCOUNT_TYPE', type: 'string', desc: 'checking | savings.' },
      { name: 'RAIL', type: 'string', desc: 'ACH | wire | rtp.' },
    ],
    requestExample: `{
  "DISPLAY_NAME": "Maya Chen",
  "ACCOUNT_HOLDER_NAME": "Maya Chen",
  "ROUTING_NUMBER": "021000021",
  "ACCOUNT_LAST4": "4021",
  "ACCOUNT_TYPE": "checking",
  "RAIL": "ACH"
}`,
    responseExample: `{ "ID": "rcp_…", "DISPLAY_NAME": "Maya Chen", "ACCOUNT_LAST4": "4021" }`,
    statuses: [
      { code: '200', behavior: 'Recipient upserted.' },
      { code: '417', behavior: 'Body parse/type failure.' },
      { code: '422', behavior: 'Validation failed.' },
    ],
  },
  {
    id: 'postBanksLink',
    method: 'POST',
    path: '/banks/link',
    host: 'api.cipherbank.dev · /v1',
    section: 'Profile & sync',
    summary: 'Starts or completes institution link (Plaid-style). Later phase.',
    requestFields: [{ name: 'PROVIDER_TOKEN', type: 'string', desc: 'Link provider public token.' }],
    requestExample: `{ "PROVIDER_TOKEN": "link-sandbox-…" }`,
    responseFields: [
      { name: 'LINKED', type: 'boolean', desc: 'Whether link succeeded.' },
      { name: 'BANK_ID', type: 'string', desc: 'Linked institution id.' },
      { name: 'LAST4', type: 'string', desc: 'Masked account last4.' },
    ],
    responseExample: `{ "LINKED": true, "BANK_ID": "bank_…", "LAST4": "4021" }`,
    statuses: [
      { code: '200', behavior: 'Bank linked.' },
      { code: '422', behavior: 'Provider token invalid.' },
    ],
  },

  // ── History ──
  {
    id: 'getHistory',
    method: 'GET',
    path: '/history',
    host: 'api.cipherbank.dev · /v1',
    section: 'Market & charts',
    summary: 'Bulk OHLC / series for Home charts. Query: RANGE, GRANULARITY, SYMBOLS, FROM, TO (as query string keys screaming or lower — prefer RANGE=&GRANULARITY=&SYMBOLS=&FROM=&TO=).',
    requestEmpty: true,
    responseFields: [
      { name: 'SERIES', type: 'array', desc: 'Series rows: LABEL, SYMBOL, GRANULARITY, POINTS[{ T, V, O?, H?, L?, C? }].' },
      { name: 'META', type: 'object', desc: 'SOURCE, GENERATED_AT.' },
    ],
    responseExample: `{
  "SERIES": [
    { "LABEL": "Wallet", "SYMBOL": "WALLET", "GRANULARITY": "1h", "POINTS": [{ "T": 1720900000, "V": 100000, "O": 99, "H": 101, "L": 98, "C": 100 }] }
  ],
  "META": { "SOURCE": "cipherbank", "GENERATED_AT": 1720900000000 }
}`,
    statuses: [
      { code: '200', behavior: 'Series returned.' },
      { code: '422', behavior: 'Invalid range or symbols.' },
    ],
  },

  // ── Money ──
  {
    id: 'postConvert',
    method: 'POST',
    path: '/convert',
    host: 'api.cipherbank.dev · /v1',
    section: 'Money movement',
    summary: 'Executes a convert against a locked quote. Requires Idempotency-Key. Pricing via public /iquote beforehand.',
    requestFields: [
      { name: 'QUOTE_ID', type: 'string', desc: 'Client or server lock id from pricing step.' },
      { name: 'AMOUNT', type: 'string', desc: 'Input amount in asset units (decimal string).' },
      { name: 'INPUT_CURRENCY', type: 'string (currency-code)', desc: 'Optional public code echo (BITCOIN).' },
      { name: 'OUTPUT_CURRENCY', type: 'string (currency-code)', desc: 'Optional public code echo.' },
    ],
    requestExample: `{
  "QUOTE_ID": "q_…",
  "AMOUNT": "0.0015",
  "INPUT_CURRENCY": "BITCOIN",
  "OUTPUT_CURRENCY": "USD"
}`,
    responseFields: [
      { name: 'TX_ID', type: 'string', desc: 'Convert transaction id.' },
      { name: 'STATUS', type: 'string', desc: 'accepted | settled | failed.' },
    ],
    responseExample: `{ "TX_ID": "cvt_…", "STATUS": "accepted" }`,
    statuses: [
      { code: '200', behavior: 'Accepted; stream CONVERT.SETTLED follows.' },
      { code: '422', behavior: 'quote_expired or invalid quote.' },
    ],
  },
  {
    id: 'postTransfers',
    method: 'POST',
    path: '/transfers',
    host: 'api.cipherbank.dev · /v1',
    section: 'Money movement',
    summary: 'Send to a recipient (instant or ACH). Idempotency-Key required.',
    requestFields: [
      { name: 'RECIPIENT', type: 'string', desc: 'Recipient id or handle.' },
      { name: 'AMOUNT', type: 'string', desc: 'Send amount.' },
      { name: 'SOURCE', type: 'string', desc: 'Funding asset / wallet id.' },
      { name: 'SPEED', type: 'string', desc: 'instant | ach.' },
    ],
    requestExample: `{
  "RECIPIENT": "maya",
  "AMOUNT": "1200",
  "SOURCE": "USD",
  "SPEED": "ach"
}`,
    responseExample: `{ "TX_ID": "xfer_…", "STATUS": "accepted" }`,
    statuses: [
      { code: '200', behavior: 'Accepted; stream TRANSFER.SETTLED follows.' },
      { code: '422', behavior: 'insufficient_funds or recipient_unresolved.' },
    ],
  },
  {
    id: 'postPayments',
    method: 'POST',
    path: '/payments',
    host: 'api.cipherbank.dev · /v1',
    section: 'Money movement',
    summary: 'Multi-asset payment mix. Idempotency-Key required.',
    requestFields: [
      { name: 'RECIPIENT', type: 'string', desc: 'Payee id.' },
      { name: 'TOTAL', type: 'string', desc: 'Total due in quote currency.' },
      { name: 'SOURCES', type: 'array', desc: '[{ ASSET, VALUE }] funding legs.' },
    ],
    requestExample: `{
  "RECIPIENT": "sunset",
  "TOTAL": "2400",
  "SOURCES": [
    { "ASSET": "USD", "VALUE": "1200" },
    { "ASSET": "BTC", "VALUE": "0.02" }
  ]
}`,
    responseExample: `{ "PAYMENT_ID": "pay_…", "STATUS": "accepted" }`,
    statuses: [
      { code: '200', behavior: 'Accepted; stream PAYMENT.SETTLED follows.' },
      { code: '422', behavior: 'mix_undercovered when sources do not cover TOTAL.' },
    ],
  },
  {
    id: 'getActivity',
    method: 'GET',
    path: '/activity',
    host: 'api.cipherbank.dev · /v1',
    section: 'Money movement',
    summary: 'Unified activity feed with cursor pagination.',
    requestEmpty: true,
    responseFields: [
      { name: 'ITEMS', type: 'array', desc: 'ID, KIND, STATUS, TITLE, AMOUNT, ASSET, COUNTERPART, CREATED_AT.' },
      { name: 'NEXT_CURSOR', type: 'string', desc: 'Opaque cursor or null.' },
    ],
    responseExample: `{ "ITEMS": [], "NEXT_CURSOR": null }`,
    statuses: [{ code: '200', behavior: 'Page returned.' }],
  },

  // ── Wallets / receive ──
  {
    id: 'getWallets',
    method: 'GET',
    path: '/wallets',
    host: 'api.cipherbank.dev · /v1',
    section: 'Wallets & receive',
    summary: 'List server wallets. Optional query SYMBOL=XMR.',
    requestEmpty: true,
    responseFields: [{ name: 'WALLETS', type: 'array', desc: 'Server wallet rows.' }],
    responseExample: `{ "WALLETS": [] }`,
    statuses: [{ code: '200', behavior: 'List returned.' }],
  },
  {
    id: 'getWallet',
    method: 'GET',
    path: '/wallets/:id',
    host: 'api.cipherbank.dev · /v1',
    section: 'Wallets & receive',
    summary: 'Wallet detail including sync status.',
    requestEmpty: true,
    responseExample: `{
  "ID": "wal_xmr_1",
  "SYMBOL": "XMR",
  "LABEL": "Primary",
  "MODE": "unmanaged",
  "ADDRESS": "4…",
  "BALANCE": "0",
  "SYNC": { "HEIGHT": 3100400, "TARGET": 3100500, "STATE": "syncing" }
}`,
    statuses: [
      { code: '200', behavior: 'Wallet returned.' },
      { code: '404', behavior: 'not_found.' },
    ],
  },
  {
    id: 'postWallets',
    method: 'POST',
    path: '/wallets',
    host: 'api.cipherbank.dev · /v1',
    section: 'Wallets & receive',
    summary: 'Create managed / unmanaged / watch wallet. Unmanaged may send VIEW_KEY once. Never send spend key or mnemonic.',
    requestFields: [
      { name: 'MODE', type: 'string', desc: 'managed | unmanaged | watch.' },
      { name: 'SYMBOL', type: 'string', desc: 'Asset ticker (XMR).' },
      { name: 'LABEL', type: 'string', desc: 'Display label.' },
      { name: 'ADDRESS', type: 'string', desc: 'Required for unmanaged/watch.' },
      { name: 'VIEW_KEY', type: 'string', desc: 'Unmanaged view key (once).' },
      { name: 'RESTORE_HEIGHT', type: 'integer (int64)', desc: 'Optional scan height.' },
    ],
    requestExample: `{
  "MODE": "unmanaged",
  "SYMBOL": "XMR",
  "LABEL": "Phone",
  "ADDRESS": "4…",
  "VIEW_KEY": "…",
  "RESTORE_HEIGHT": 3100000
}`,
    responseExample: `{
  "WALLET_ID": "wal_xmr_…",
  "SYMBOL": "XMR",
  "MODE": "unmanaged",
  "ADDRESS": "4…",
  "VIEW_KEY_FINGERPRINT": "••••abcd",
  "SYNC": { "STATE": "syncing" }
}`,
    statuses: [
      { code: '200', behavior: 'Wallet created.' },
      { code: '400', behavior: 'custody_local_only if seed/spend key present.' },
      { code: '422', behavior: 'Missing address/view key for mode.' },
    ],
  },
  {
    id: 'postWalletRefresh',
    method: 'POST',
    path: '/wallets/:id/refresh',
    host: 'api.cipherbank.dev · /v1',
    section: 'Wallets & receive',
    summary: 'Kick wallet sync / balance refresh.',
    requestEmpty: true,
    requestExample: '{}',
    responseExample: `{ "ID": "wal_xmr_…", "SYNC": { "STATE": "synced", "HEIGHT": 3100500, "TARGET": 3100500 } }`,
    statuses: [
      { code: '200', behavior: 'Refresh accepted / completed.' },
      { code: '404', behavior: 'Wallet not found.' },
    ],
  },
  {
    id: 'getReceive',
    method: 'GET',
    path: '/receive/:asset',
    host: 'api.cipherbank.dev · /v1',
    section: 'Wallets & receive',
    summary: 'Receive handle / address / URI for an asset. Local-derived addresses may override for self-custody coins.',
    requestEmpty: true,
    responseFields: [
      { name: 'HANDLE', type: 'string', desc: 'Payment handle.' },
      { name: 'ADDRESS', type: 'string', desc: 'Chain address.' },
      { name: 'URI', type: 'string', desc: 'Payment URI.' },
      { name: 'QR', type: 'string', desc: 'QR payload (often same as URI).' },
    ],
    responseExample: `{
  "HANDLE": "cora@cipherbank.id",
  "ADDRESS": "bc1q…",
  "URI": "bitcoin:bc1q…",
  "QR": "bitcoin:bc1q…"
}`,
    statuses: [{ code: '200', behavior: 'Receive info returned.' }],
  },
  {
    id: 'postReceiveRequest',
    method: 'POST',
    path: '/receive/request',
    host: 'api.cipherbank.dev · /v1',
    section: 'Wallets & receive',
    summary: 'Optional server-issued amount request URI.',
    requestFields: [
      { name: 'ASSET', type: 'string', desc: 'Asset ticker.' },
      { name: 'AMOUNT', type: 'string', desc: 'Requested amount.' },
    ],
    requestExample: `{ "ASSET": "BTC", "AMOUNT": "0.01" }`,
    responseExample: `{
  "HANDLE": "cora@cipherbank.id",
  "ADDRESS": "bc1q…",
  "AMOUNT": "0.01",
  "URI": "bitcoin:bc1q…?amount=0.01",
  "QR": "bitcoin:bc1q…?amount=0.01"
}`,
    statuses: [{ code: '200', behavior: 'Request URI built.' }],
  },

  // ── Vault ──
  {
    id: 'getVaultBinaries',
    method: 'GET',
    path: '/vault/binaries',
    host: 'api.cipherbank.dev · /v1',
    section: 'Vault',
    summary: 'Server-held wallet binary metadata refs (never key material).',
    requestEmpty: true,
    responseFields: [{ name: 'BINARIES', type: 'array', desc: 'ID, LABEL, KIND, STATUS, CREATED_AT.' }],
    responseExample: `{ "BINARIES": [] }`,
    statuses: [{ code: '200', behavior: 'List returned.' }],
  },
  {
    id: 'postVaultBinaries',
    method: 'POST',
    path: '/vault/binaries',
    host: 'api.cipherbank.dev · /v1',
    section: 'Vault',
    summary: 'Register binary metadata. Idempotency-Key recommended.',
    requestFields: [
      { name: 'LABEL', type: 'string', desc: 'Display label.' },
      { name: 'KIND', type: 'string', desc: 'server_shard | backup_ref | …' },
    ],
    requestExample: `{ "LABEL": "Primary shard", "KIND": "server_shard" }`,
    responseExample: `{
  "ID": "bin_…",
  "LABEL": "Primary shard",
  "KIND": "server_shard",
  "STATUS": "active",
  "CREATED_AT": 1720900000000
}`,
    statuses: [{ code: '200', behavior: 'Binary registered.' }],
  },
  {
    id: 'getVaultCards',
    method: 'GET',
    path: '/vault/cards',
    host: 'api.cipherbank.dev · /v1',
    section: 'Vault',
    summary: 'Processor card tokens only — never PAN/CVV.',
    requestEmpty: true,
    responseFields: [{ name: 'CARDS', type: 'array', desc: 'ID, BRAND, LAST4, EXP_MONTH, EXP_YEAR, PROCESSOR_TOKEN, HARDWARE_TEST?, LABEL?, CREATED_AT.' }],
    responseExample: `{ "CARDS": [] }`,
    statuses: [{ code: '200', behavior: 'List returned.' }],
  },
  {
    id: 'postVaultCards',
    method: 'POST',
    path: '/vault/cards',
    host: 'api.cipherbank.dev · /v1',
    section: 'Vault',
    summary: 'Tokenize display card fields into a processor token.',
    requestFields: [
      { name: 'BRAND', type: 'string', desc: 'Visa | Mastercard | …' },
      { name: 'LAST4', type: 'string', desc: 'Last four digits.' },
      { name: 'EXP_MONTH', type: 'integer', desc: '1–12.' },
      { name: 'EXP_YEAR', type: 'integer', desc: 'Four-digit year.' },
      { name: 'LABEL', type: 'string', desc: 'Optional label.' },
    ],
    requestExample: `{
  "BRAND": "Visa",
  "LAST4": "4242",
  "EXP_MONTH": 12,
  "EXP_YEAR": 2030
}`,
    responseExample: `{
  "ID": "card_…",
  "BRAND": "Visa",
  "LAST4": "4242",
  "PROCESSOR_TOKEN": "tok_…",
  "CREATED_AT": 1720900000000
}`,
    statuses: [
      { code: '200', behavior: 'Card tokenized.' },
      { code: '400', behavior: 'custody_local_only if PAN/CVV present.' },
    ],
  },
  {
    id: 'postVaultCardDelete',
    method: 'POST',
    path: '/vault/cards/:id/delete',
    host: 'api.cipherbank.dev · /v1',
    section: 'Vault',
    summary: 'Deletes / revokes a card token.',
    requestEmpty: true,
    requestExample: '{}',
    responseExample: `{ "OK": true }`,
    statuses: [{ code: '200', behavior: 'Card removed.' }],
  },

  // ── POS ──
  {
    id: 'postPosSessions',
    method: 'POST',
    path: '/pos/sessions',
    host: 'api.cipherbank.dev · /v1',
    section: 'POS / tap-to-pay',
    summary: 'Creates a POS session (pending_auth). Idempotency-Key recommended.',
    requestFields: [
      { name: 'MERCHANT_ID', type: 'string', desc: 'Merchant / lab terminal id.' },
      { name: 'AMOUNT', type: 'string', desc: 'Ticket amount.' },
      { name: 'CURRENCY', type: 'string', desc: 'Ticket currency (USD).' },
      { name: 'LABEL', type: 'string', desc: 'Optional description.' },
    ],
    requestExample: `{
  "MERCHANT_ID": "merchant_lab",
  "AMOUNT": "42.50",
  "CURRENCY": "USD",
  "LABEL": "Coffee"
}`,
    responseExample: `{
  "SESSION_ID": "pos_…",
  "STATUS": "pending_auth",
  "AMOUNT": "42.50",
  "CURRENCY": "USD",
  "EXPIRES_AT": 1720900120000
}`,
    statuses: [{ code: '200', behavior: 'Session created.' }],
  },
  {
    id: 'postPosAuthorize',
    method: 'POST',
    path: '/pos/authorize',
    host: 'api.cipherbank.dev · /v1',
    section: 'POS / tap-to-pay',
    summary: 'Authorizes funding sources + card for presentment.',
    requestFields: [
      { name: 'SESSION_ID', type: 'string', desc: 'POS session id.' },
      { name: 'CARD_ID', type: 'string', desc: 'Vault card id.' },
      { name: 'SOURCES', type: 'array', desc: '[{ ASSET, VALUE }] funding.' },
      { name: 'DEVICE_ATTESTATION', type: 'string', desc: 'Unlock / CDCVM proof.' },
    ],
    requestExample: `{
  "SESSION_ID": "pos_…",
  "CARD_ID": "card_tok_nfc_bench_4242",
  "SOURCES": [{ "ASSET": "USD", "VALUE": "42.50" }],
  "DEVICE_ATTESTATION": "ok"
}`,
    responseExample: `{
  "SESSION_ID": "pos_…",
  "STATUS": "authorized",
  "EPHEMERAL_CARD_TOKEN_ID": "eph_…",
  "PRESENTMENT": { "TTL_MS": 60000, "TOKEN_REF": "…" }
}`,
    statuses: [
      { code: '200', behavior: 'Authorized for presentment.' },
      { code: '401', behavior: 'wallet_locked.' },
      { code: '409', behavior: 'pos_expired.' },
      { code: '422', behavior: 'insufficient_funds | test_card_required | mix_undercovered.' },
    ],
  },
  {
    id: 'postPosConfirm',
    method: 'POST',
    path: '/pos/confirm',
    host: 'api.cipherbank.dev · /v1',
    section: 'POS / tap-to-pay',
    summary: 'Marks session ready_to_present for NFC / simulate exchange.',
    requestFields: [{ name: 'SESSION_ID', type: 'string', desc: 'POS session id.' }],
    requestExample: `{ "SESSION_ID": "pos_…" }`,
    responseExample: `{ "SESSION_ID": "pos_…", "STATUS": "ready_to_present" }`,
    statuses: [
      { code: '200', behavior: 'Ready to present.' },
      { code: '409', behavior: 'pos_expired or not authorized.' },
    ],
  },
  {
    id: 'getPosSession',
    method: 'GET',
    path: '/pos/sessions/:id',
    host: 'api.cipherbank.dev · /v1',
    section: 'POS / tap-to-pay',
    summary: 'Poll POS session status.',
    requestEmpty: true,
    responseExample: `{
  "SESSION_ID": "pos_…",
  "STATUS": "ready_to_present",
  "AMOUNT": "42.50",
  "CURRENCY": "USD",
  "EXPIRES_AT": 1720900120000
}`,
    statuses: [
      { code: '200', behavior: 'Session returned.' },
      { code: '404', behavior: 'not_found.' },
    ],
  },
];

const css = fs.readFileSync(
  path.join(__dirname, '../docs/CB_InitialAPIRef.html'),
  'utf8',
).match(/<style>[\s\S]*?<\/style>/)?.[0] ?? '<style></style>';

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

function esc(s) {
  return String(s)
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;');
}

function renderEndpoint(ep) {
  const reqBody = ep.requestEmpty
    ? `<p class="empty">No JSON request body is required.</p>`
    : fieldsTable(ep.requestFields);
  const resBody = ep.responseEmpty
    ? `<p class="empty">The successful response has an empty JSON object body.</p>`
    : (ep.responseNote ? `<p>${esc(ep.responseNote)}</p>` : '') + fieldsTable(ep.responseFields ?? []);
  const statuses = ep.statuses
    .map((s) => `\n							<tr><th scope="row"><code>${esc(s.code)}</code></th><td>${esc(s.behavior)}</td></tr>`)
    .join('');

  return `
			<article class="endpoint" id="${esc(ep.id)}">
				<div class="route">
					<span class="method">${esc(ep.method)}</span>
					<h2><code>${esc(ep.path)}</code></h2>
				</div>
				<p>${esc(ep.summary)}</p>
				<p><strong>Host:</strong> <code>${esc(ep.host)}</code> · <strong>HTTP:</strong> 1.1 · <strong>Wire:</strong> SCREAMING_SNAKE_CASE JSON</p>

				<section aria-labelledby="${esc(ep.id)}-request">
					<h3 id="${esc(ep.id)}-request">Request JSON</h3>
					${reqBody}
					<h4>Example</h4>
					<pre><code>${esc(ep.requestExample ?? '{}')}</code></pre>
				</section>

				<section aria-labelledby="${esc(ep.id)}-response">
					<h3 id="${esc(ep.id)}-response">Successful response JSON</h3>
					${resBody}
					<h4>Example</h4>
					<pre><code>${esc(ep.responseExample ?? '{}')}</code></pre>
				</section>

				<section aria-labelledby="${esc(ep.id)}-statuses">
					<h3 id="${esc(ep.id)}-statuses">Status codes</h3>
					<table>
						<thead><tr><th scope="col">Status</th><th scope="col">Behavior</th></tr></thead>
						<tbody>${statuses}
						</tbody>
					</table>
				</section>
			</article>`;
}

const navItems = [
  `<li><a href="#json-runtime">JSON runtime</a></li>`,
  `<li><a href="#standards">Wire standards</a></li>`,
  `<li><a href="#stream">WSS /stream</a></li>`,
  ...endpoints.map(
    (ep) =>
      `<li><a href="#${esc(ep.id)}"><span aria-hidden="true">${esc(ep.method)}</span> ${esc(ep.path)}</a></li>`,
  ),
].join('\n\t\t\t\t');

const articles = endpoints.map(renderEndpoint).join('\n');

const html = `<!doctype html>
<html lang="en">
<head>
	<meta charset="utf-8">
	<meta name="viewport" content="width=device-width, initial-scale=1">
	<title>CipherBank Full API Reference</title>
	${css}
</head>
<body>
	<header>
		<h1>CipherBank Full API Reference</h1>
		<p>HTTP 1.1 JSON endpoints for the Cora Digital Teller app — public PriceCache plus product <code>/v1</code> — using the same SCREAMING_SNAKE_CASE wire standard as the initial public runtime contracts.</p>
		<p>
			<a href="./CB_InitialAPIRef.html">Initial public API (PriceCache)</a>
			·
			<a href="../src/mocks/API_CONTRACT.md">Markdown contract</a>
			·
			<a href="./PUBLIC_API.md">Public standards notes</a>
		</p>
	</header>
	<div class="layout">
		<nav aria-label="Endpoint navigation">
			<h2>Endpoints</h2>
			<ul>
				${navItems}
			</ul>
		</nav>
		<main id="main">
			<section class="endpoint" id="json-runtime" aria-labelledby="json-runtime-title">
				<h2 id="json-runtime-title">JSON value representations</h2>
				<p>Field types below use these JSON representations and limits.</p>
				<div class="representation-grid">
					<section class="representation-card" aria-labelledby="representation-int64">
						<h3 id="representation-int64">integer (int64)</h3>
						<dl>
							<dt>Minimum</dt><dd><code>-9223372036854775808</code></dd>
							<dt>Maximum</dt><dd><code>9223372036854775807</code></dd>
						</dl>
					</section>
					<section class="representation-card" aria-labelledby="representation-uint64">
						<h3 id="representation-uint64">integer (uint64)</h3>
						<dl>
							<dt>Minimum</dt><dd><code>0</code></dd>
							<dt>Maximum</dt><dd><code>18446744073709551615</code></dd>
						</dl>
					</section>
					<section class="representation-card" aria-labelledby="representation-double">
						<h3 id="representation-double">number (double)</h3>
						<dl>
							<dt>Minimum</dt><dd><code>-1.7976931348623157e+308</code></dd>
							<dt>Maximum</dt><dd><code>1.7976931348623157e+308</code></dd>
							<dt>Precision</dt><dd>15 decimal digits; 17 for round trips</dd>
						</dl>
					</section>
					<section class="representation-card" aria-labelledby="representation-string">
						<h3 id="representation-string">string (UTF-8)</h3>
						<dl>
							<dt>Encoding</dt><dd>UTF-8 Unicode</dd>
							<dt>Code unit</dt><dd>8 bits</dd>
						</dl>
					</section>
					<section class="representation-card" aria-labelledby="representation-boolean">
						<h3 id="representation-boolean">boolean</h3>
						<dl>
							<dt>Values</dt><dd><code>true</code> or <code>false</code></dd>
						</dl>
					</section>
				</div>
			</section>

			<section class="endpoint" id="standards" aria-labelledby="standards-title">
				<h2 id="standards-title">Wire standards (all surfaces)</h2>
				<table>
					<thead><tr><th scope="col">Rule</th><th scope="col">Value</th></tr></thead>
					<tbody>
						<tr><th scope="row">Field names</th><td><code>SCREAMING_SNAKE_CASE</code></td></tr>
						<tr><th scope="row">Public host</th><td><code>api.cipherbank.money</code> (no <code>/v1</code> prefix)</td></tr>
						<tr><th scope="row">Product host</th><td><code>api.cipherbank.dev/v1</code> · stream <code>wss://api.cipherbank.dev/v1/stream</code></td></tr>
						<tr><th scope="row">Auth</th><td><code>Authorization: Bearer</code> on product routes except <code>POST /session</code></td></tr>
						<tr><th scope="row">Content</th><td><code>Accept</code> + <code>Content-Type: application/json</code></td></tr>
						<tr><th scope="row">Money mutations</th><td><code>Idempotency-Key</code> header</td></tr>
						<tr><th scope="row">Public amounts</th><td>JSON <code>number (double)</code></td></tr>
						<tr><th scope="row">Product asset amounts</th><td>Decimal <code>string</code> where noted (AMOUNT legs)</td></tr>
						<tr><th scope="row">Currency codes (public)</th><td><code>BITCOIN</code>, <code>MONERO</code>, <code>USD</code></td></tr>
						<tr><th scope="row">Errors</th><td><code>{ "CODE", "MESSAGE", "DETAIL"? }</code></td></tr>
						<tr><th scope="row">Status vocabulary</th><td><code>406</code> Accept · <code>415</code> Content-Type · <code>417</code> parse/type · <code>422</code> business · <code>424</code> dependency</td></tr>
						<tr><th scope="row">Never on wire</th><td>Mnemonic, spend key, PIN, PAN/CVV, full ACH account number</td></tr>
					</tbody>
				</table>
				<p>App UI may keep camelCase tickers (<code>BTC</code>/<code>XMR</code>); encoding to public currency codes and SCREAMING_SNAKE happens at the HTTP boundary (<code>wireFormat.ts</code>, <code>publicCurrency.ts</code>).</p>
			</section>

			<section class="endpoint" id="stream" aria-labelledby="stream-title">
				<h2 id="stream-title">WebSocket <code>/v1/stream</code></h2>
				<p><strong>Host:</strong> <code>wss://api.cipherbank.dev/v1/stream</code> · Authenticated connection.</p>
				<p>Envelope:</p>
				<pre><code>{
  "TYPE": "BALANCE.UPDATE",
  "PAYLOAD": { }
}</code></pre>
				<table>
					<thead><tr><th scope="col">TYPE</th><th scope="col">PAYLOAD (summary)</th></tr></thead>
					<tbody>
						<tr><th scope="row"><code>BALANCE.UPDATE</code></th><td>Full portfolio document (SCREAMING_SNAKE).</td></tr>
						<tr><th scope="row"><code>RATE.TICK</code></th><td><code>PAIR</code>, <code>RATE</code>, <code>TS</code>.</td></tr>
						<tr><th scope="row"><code>CONVERT.SETTLED</code></th><td><code>TX_ID</code>, <code>AMOUNT_OUT</code>.</td></tr>
						<tr><th scope="row"><code>TRANSFER.SETTLED</code></th><td><code>TX_ID</code>, <code>ARRIVED_AT</code>.</td></tr>
						<tr><th scope="row"><code>PAYMENT.SETTLED</code></th><td><code>PAYMENT_ID</code>, <code>BREAKDOWN</code>.</td></tr>
						<tr><th scope="row"><code>POS.SETTLED</code></th><td><code>SESSION_ID</code>, <code>RECEIPT_ID</code>, <code>AMOUNT</code>, <code>CURRENCY</code>.</td></tr>
					</tbody>
				</table>
			</section>

${articles}

		</main>
	</div>
	<footer>CipherBank Full API Reference · aligned to CB_InitialAPIRef wire standards · generated for Cora Digital Teller</footer>
</body>
</html>
`;

fs.writeFileSync(outPath, html);
fs.writeFileSync(handoffOut, html);
console.log('Wrote', outPath);
console.log('Wrote', handoffOut);
console.log('Endpoints:', endpoints.length);
