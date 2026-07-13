import { queryClient } from '@/lib/queryClient';
import { getDb } from '@/features/persist/db';
import { listWallets, heldSymbolsFromWallets } from '@/features/persist/walletsRepo';
import { loadPrefs } from '@/features/persist/prefsRepo';
import { getRatesSnapshot, upsertRatesSnapshot, setSyncMeta } from '@/features/persist/marketRepo';
import { seedAchRecipientsIfEmpty } from '@/features/persist/recipientsRepo';
import { RATES_QUERY_KEY, type RatesSnapshot } from '@/features/market/ratesCache';
import { api } from '@/lib/apiClient';

export type BootstrapResult = {
  walletCount: number;
  heldSymbols: string[];
  ratesHydrated: number;
};

/**
 * Cold-start **P2 only**: open DB, migrate, hydrate RQ with wallet index + prefs + held rates.
 * Does not pull full OHLC into memory.
 */
export async function runP2Bootstrap(): Promise<BootstrapResult> {
  await getDb();
  try {
    await seedAchRecipientsIfEmpty();
  } catch {
    /* demo seed is best-effort */
  }
  const wallets = await listWallets();
  const held = await heldSymbolsFromWallets();
  const prefs = await loadPrefs();

  queryClient.setQueryData(['localWallets'], wallets);
  queryClient.setQueryData(['prefs', 'local'], prefs);

  let rates = await getRatesSnapshot(held.length ? held : undefined);
  if (!rates.length && held.length) {
    try {
      const snap = await api.get<RatesSnapshot>('/rates');
      const filtered = snap.rates.filter((r) => held.includes(r.symbol.toUpperCase()));
      if (filtered.length) {
        await upsertRatesSnapshot(filtered);
        rates = filtered;
      } else if (snap.rates.length) {
        // First launch: keep only held if we know them; else store none (avoid all-coin RAM)
        await upsertRatesSnapshot(
          held.length ? snap.rates.filter((r) => held.includes(r.symbol.toUpperCase())) : [],
        );
        rates = await getRatesSnapshot(held);
      }
      queryClient.setQueryData(RATES_QUERY_KEY, {
        ...snap,
        rates: held.length
          ? snap.rates.filter((r) => held.includes(r.symbol.toUpperCase()))
          : snap.rates.slice(0, 8),
      });
    } catch {
      /* offline — SQLite snapshot only */
    }
  } else if (rates.length) {
    queryClient.setQueryData(RATES_QUERY_KEY, {
      rates,
      generatedAt: Date.now(),
      ttlMs: 60_000,
    });
  }

  await setSyncMeta('last_p2_bootstrap', String(Date.now()));

  return {
    walletCount: wallets.length,
    heldSymbols: held,
    ratesHydrated: rates.length,
  };
}
