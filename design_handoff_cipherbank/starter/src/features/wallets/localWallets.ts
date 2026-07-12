import AsyncStorage from '@react-native-async-storage/async-storage';
import type { LocalWalletDraft, WalletSource } from '@/features/portfolio/portfolio.types';
import { deriveAddress, isDerivableSymbol } from './derive';
import { getSessionMnemonic, unlockLocalCustody } from '@/features/vault/custody';

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
  derivationPath?: string;
  accountIndex?: number;
}): Promise<LocalWalletDraft> {
  const list = await loadLocalWallets();
  const draft: LocalWalletDraft = {
    id: 'wal_local_' + Date.now().toString(36) + '_' + Math.random().toString(36).slice(2, 6),
    symbol: input.symbol.toUpperCase(),
    label: input.label.trim() || 'Wallet',
    address: input.address?.trim() || undefined,
    derivationPath: input.derivationPath,
    accountIndex: input.accountIndex,
    source: input.source ?? (input.address && !input.derivationPath ? 'watch' : 'local'),
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

async function requireMnemonic(): Promise<string> {
  let m = getSessionMnemonic();
  if (!m) {
    const ok = await unlockLocalCustody();
    if (!ok) throw new Error('Unlock required');
    m = getSessionMnemonic();
  }
  if (!m) throw new Error('Unlock required');
  return m;
}

/** Ensure Primary (index 0) BTC + ETH derived metadata exist. */
export async function ensureDerivedWallets(mnemonic?: string): Promise<LocalWalletDraft[]> {
  const phrase = mnemonic ?? (await requireMnemonic());
  const list = await loadLocalWallets();
  const created: LocalWalletDraft[] = [];

  for (const symbol of ['BTC', 'ETH'] as const) {
    const hasPrimary = list.some(
      (w) => w.symbol === symbol && w.source === 'local' && (w.accountIndex === 0 || w.derivationPath?.endsWith('/0')),
    );
    if (hasPrimary) continue;
    const derived = deriveAddress(symbol, phrase, 0);
    if (!derived) continue;
    const draft = await addLocalWallet({
      symbol,
      label: 'Primary',
      address: derived.address,
      derivationPath: derived.path,
      accountIndex: derived.accountIndex,
      source: 'local',
    });
    created.push(draft);
    list.push(draft);
  }
  return created;
}

/** Derive next account index for BTC/ETH and persist public metadata. */
export async function deriveNextWallet(symbol: string, label?: string): Promise<LocalWalletDraft> {
  if (!isDerivableSymbol(symbol)) throw new Error('Symbol not supported for derivation');
  const phrase = await requireMnemonic();
  const list = await loadLocalWallets();
  const sym = symbol.toUpperCase();
  const indices = list
    .filter((w) => w.symbol === sym && w.source === 'local' && typeof w.accountIndex === 'number')
    .map((w) => w.accountIndex as number);
  const next = indices.length ? Math.max(...indices) + 1 : 0;
  const derived = deriveAddress(sym, phrase, next);
  if (!derived) throw new Error('Derive failed');
  return addLocalWallet({
    symbol: sym,
    label: label?.trim() || (next === 0 ? 'Primary' : `Account ${next}`),
    address: derived.address,
    derivationPath: derived.path,
    accountIndex: derived.accountIndex,
    source: 'local',
  });
}
