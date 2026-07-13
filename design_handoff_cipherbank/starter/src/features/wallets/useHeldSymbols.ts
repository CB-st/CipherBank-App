import { useMemo } from 'react';
import { useLocalWallets } from '@/features/wallets/useLocalWallets';
import { usePortfolio } from '@/features/portfolio/usePortfolio';

/**
 * Distinct symbols the user holds (SQLite wallet index ∪ portfolio holdings).
 * Used for P1 history / P2 rates — never hardcodes BTC/ETH alone.
 */
export function useHeldSymbols(): string[] {
  const { drafts } = useLocalWallets();
  const { data } = usePortfolio();

  return useMemo(() => {
    const set = new Set<string>();
    for (const d of drafts) set.add(d.symbol.toUpperCase());
    for (const h of data?.holdings ?? []) set.add(h.symbol.toUpperCase());
    return Array.from(set).sort();
  }, [drafts, data?.holdings]);
}
