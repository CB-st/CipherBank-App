import AsyncStorage from '@react-native-async-storage/async-storage';
import type { LocalWalletDraft, WalletSource } from '@/features/portfolio/portfolio.types';

const KEY = 'cb_local_wallets_v1';

export async function loadLocalWallets(): Promise<LocalWalletDraft[]> {
  try {
    const raw = await AsyncStorage.getItem(KEY);
    if (!raw) return [];
    return JSON.parse(raw) as LocalWalletDraft[];
  } catch {
    return [];
  }
}

export async function saveLocalWallets(list: LocalWalletDraft[]): Promise<void> {
  await AsyncStorage.setItem(KEY, JSON.stringify(list));
}

export async function addLocalWallet(input: {
  symbol: string;
  label: string;
  address?: string;
  source?: WalletSource;
}): Promise<LocalWalletDraft> {
  const list = await loadLocalWallets();
  const draft: LocalWalletDraft = {
    id: 'wal_local_' + Date.now().toString(36),
    symbol: input.symbol.toUpperCase(),
    label: input.label.trim() || 'Wallet',
    address: input.address?.trim() || undefined,
    source: input.source ?? (input.address ? 'watch' : 'local'),
    createdAt: Date.now(),
  };
  list.push(draft);
  await saveLocalWallets(list);
  return draft;
}

export async function removeLocalWallet(id: string): Promise<void> {
  const list = await loadLocalWallets();
  await saveLocalWallets(list.filter((w) => w.id !== id));
}
