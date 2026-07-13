import * as SQLite from 'expo-sqlite';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { MIGRATIONS, SCHEMA_SQL, SCHEMA_VERSION } from './schema';
import type { LocalWalletDraft } from '@/features/portfolio/portfolio.types';
import { normalizePrefs } from '@/features/prefs/localeCurrency';
import type { UserPrefs } from '@/features/prefs/prefs.types';

const DB_NAME = 'cipherbank.db';
const ASYNC_WALLETS_KEY = 'cb_local_wallets_v1';
const ASYNC_PREFS_KEY = 'cb_user_prefs_v1';

let dbPromise: Promise<SQLite.SQLiteDatabase> | null = null;
let migrated = false;

export async function getDb(): Promise<SQLite.SQLiteDatabase> {
  if (!dbPromise) {
    dbPromise = SQLite.openDatabaseAsync(DB_NAME);
  }
  const db = await dbPromise;
  if (!migrated) {
    await db.execAsync(SCHEMA_SQL);
    await applyVersionMigrations(db);
    await migrateFromAsyncStorage(db);
    migrated = true;
  }
  return db;
}

async function applyVersionMigrations(db: SQLite.SQLiteDatabase): Promise<void> {
  const row = await db.getFirstAsync<{ value: string }>(
    'SELECT value FROM schema_meta WHERE key = ?',
    'version',
  );
  let current = Number(row?.value ?? 0);
  if (!Number.isFinite(current)) current = 0;

  for (const m of MIGRATIONS) {
    if (current < m.to) {
      await db.execAsync(m.sql);
      current = m.to;
    }
  }

  await db.runAsync(
    'INSERT OR REPLACE INTO schema_meta (key, value) VALUES (?, ?)',
    'version',
    String(SCHEMA_VERSION),
  );
}

/** Reset migration flag — tests only. */
export function _resetPersistForTests() {
  dbPromise = null;
  migrated = false;
}

async function migrateFromAsyncStorage(db: SQLite.SQLiteDatabase): Promise<void> {
  const done = await db.getFirstAsync<{ value: string }>(
    'SELECT value FROM sync_meta WHERE key = ?',
    'async_migrated_v1',
  );
  if (done?.value === '1') return;

  try {
    const rawWallets = await AsyncStorage.getItem(ASYNC_WALLETS_KEY);
    if (rawWallets) {
      const list = JSON.parse(rawWallets) as LocalWalletDraft[];
      for (const w of list) {
        await db.runAsync(
          `INSERT OR REPLACE INTO wallets
            (id, symbol, label, address, derivation_path, account_index, source, mode, sync_json, view_key_fp, created_at)
           VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
          w.id,
          w.symbol.toUpperCase(),
          w.label,
          w.address ?? null,
          w.derivationPath ?? null,
          w.accountIndex ?? null,
          w.source,
          w.mode ?? null,
          w.sync ? JSON.stringify(w.sync) : null,
          w.viewKeyFingerprint ?? null,
          w.createdAt,
        );
      }
    }
  } catch {
    /* ignore corrupt migration source */
  }

  try {
    const rawPrefs = await AsyncStorage.getItem(ASYNC_PREFS_KEY);
    if (rawPrefs) {
      const parsed = JSON.parse(rawPrefs) as Partial<UserPrefs>;
      const prefs = normalizePrefs(parsed);
      await db.runAsync(
        'INSERT OR REPLACE INTO prefs (key, value_json) VALUES (?, ?)',
        'user',
        JSON.stringify(prefs),
      );
    }
  } catch {
    /* ignore */
  }

  await db.runAsync(
    'INSERT OR REPLACE INTO sync_meta (key, value, updated_at) VALUES (?, ?, ?)',
    'async_migrated_v1',
    '1',
    Date.now(),
  );
}
