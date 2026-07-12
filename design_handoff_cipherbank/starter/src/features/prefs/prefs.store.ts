import AsyncStorage from '@react-native-async-storage/async-storage';
import { api } from '@/lib/apiClient';
import { DEFAULT_PREFS, type UserPrefs } from './prefs.types';

const LOCAL_KEY = 'cb_user_prefs_v1';

export async function loadLocalPrefs(): Promise<UserPrefs> {
  try {
    const raw = await AsyncStorage.getItem(LOCAL_KEY);
    if (!raw) return { ...DEFAULT_PREFS, homeVisible: { ...DEFAULT_PREFS.homeVisible }, homeOrder: [...DEFAULT_PREFS.homeOrder] };
    const parsed = JSON.parse(raw) as Partial<UserPrefs>;
    return {
      ...DEFAULT_PREFS,
      ...parsed,
      homeOrder: parsed.homeOrder ?? [...DEFAULT_PREFS.homeOrder],
      homeVisible: { ...DEFAULT_PREFS.homeVisible, ...parsed.homeVisible },
      appearance: parsed.appearance === 'light' ? 'light' : 'dark',
    };
  } catch {
    return { ...DEFAULT_PREFS, homeVisible: { ...DEFAULT_PREFS.homeVisible }, homeOrder: [...DEFAULT_PREFS.homeOrder] };
  }
}

export async function saveLocalPrefs(prefs: UserPrefs): Promise<void> {
  await AsyncStorage.setItem(LOCAL_KEY, JSON.stringify(prefs));
}

export const fetchRemotePrefs = () => api.get<UserPrefs>('/prefs');
export const pushRemotePrefs = (prefs: UserPrefs) => api.put<UserPrefs>('/prefs', prefs);
