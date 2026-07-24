import AsyncStorage from '@react-native-async-storage/async-storage';
import { getDb } from './db';
import type { HistoryGranularity, HistoryPoint } from '@/features/history/history.api';
import type { RateRow } from '@/features/market/ratesCache';

const SYNC_META_ASYNC = 'cb_sync_meta_v1:';

async function getSyncMetaAsync(key: string): Promise<string | null> {
  try {
    return await AsyncStorage.getItem(SYNC_META_ASYNC + key);
  } catch {
    return null;
  }
}

async function setSyncMetaAsync(key: string, value: string): Promise<void> {
  try {
    await AsyncStorage.setItem(SYNC_META_ASYNC + key, value);
  } catch {
    /* ignore */
  }
}

export async function getRatesSnapshot(symbols?: string[]): Promise<RateRow[]> {
  const db = await getDb();
  if (symbols?.length) {
    const placeholders = symbols.map(() => '?').join(',');
    const rows = await db.getAllAsync<{ symbol: string; usd: number; change24h: number }>(
      `SELECT symbol, usd, change24h FROM rates_snapshot WHERE symbol IN (${placeholders})`,
      ...symbols.map((s) => s.toUpperCase()),
    );
    return rows;
  }
  return db.getAllAsync<{ symbol: string; usd: number; change24h: number }>(
    'SELECT symbol, usd, change24h FROM rates_snapshot',
  );
}

export async function upsertRatesSnapshot(rates: RateRow[]): Promise<void> {
  const db = await getDb();
  const now = Date.now();
  await db.withTransactionAsync(async () => {
    for (const r of rates) {
      await db.runAsync(
        'INSERT OR REPLACE INTO rates_snapshot (symbol, usd, change24h, updated_at) VALUES (?, ?, ?, ?)',
        r.symbol.toUpperCase(),
        r.usd,
        r.change24h,
        now,
      );
    }
  });
}

export async function getOhlcWindow(
  symbol: string,
  granularity: HistoryGranularity,
  from?: number,
  to?: number,
): Promise<HistoryPoint[]> {
  const db = await getDb();
  const sym = symbol.toUpperCase();
  let sql =
    'SELECT t, o, h, l, c, v FROM market_ohlc WHERE symbol = ? AND granularity = ?';
  const params: (string | number)[] = [sym, granularity];
  if (from != null) {
    sql += ' AND t >= ?';
    params.push(from);
  }
  if (to != null) {
    sql += ' AND t <= ?';
    params.push(to);
  }
  sql += ' ORDER BY t ASC';
  const rows = await db.getAllAsync<{
    t: number;
    o: number | null;
    h: number | null;
    l: number | null;
    c: number | null;
    v: number;
  }>(sql, ...params);
  return rows.map((r) => ({
    t: r.t,
    v: r.v,
    o: r.o ?? undefined,
    h: r.h ?? undefined,
    l: r.l ?? undefined,
    c: r.c ?? undefined,
  }));
}

export async function upsertOhlcPoints(
  symbol: string,
  granularity: HistoryGranularity,
  points: HistoryPoint[],
): Promise<void> {
  if (!points.length) return;
  const db = await getDb();
  const sym = symbol.toUpperCase();
  await db.withTransactionAsync(async () => {
    for (const p of points) {
      await db.runAsync(
        `INSERT OR REPLACE INTO market_ohlc (symbol, granularity, t, o, h, l, c, v)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?)`,
        sym,
        granularity,
        p.t,
        p.o ?? null,
        p.h ?? null,
        p.l ?? null,
        p.c ?? p.v,
        p.v,
      );
    }
  });
}

export async function getSyncMeta(key: string): Promise<string | null> {
  try {
    const db = await getDb();
    const row = await db.getFirstAsync<{ value: string }>(
      'SELECT value FROM sync_meta WHERE key = ?',
      key,
    );
    if (row?.value != null) return row.value;
  } catch {
    /* web / corrupt sqlite — fall through */
  }
  return getSyncMetaAsync(key);
}

export async function setSyncMeta(key: string, value: string): Promise<void> {
  // AsyncStorage first so setup flags survive when expo-sqlite web is flaky.
  await setSyncMetaAsync(key, value);
  try {
    const db = await getDb();
    await db.runAsync(
      'INSERT OR REPLACE INTO sync_meta (key, value, updated_at) VALUES (?, ?, ?)',
      key,
      value,
      Date.now(),
    );
  } catch {
    /* AsyncStorage already holds the value */
  }
}
