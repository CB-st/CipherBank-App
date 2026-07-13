import { useMemo } from 'react';
import type { Holding } from './portfolio.types';

/** Split portfolio holdings by user-enabled currency prefs (hide ≠ delete). */
export function useVisibleHoldings(
  holdings: Holding[] | undefined,
  enabledCurrencies: string[],
): { visible: Holding[]; hidden: Holding[] } {
  return useMemo(() => {
    const enabled = new Set(enabledCurrencies.map((s) => s.toUpperCase()));
    const visible: Holding[] = [];
    const hidden: Holding[] = [];
    for (const h of holdings ?? []) {
      if (enabled.has(h.symbol.toUpperCase())) visible.push(h);
      else hidden.push(h);
    }
    return { visible, hidden };
  }, [holdings, enabledCurrencies]);
}
