import { api } from '@/lib/apiClient';
import { loadPrefs, savePrefs } from '@/features/persist/prefsRepo';
import { normalizePrefs } from './localeCurrency';
import { DEFAULT_PREFS, type UserPrefs } from './prefs.types';

export async function loadLocalPrefs(): Promise<UserPrefs> {
  try {
    return await loadPrefs();
  } catch {
    return normalizePrefs({});
  }
}

export async function saveLocalPrefs(prefs: UserPrefs): Promise<void> {
  await savePrefs(prefs);
}

export const fetchRemotePrefs = () => api.get<UserPrefs>('/prefs');
export const pushRemotePrefs = (prefs: UserPrefs) => api.put<UserPrefs>('/prefs', prefs);

/** @deprecated use normalizePrefs */
export { DEFAULT_PREFS };
