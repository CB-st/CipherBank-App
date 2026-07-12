import { mockLatency } from './latency';
import { scheduleSettlement } from './stream';
import portfolio from './fixtures/portfolio.json';
import assets from './fixtures/assets.json';
import rates from './fixtures/rates.json';
import recipients from './fixtures/recipients.json';
import activity from './fixtures/activity.json';
import receive from './fixtures/receive.json';
import prefsFixture from './fixtures/prefs.json';
import vaultBinaries from './fixtures/vault-binaries.json';
import vaultCards from './fixtures/vault-cards.json';
import type { UserPrefs } from '@/features/prefs/prefs.types';

type Opts = { idempotencyKey?: string; signal?: AbortSignal };

function deepClone<T>(v: T): T {
  return JSON.parse(JSON.stringify(v)) as T;
}

const idempotencyStore = new Map<string, unknown>();
let quoteSeq = 0;
let txSeq = 0;

/** Mutable mock state for prefs / vault writes during a session. */
let prefsState: UserPrefs = deepClone(prefsFixture) as UserPrefs;
let binariesState = deepClone(vaultBinaries.binaries);
let cardsState = deepClone(vaultCards.cards) as any[];
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

function rateFor(symbol: string): number {
  const row = rates.rates.find((r) => r.symbol === symbol);
  return row?.usd ?? 1;
}

function rejectMnemonicLeak(body?: unknown) {
  if (!body || typeof body !== 'object') return;
  const raw = JSON.stringify(body).toLowerCase();
  if (raw.includes('mnemonic') || raw.includes('recovery phrase') || raw.includes('"seed"')) {
    throw new MockApiError(400, 'custody_local_only', 'Recovery mnemonic must never be sent to the server');
  }
}

function buildSeries(range: string, start: number, drift: number, vol: number) {
  const N: Record<string, number> = { '1D': 24, '1W': 28, '1M': 30, '1Y': 52, ALL: 60 };
  const n = N[range] ?? 30;
  const now = Date.now();
  const step = 86400e3 / 4;
  let v = start;
  const points: { t: number; v: number }[] = [];
  for (let i = 0; i < n; i++) {
    v = v * (1 + drift / n + (Math.sin(i * 1.7) + Math.cos(i * 0.6)) * (vol / n));
    points.push({ t: now - (n - i) * step, v: Math.round(v) });
  }
  return points;
}

async function handleGet(path: string): Promise<unknown> {
  const { pathname, query } = parsePath(path);

  if (pathname === '/portfolio') return deepClone(portfolio);
  if (pathname === '/assets') return deepClone(assets);
  if (pathname === '/rates') return deepClone(rates);
  if (pathname === '/recipients') return deepClone(recipients);
  if (pathname === '/activity') return deepClone(activity);
  if (pathname === '/prefs') return deepClone(prefsState);
  if (pathname === '/vault/binaries') return { binaries: deepClone(binariesState) };
  if (pathname === '/vault/cards') return { cards: deepClone(cardsState) };

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
    const compare = (query.get('compare') ?? '').split(',').filter(Boolean);
    const series = [
      { label: 'Wallet', symbol: 'WALLET', points: buildSeries(range, 100000, 0.02, 0.03) },
      ...compare.map((sym, i) => ({
        label: sym,
        symbol: sym,
        points: buildSeries(range, 100000 * (0.6 + i * 0.2), 0.015 - i * 0.005, 0.04),
      })),
    ];
    return { series };
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

  if (pathname === '/quotes') {
    quoteSeq += 1;
    const from = String(b.from ?? 'BTC');
    const to = String(b.to ?? 'USD');
    const amount = String(b.amount ?? '0');
    const fromUsd = rateFor(from);
    const toUsd = rateFor(to);
    const rate = toUsd === 0 ? 0 : fromUsd / toUsd;
    const amountOut = String(Number(amount) * rate);
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
