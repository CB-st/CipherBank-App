import { mockLatency } from './latency';
import { scheduleSettlement } from './stream';
import portfolioEmpty from './fixtures/portfolio.json';
import portfolioDemo from './fixtures/portfolio.demo.json';
import assets from './fixtures/assets.json';
import rates from './fixtures/rates.json';
import recipients from './fixtures/recipients.json';
import activity from './fixtures/activity.json';
import receive from './fixtures/receive.json';
import prefsFixture from './fixtures/prefs.json';
import vaultBinaries from './fixtures/vault-binaries.json';
import vaultCards from './fixtures/vault-cards.json';
import walletsFixture from './fixtures/wallets.json';
import accountBootstrap from './fixtures/account-bootstrap.json';
import type { UserPrefs } from '@/features/prefs/prefs.types';
import { isSeedDemo } from '@/lib/runtimeFlags';

type Opts = { idempotencyKey?: string; signal?: AbortSignal };

function deepClone<T>(v: T): T {
  return JSON.parse(JSON.stringify(v)) as T;
}

const portfolio = isSeedDemo() ? portfolioDemo : portfolioEmpty;

const idempotencyStore = new Map<string, unknown>();
let quoteSeq = 0;
let txSeq = 0;

/** Mutable mock state for prefs / vault writes during a session. */
let prefsState: UserPrefs = deepClone(prefsFixture) as UserPrefs;
let binariesState = deepClone(vaultBinaries.binaries);
let cardsState = deepClone(vaultCards.cards) as any[];
let walletsState = deepClone(walletsFixture.wallets) as any[];
const posSessions = new Map<string, any>();

const requireTestCard =
  (typeof process !== 'undefined' ? process.env.EXPO_PUBLIC_POS_REQUIRE_TEST_CARD : undefined) !== 'false';


class MockApiError extends Error {
  status: number;
  detail: { code: string; message: string; detail?: object };
  constructor(status: number, code: string, message: string, detail?: object) {
    super(message);
    this.status = status;
    this.detail = { code, message, detail };
  }
}

function id(prefix: string) {
  txSeq += 1;
  return `${prefix}_${Date.now()}_${txSeq}`;
}

function parsePath(path: string): { pathname: string; query: URLSearchParams } {
  const [pathname, qs] = path.split('?');
  return { pathname, query: new URLSearchParams(qs ?? '') };
}

/** Resolve USD price from fixture using app ticker or public currency code. */
function rateFor(symbol: string): number {
  const u = String(symbol ?? '').toUpperCase();
  const aliases: Record<string, string> = {
    BITCOIN: 'BTC',
    MONERO: 'XMR',
    ETHEREUM: 'ETH',
    LITECOIN: 'LTC',
    DOGECOIN: 'DOGE',
  };
  const ticker = aliases[u] ?? u;
  const row = rates.rates.find((r) => r.symbol === ticker);
  return row?.usd ?? 1;
}

function publicCurrenciesFromFixture(): string[] {
  const aliases: Record<string, string> = {
    BTC: 'BITCOIN',
    XMR: 'MONERO',
    ETH: 'ETHEREUM',
    LTC: 'LITECOIN',
    DOGE: 'DOGECOIN',
    USD: 'USD',
    EUR: 'EUR',
    JPY: 'JPY',
  };
  return rates.rates.map((r) => aliases[r.symbol] ?? r.symbol);
}

function requireJsonBodyFields(b: Record<string, unknown>, fields: string[]) {
  for (const f of fields) {
    if (b[f] === undefined || b[f] === null) {
      throw new MockApiError(417, 'invalid_request', `Missing or invalid field: ${f}`);
    }
  }
}

function rejectMnemonicLeak(body?: unknown) {
  if (!body || typeof body !== 'object') return;
  const raw = JSON.stringify(body).toLowerCase();
  if (raw.includes('mnemonic') || raw.includes('recovery phrase') || raw.includes('"seed"') || raw.includes('spendkey') || raw.includes('spend_key')) {
    throw new MockApiError(400, 'custody_local_only', 'Recovery mnemonic / spend key must never be sent to the server');
  }
}

function fingerprintKey(viewKey: string): string {
  const cleaned = viewKey.trim().toLowerCase().replace(/\s+/g, '');
  if (cleaned.length < 8) return '••••';
  return cleaned.slice(0, 4) + '…' + cleaned.slice(-4);
}

function mockXmrAddress(seed: string) {
  // Deterministic-looking mainnet-shaped mock (not a real checksummed address)
  const base =
    '4AdUndXHHZ6cfufTMvppY6JwXNouMBzSkbLYfpAV5Usx3skxNgYeYTRj5UzqtReoS44qo9mtmXCqY45DJ852K5Jp2zC5AW6';
  return base.slice(0, 80) + seed.replace(/\W/g, '').slice(0, 15).padEnd(15, '0');
}

function stepMs(granularity: string): number {
  const m: Record<string, number> = { '1m': 60e3, '5m': 300e3, '1h': 3600e3, '1d': 86400e3 };
  return m[granularity] ?? 3600e3;
}

function pointCount(range: string, granularity: string): number {
  const presets: Record<string, number> = { '1D': 24, '1W': 28, '1M': 30, '1Y': 52, ALL: 60 };
  if (granularity === '1m' && range === '1D') return 96;
  if (granularity === '5m' && range === '1D') return 48;
  if (granularity === '1h' && (range === '1W' || range === '1M')) return range === '1W' ? 42 : 48;
  return presets[range] ?? 30;
}

function buildSeries(
  range: string,
  start: number,
  drift: number,
  vol: number,
  granularity: string,
  fromMs?: number,
  toMs?: number,
) {
  const n = pointCount(range, granularity);
  const step = stepMs(granularity);
  const end = toMs ?? Date.now();
  const startT = fromMs ?? end - n * step;
  let v = start;
  const points: { t: number; v: number; o: number; h: number; l: number; c: number }[] = [];
  for (let i = 0; i < n; i++) {
    const open = v;
    v = v * (1 + drift / n + (Math.sin(i * 1.7) + Math.cos(i * 0.6)) * (vol / n));
    const close = Math.round(v);
    const high = Math.round(Math.max(open, close) * (1 + vol / (n * 2)));
    const low = Math.round(Math.min(open, close) * (1 - vol / (n * 2)));
    points.push({
      t: startT + i * step,
      v: close,
      o: Math.round(open),
      h: high,
      l: low,
      c: close,
    });
  }
  return points;
}

async function handleGet(path: string): Promise<unknown> {
  const { pathname, query } = parsePath(path);

  if (pathname === '/portfolio') return deepClone(portfolio);
  if (pathname === '/assets') return deepClone(assets);
  if (pathname === '/rates') {
    return {
      ...deepClone(rates),
      generatedAt: Date.now(),
      ttlMs: 10_000,
    };
  }
  if (pathname === '/recipients') return deepClone(recipients);
  if (pathname === '/account/bootstrap') return deepClone(accountBootstrap);
  if (pathname === '/activity') return deepClone(activity);
  if (pathname === '/prefs') return deepClone(prefsState);
  if (pathname === '/vault/binaries') return { binaries: deepClone(binariesState) };
  if (pathname === '/vault/cards') return { cards: deepClone(cardsState) };

  if (pathname === '/wallets') {
    const symbol = (query.get('symbol') ?? '').toUpperCase();
    const list = symbol ? walletsState.filter((w) => w.symbol === symbol) : walletsState;
    return { wallets: deepClone(list) };
  }

  if (pathname.startsWith('/wallets/')) {
    const wid = pathname.slice('/wallets/'.length);
    if (wid.includes('/')) {
      /* fall through to refresh POST only */
    } else {
      const w = walletsState.find((x) => x.id === wid);
      if (!w) throw new MockApiError(404, 'not_found', 'Wallet not found');
      return deepClone(w);
    }
  }

  if (pathname.startsWith('/pos/sessions/')) {
    const sid = pathname.slice('/pos/sessions/'.length);
    const sess = posSessions.get(sid);
    if (!sess) throw new MockApiError(404, 'not_found', 'POS session not found');
    if (sess.expiresAt < Date.now() && sess.status !== 'settled') {
      sess.status = 'expired';
    }
    return deepClone(sess);
  }

  if (pathname.startsWith('/receive/')) {
    const asset = pathname.slice('/receive/'.length).toUpperCase();
    const info = (receive as Record<string, unknown>)[asset] ?? {
      handle: 'cora@cipherbank.id',
      address: `${asset}-ADDR-MOCK`,
      uri: `cipherbank:receive/${asset}`,
      qr: `cipherbank:receive/${asset}`,
    };
    return deepClone(info);
  }

  if (pathname === '/history') {
    const range = query.get('range') ?? '1M';
    const granularity = query.get('granularity') ?? '1h';
    const symbolsRaw = query.get('symbols') || query.get('compare') || '';
    const compare = symbolsRaw.split(',').filter(Boolean);
    const from = query.get('from') ? Number(query.get('from')) : undefined;
    const to = query.get('to') ? Number(query.get('to')) : undefined;
    const series = [
      {
        label: 'Wallet',
        symbol: 'WALLET',
        granularity,
        points: buildSeries(range, 100000, 0.02, 0.03, granularity, from, to),
      },
      ...compare.map((sym, i) => ({
        label: sym,
        symbol: sym,
        granularity,
        points: buildSeries(range, 100000 * (0.6 + i * 0.2), 0.015 - i * 0.005, 0.04, granularity, from, to),
      })),
    ];
    return { series, meta: { source: 'mock', generatedAt: Date.now() } };
  }

  if (pathname.startsWith('/convert/') || pathname.startsWith('/transfers/') || pathname.startsWith('/payments/')) {
    return { txId: pathname.split('/').pop(), status: 'settled' };
  }

  throw new MockApiError(404, 'not_found', `No mock handler for GET ${pathname}`);
}

async function handlePost(path: string, body?: unknown): Promise<unknown> {
  rejectMnemonicLeak(body);
  const { pathname } = parsePath(path);
  const b = (body ?? {}) as Record<string, any>;

  // ── CipherBank public API (CB_InitialAPIRef.html) — SCREAMING_SNAKE ──
  if (pathname === '/test') {
    return {};
  }

  if (pathname === '/currencies') {
    return { CURRENCIES: publicCurrenciesFromFixture() };
  }

  if (pathname === '/iquote') {
    requireJsonBodyFields(b, ['INPUT_AMOUNT', 'INPUT_CURRENCY', 'OUTPUT_CURRENCY']);
    const inputAmount = Number(b.INPUT_AMOUNT);
    const inputCur = String(b.INPUT_CURRENCY).toUpperCase();
    const outputCur = String(b.OUTPUT_CURRENCY).toUpperCase();
    if (!Number.isFinite(inputAmount)) {
      throw new MockApiError(417, 'invalid_request', 'INPUT_AMOUNT must be a number');
    }
    const fromUsd = rateFor(inputCur);
    const toUsd = rateFor(outputCur);
    const outputAmount = toUsd === 0 ? 0 : (inputAmount * fromUsd) / toUsd;
    return {
      INPUT_AMOUNT: inputAmount,
      INPUT_CURRENCY: inputCur,
      OUTPUT_AMOUNT: outputAmount,
      OUTPUT_CURRENCY: outputCur,
    };
  }

  if (pathname === '/quote') {
    requireJsonBodyFields(b, ['INPUT_CURRENCY', 'OUTPUT_AMOUNT', 'OUTPUT_CURRENCY']);
    const outputAmount = Number(b.OUTPUT_AMOUNT);
    const inputCur = String(b.INPUT_CURRENCY).toUpperCase();
    const outputCur = String(b.OUTPUT_CURRENCY).toUpperCase();
    if (!Number.isFinite(outputAmount)) {
      throw new MockApiError(417, 'invalid_request', 'OUTPUT_AMOUNT must be a number');
    }
    const fromUsd = rateFor(inputCur);
    const toUsd = rateFor(outputCur);
    const inputAmount = fromUsd === 0 ? 0 : (outputAmount * toUsd) / fromUsd;
    return {
      INPUT_AMOUNT: inputAmount,
      INPUT_CURRENCY: inputCur,
      OUTPUT_AMOUNT: outputAmount,
      OUTPUT_CURRENCY: outputCur,
    };
  }

  if (pathname === '/session') {
    return {
      token: 'mock_token_' + Date.now(),
      refreshToken: 'mock_refresh_' + Date.now(),
      expiresAt: Date.now() + 3600_000,
      userId: 'user_mock_cora',
    };
  }

  if (pathname === '/session/refresh') {
    return {
      token: 'mock_token_refreshed_' + Date.now(),
      refreshToken: b.refreshToken ?? 'mock_refresh',
      expiresAt: Date.now() + 3600_000,
    };
  }

  /**
   * Legacy product `/quotes` — prefer public POST `/iquote`.
   * Still accepted for older callers; same PriceCache math.
   */
  if (pathname === '/quotes') {
    quoteSeq += 1;
    const from = String(b.from ?? b.INPUT_CURRENCY ?? 'BTC');
    const to = String(b.to ?? b.OUTPUT_CURRENCY ?? 'USD');
    const amount = Number(b.amount ?? b.INPUT_AMOUNT ?? 0);
    const fromUsd = rateFor(from);
    const toUsd = rateFor(to);
    const rate = toUsd === 0 ? 0 : fromUsd / toUsd;
    const amountOut = String(amount * rate);
    return {
      quoteId: `q_${Date.now()}_${quoteSeq}`,
      from,
      to,
      rate,
      amountOut,
      expiresAt: Date.now() + 15_000,
      fee: '0.00',
    };
  }

  if (pathname === '/wallets') {
    const mode = String(b.mode ?? 'watch') as 'managed' | 'unmanaged' | 'watch';
    const symbol = String(b.symbol ?? 'XMR').toUpperCase();
    const label = String(b.label ?? (mode === 'managed' ? 'Managed' : mode === 'unmanaged' ? 'Unmanaged' : 'Watch'));
    if (mode === 'unmanaged') {
      if (!b.address || !b.viewKey) {
        throw new MockApiError(422, 'invalid_request', 'Unmanaged XMR requires address + viewKey');
      }
    }
    if (mode === 'watch' && !b.address) {
      throw new MockApiError(422, 'invalid_request', 'Watch wallet requires address');
    }
    const walletId = id('wal_xmr');
    const address =
      mode === 'managed'
        ? mockXmrAddress(walletId)
        : String(b.address);
    const row = {
      id: walletId,
      symbol,
      label,
      mode,
      address,
      balance: '0',
      unlockedBalance: '0',
      restoreHeight: Number(b.restoreHeight ?? 3100000),
      sync: {
        height: mode === 'watch' ? 0 : 3100400,
        target: 3100500,
        state: mode === 'watch' ? 'pending' : 'syncing',
      },
      viewKeyFingerprint: b.viewKey ? fingerprintKey(String(b.viewKey)) : undefined,
    };
    walletsState = [...walletsState, row];
    return {
      walletId,
      symbol,
      label,
      mode,
      address,
      sync: row.sync,
      viewKeyFingerprint: row.viewKeyFingerprint,
    };
  }

  if (pathname.match(/^\/wallets\/[^/]+\/refresh$/)) {
    const wid = pathname.split('/')[2];
    const w = walletsState.find((x) => x.id === wid);
    if (!w) throw new MockApiError(404, 'not_found', 'Wallet not found');
    w.sync = {
      height: w.sync?.target ?? 3100500,
      target: w.sync?.target ?? 3100500,
      state: 'synced',
    };
    return { id: wid, sync: deepClone(w.sync) };
  }

  if (pathname === '/convert') {
    const txId = id('cvt');
    scheduleSettlement('convert', txId, { amountOut: String(b.amount ?? '0') });
    return { txId, status: 'accepted' };
  }

  if (pathname === '/transfers') {
    const txId = id('xfer');
    scheduleSettlement('transfer', txId);
    return { txId, status: 'accepted' };
  }

  if (pathname === '/payments') {
    const sources = (b.sources ?? []) as { asset: string; value: string }[];
    const total = Number(b.total ?? 0);
    const covered = sources.reduce((s, x) => s + Number(x.value), 0);
    if (covered < total - 0.001) {
      throw new MockApiError(422, 'mix_undercovered', 'Funding mix does not cover total', {
        covered,
        total,
      });
    }
    const paymentId = id('pay');
    scheduleSettlement('payment', paymentId, { breakdown: sources });
    return { paymentId, status: 'accepted' };
  }

  if (pathname === '/receive/request') {
    const asset = String(b.asset ?? 'BTC').toUpperCase();
    const amount = String(b.amount ?? '0');
    const base = (receive as Record<string, any>)[asset] ?? {
      handle: 'cora@cipherbank.id',
      address: `${asset}-ADDR-MOCK`,
      uri: `cipherbank:receive/${asset}`,
    };
    return {
      ...base,
      amount,
      uri: `${base.uri}?amount=${amount}`,
      qr: `${base.uri}?amount=${amount}`,
    };
  }

  if (pathname === '/recipients') {
    return { id: id('rcp'), ...b };
  }

  if (pathname === '/banks/link') {
    return { linked: true, bankId: id('bank'), last4: '4021' };
  }

  if (pathname === '/vault/binaries') {
    const row = {
      id: id('bin'),
      label: String(b.label ?? 'Wallet binary'),
      kind: String(b.kind ?? 'server_shard'),
      status: 'active',
      createdAt: Date.now(),
    };
    binariesState = [...binariesState, row];
    return row;
  }

  if (pathname === '/vault/cards') {
    const row = {
      id: id('card'),
      brand: String(b.brand ?? 'Visa'),
      last4: String(b.last4 ?? '0000'),
      expMonth: Number(b.expMonth ?? 1),
      expYear: Number(b.expYear ?? 2030),
      processorToken: 'tok_mock_' + id('tok'),
      createdAt: Date.now(),
    };
    cardsState = [...cardsState, row];
    return row;
  }

  if (pathname.match(/^\/vault\/cards\/[^/]+\/delete$/)) {
    const cardId = pathname.split('/')[3];
    cardsState = cardsState.filter((c) => c.id !== cardId);
    return { ok: true };
  }

  if (pathname === '/pos/sessions') {
    const sessionId = id('pos');
    const sess = {
      sessionId,
      merchantId: String(b.merchantId ?? 'merchant_lab'),
      amount: String(b.amount ?? '0'),
      currency: String(b.currency ?? 'USD'),
      label: b.label ? String(b.label) : undefined,
      status: 'pending_auth',
      expiresAt: Date.now() + 120_000,
    };
    posSessions.set(sessionId, sess);
    return deepClone(sess);
  }

  if (pathname === '/pos/authorize') {
    const sess = posSessions.get(String(b.sessionId));
    if (!sess) throw new MockApiError(404, 'not_found', 'POS session not found');
    if (sess.expiresAt < Date.now()) {
      sess.status = 'expired';
      throw new MockApiError(409, 'pos_expired', 'POS session expired');
    }
    if (!b.deviceAttestation) {
      throw new MockApiError(401, 'wallet_locked', 'Device attestation required');
    }
    const card = cardsState.find((c) => c.id === b.cardId);
    if (!card) throw new MockApiError(404, 'card_not_found', 'Card not in vault');
    if (requireTestCard && !card.hardwareTest) {
      throw new MockApiError(422, 'test_card_required', 'Lab mode requires a hardwareTest card');
    }
    const sources = (b.sources ?? []) as { asset: string; value: string }[];
    const covered = sources.reduce((s, x) => s + Number(x.value), 0);
    if (covered < Number(sess.amount) - 0.001) {
      throw new MockApiError(422, 'insufficient_funds', 'Funding sources do not cover POS amount', {
        covered,
        total: Number(sess.amount),
      });
    }
    const ephemeralCardTokenId = id('eph');
    const presentment = {
      tokenRef: id('ptr'),
      last4: card.last4,
      brand: card.brand,
      ttlMs: 60_000,
    };
    sess.status = 'authorized';
    sess.ephemeralCardTokenId = ephemeralCardTokenId;
    sess.presentment = presentment;
    scheduleSettlement('payment', sess.sessionId, {
      breakdown: sources,
    });
    return {
      sessionId: sess.sessionId,
      status: 'authorized',
      ephemeralCardTokenId,
      presentment,
    };
  }

  if (pathname === '/pos/confirm') {
    const sess = posSessions.get(String(b.sessionId));
    if (!sess) throw new MockApiError(404, 'not_found', 'POS session not found');
    if (sess.status === 'expired' || sess.expiresAt < Date.now()) {
      throw new MockApiError(409, 'pos_expired', 'POS session expired');
    }
    if (sess.status !== 'authorized' && sess.status !== 'ready_to_present') {
      throw new MockApiError(409, 'pos_expired', 'Session not authorized');
    }
    sess.status = 'ready_to_present';
    return { sessionId: sess.sessionId, status: 'ready_to_present' };
  }

  throw new MockApiError(404, 'not_found', `No mock handler for POST ${pathname}`);
}

async function handlePut(path: string, body?: unknown): Promise<unknown> {
  rejectMnemonicLeak(body);
  const { pathname } = parsePath(path);
  if (pathname === '/prefs') {
    prefsState = { ...prefsState, ...(body as UserPrefs) };
    return deepClone(prefsState);
  }
  throw new MockApiError(404, 'not_found', `No mock handler for PUT ${pathname}`);
}

export async function mockRequest<T>(
  method: string,
  path: string,
  body?: unknown,
  opts: Opts = {},
): Promise<T> {
  if (opts.idempotencyKey && method !== 'GET') {
    const hit = idempotencyStore.get(opts.idempotencyKey);
    if (hit !== undefined) {
      await mockLatency(80, 150);
      return hit as T;
    }
  }

  await mockLatency();

  let result: unknown;
  if (method === 'GET') result = await handleGet(path);
  else if (method === 'POST') result = await handlePost(path, body);
  else if (method === 'PUT') result = await handlePut(path, body);
  else throw new MockApiError(405, 'method_not_allowed', `Mock does not support ${method}`);

  if (opts.idempotencyKey && method !== 'GET') {
    idempotencyStore.set(opts.idempotencyKey, result);
  }
  return result as T;
}

export { MockApiError };
