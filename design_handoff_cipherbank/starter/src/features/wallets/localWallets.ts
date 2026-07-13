import {
  deleteWallet,
  listWallets,
  replaceAllWallets,
  upsertWallet,
} from '@/features/persist/walletsRepo';
import type { LocalWalletDraft, WalletMode, WalletSource, WalletSyncStatus } from '@/features/portfolio/portfolio.types';
import { deriveAddress, isDerivableSymbol } from './derive';
import { getSessionMnemonic, unlockLocalCustody } from '@/features/vault/custody';

export async function loadLocalWallets(): Promise<LocalWalletDraft[]> {
  try {
    return await listWallets();
  } catch {
    return [];
  }
}

export async function saveLocalWallets(list: LocalWalletDraft[]): Promise<void> {
  await replaceAllWallets(list);
}

export async function addLocalWallet(input: {
  symbol: string;
  label: string;
  address?: string;
  source?: WalletSource;
  derivationPath?: string;
  accountIndex?: number;
  mode?: WalletMode;
  sync?: WalletSyncStatus;
  viewKeyFingerprint?: string;
  id?: string;
}): Promise<LocalWalletDraft> {
  const draft: LocalWalletDraft = {
    id: input.id ?? 'wal_local_' + Date.now().toString(36) + '_' + Math.random().toString(36).slice(2, 6),
    symbol: input.symbol.toUpperCase(),
    label: input.label.trim() || 'Wallet',
    address: input.address?.trim() || undefined,
    derivationPath: input.derivationPath,
    accountIndex: input.accountIndex,
    source: input.source ?? (input.mode === 'managed' ? 'server' : input.address && !input.derivationPath ? 'watch' : 'local'),
    mode: input.mode,
    sync: input.sync,
    viewKeyFingerprint: input.viewKeyFingerprint,
    createdAt: Date.now(),
  };
  await upsertWallet(draft);
  return draft;
}

export async function removeLocalWallet(id: string): Promise<void> {
  await deleteWallet(id);
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

/** Ensure Primary (index 0) wallets exist for core derivable coins. */
export async function ensureDerivedWallets(mnemonic?: string): Promise<LocalWalletDraft[]> {
  const phrase = mnemonic ?? (await requireMnemonic());
  const list = await loadLocalWallets();
  const created: LocalWalletDraft[] = [];

  for (const symbol of ['BTC', 'ETH', 'LTC', 'DOGE'] as const) {
    if (!isDerivableSymbol(symbol)) continue;
    const hasPrimary = list.some(
      (w) => w.symbol === symbol && w.source === 'local' && (w.accountIndex === 0 || w.derivationPath?.endsWith('/0')),
    );
    if (hasPrimary) continue;
    // Only auto-create BTC/ETH on first unlock; LTC/DOGE on explicit ensure via symbol list
    if (symbol === 'LTC' || symbol === 'DOGE') continue;
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

/** Create or return Primary (index 0) for a single derivable symbol. */
export async function ensurePrimaryWallet(symbol: string, mnemonic?: string): Promise<LocalWalletDraft> {
  if (!isDerivableSymbol(symbol)) throw new Error('Symbol not supported for derivation');
  const phrase = mnemonic ?? (await requireMnemonic());
  const list = await loadLocalWallets();
  const sym = symbol.toUpperCase();
  const existing = list.find(
    (w) => w.symbol === sym && w.source === 'local' && (w.accountIndex === 0 || w.derivationPath?.endsWith('/0')),
  );
  if (existing?.address) return existing;
  const derived = deriveAddress(sym, phrase, 0);
  if (!derived) throw new Error('Derive failed');
  return addLocalWallet({
    symbol: sym,
    label: 'Primary',
    address: derived.address,
    derivationPath: derived.path,
    accountIndex: derived.accountIndex,
    source: 'local',
  });
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
