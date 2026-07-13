import { getDb } from './db';
import { normalizePrefs } from '@/features/prefs/localeCurrency';
import type { UserPrefs } from '@/features/prefs/prefs.types';

const PREFS_KEY = 'user';

export async function loadPrefs(): Promise<UserPrefs> {
  const db = await getDb();
  const row = await db.getFirstAsync<{ value_json: string }>(
    'SELECT value_json FROM prefs WHERE key = ?',
    PREFS_KEY,
  );
  if (!row?.value_json) {
    return normalizePrefs({});
  }
  try {
    return normalizePrefs(JSON.parse(row.value_json) as Partial<UserPrefs>);
  } catch {
    return normalizePrefs({});
  }
}

export async function savePrefs(prefs: UserPrefs): Promise<void> {
  const db = await getDb();
  await db.runAsync(
    'INSERT OR REPLACE INTO prefs (key, value_json) VALUES (?, ?)',
    PREFS_KEY,
    JSON.stringify(prefs),
  );
}
