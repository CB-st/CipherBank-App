import { getDb } from './db';
import type { LocalWalletDraft, WalletMode, WalletSource, WalletSyncStatus } from '@/features/portfolio/portfolio.types';

type WalletRow = {
  id: string;
  symbol: string;
  label: string;
  address: string | null;
  derivation_path: string | null;
  account_index: number | null;
  source: string;
  mode: string | null;
  sync_json: string | null;
  view_key_fp: string | null;
  created_at: number;
};

function rowToDraft(r: WalletRow): LocalWalletDraft {
  let sync: WalletSyncStatus | undefined;
  if (r.sync_json) {
    try {
      sync = JSON.parse(r.sync_json) as WalletSyncStatus;
    } catch {
      sync = undefined;
    }
  }
  return {
    id: r.id,
    symbol: r.symbol,
    label: r.label,
    address: r.address ?? undefined,
    derivationPath: r.derivation_path ?? undefined,
    accountIndex: r.account_index ?? undefined,
    source: r.source as WalletSource,
    mode: (r.mode as WalletMode) || undefined,
    sync,
    viewKeyFingerprint: r.view_key_fp ?? undefined,
    createdAt: r.created_at,
  };
}

export async function listWallets(): Promise<LocalWalletDraft[]> {
  const db = await getDb();
  const rows = await db.getAllAsync<WalletRow>('SELECT * FROM wallets ORDER BY created_at ASC');
  return rows.map(rowToDraft);
}

export async function replaceAllWallets(list: LocalWalletDraft[]): Promise<void> {
  const db = await getDb();
  await db.withTransactionAsync(async () => {
    await db.runAsync('DELETE FROM wallets');
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
  });
}

export async function upsertWallet(w: LocalWalletDraft): Promise<void> {
  const db = await getDb();
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

export async function deleteWallet(id: string): Promise<void> {
  const db = await getDb();
  await db.runAsync('DELETE FROM wallets WHERE id = ?', id);
}

/** Distinct held symbols from the wallet index (P2-safe). */
export async function heldSymbolsFromWallets(): Promise<string[]> {
  const db = await getDb();
  const rows = await db.getAllAsync<{ symbol: string }>('SELECT DISTINCT symbol FROM wallets ORDER BY symbol');
  return rows.map((r) => r.symbol.toUpperCase());
}
