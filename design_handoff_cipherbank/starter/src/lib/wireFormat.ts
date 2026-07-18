/**
 * CipherBank wire format — SCREAMING_SNAKE_CASE keys (CB_InitialAPIRef standard).
 * App domain types stay camelCase; encode/decode at the HTTP boundary.
 */

const PUBLIC_PATHS = new Set(['/currencies', '/iquote', '/quote', '/test']);

/** Explicit camel → wire for awkward digit suffixes. */
const CAMEL_TO_WIRE: Record<string, string> = {
  change24h: 'CHANGE_24H',
  accountLast4: 'ACCOUNT_LAST4',
  last4: 'LAST4',
  expMonth: 'EXP_MONTH',
  expYear: 'EXP_YEAR',
  txId: 'TX_ID',
  quoteId: 'QUOTE_ID',
  userId: 'USER_ID',
  paymentId: 'PAYMENT_ID',
  sessionId: 'SESSION_ID',
  walletId: 'WALLET_ID',
  merchantId: 'MERCHANT_ID',
  bankId: 'BANK_ID',
  viewKey: 'VIEW_KEY',
  viewKeyFingerprint: 'VIEW_KEY_FINGERPRINT',
  restoreHeight: 'RESTORE_HEIGHT',
  deviceBound: 'DEVICE_BOUND',
  deviceId: 'DEVICE_ID',
  deviceAttestation: 'DEVICE_ATTESTATION',
  refreshToken: 'REFRESH_TOKEN',
  expiresAt: 'EXPIRES_AT',
  syncedAt: 'SYNCED_AT',
  createdAt: 'CREATED_AT',
  arrivedAt: 'ARRIVED_AT',
  amountOut: 'AMOUNT_OUT',
  usdValue: 'USD_VALUE',
  generatedAt: 'GENERATED_AT',
  ttlMs: 'TTL_MS',
  nextCursor: 'NEXT_CURSOR',
  processorToken: 'PROCESSOR_TOKEN',
  hardwareTest: 'HARDWARE_TEST',
  ephemeralCardTokenId: 'EPHEMERAL_CARD_TOKEN_ID',
  cardId: 'CARD_ID',
  routingNumber: 'ROUTING_NUMBER',
  accountType: 'ACCOUNT_TYPE',
  accountHolderName: 'ACCOUNT_HOLDER_NAME',
  displayName: 'DISPLAY_NAME',
  bankName: 'BANK_NAME',
  defaultSendSpeed: 'DEFAULT_SEND_SPEED',
  coraEnabled: 'CORA_ENABLED',
  valuesHiddenOnLaunch: 'VALUES_HIDDEN_ON_LAUNCH',
  enabledCurrencies: 'ENABLED_CURRENCIES',
  baseCurrency: 'BASE_CURRENCY',
  homeOrder: 'HOME_ORDER',
  homeVisible: 'HOME_VISIBLE',
  appLockIdleSec: 'APP_LOCK_IDLE_SEC',
  localeInferredBase: 'LOCALE_INFERRED_BASE',
};

const WIRE_TO_CAMEL: Record<string, string> = Object.fromEntries(
  Object.entries(CAMEL_TO_WIRE).map(([camel, wire]) => [wire, camel]),
);

export function isPublicApiPath(path: string): boolean {
  const pathname = path.split('?')[0] ?? path;
  return PUBLIC_PATHS.has(pathname);
}

/** camelCase / mixed → SCREAMING_SNAKE_CASE */
export function toScreamingSnakeKey(key: string): string {
  if (CAMEL_TO_WIRE[key]) return CAMEL_TO_WIRE[key];
  if (/^[A-Z0-9_]+$/.test(key)) return key;
  return key
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .replace(/([A-Za-z])(\d+)/g, '$1_$2')
    .replace(/-/g, '_')
    .toUpperCase();
}

/** SCREAMING_SNAKE → camelCase */
export function toCamelKey(key: string): string {
  if (WIRE_TO_CAMEL[key]) return WIRE_TO_CAMEL[key];
  if (!/^[A-Z0-9_]+$/.test(key)) return key;
  if (!key.includes('_')) return key.toLowerCase();
  const parts = key.toLowerCase().split('_');
  return parts
    .map((p, i) => (i === 0 ? p : p.charAt(0).toUpperCase() + p.slice(1)))
    .join('');
}

function isPlainObject(v: unknown): v is Record<string, unknown> {
  return !!v && typeof v === 'object' && !Array.isArray(v) && !(v instanceof Date);
}

/** Deep-convert object keys to SCREAMING_SNAKE (wire out). */
export function toWire<T = unknown>(value: T): T {
  if (Array.isArray(value)) {
    return value.map((item) => toWire(item)) as T;
  }
  if (!isPlainObject(value)) return value;
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(value)) {
    out[toScreamingSnakeKey(k)] = toWire(v);
  }
  return out as T;
}

/** Deep-convert object keys from SCREAMING_SNAKE to camelCase (wire in). */
export function fromWire<T = unknown>(value: T): T {
  if (Array.isArray(value)) {
    return value.map((item) => fromWire(item)) as T;
  }
  if (!isPlainObject(value)) return value;
  const out: Record<string, unknown> = {};
  for (const [k, v] of Object.entries(value)) {
    out[toCamelKey(k)] = fromWire(v);
  }
  return out as T;
}

/** Encode request body for product paths; pass through public PriceCache bodies. */
export function encodeRequestBody(path: string, body: unknown): unknown {
  if (body === undefined || body === null) return body;
  if (isPublicApiPath(path)) return body;
  return toWire(body);
}

/** Decode response for app use; public paths stay as SCREAMING_SNAKE. */
export function decodeResponseBody<T>(path: string, body: T): T {
  if (body === undefined || body === null) return body;
  if (isPublicApiPath(path)) return body;
  return fromWire(body);
}
