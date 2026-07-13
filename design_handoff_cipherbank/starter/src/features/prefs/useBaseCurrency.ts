import { useMemo } from 'react';
import { usePrefs } from './usePrefs';
import { useRatesCache, rateUsd, type RatesSnapshot } from '@/features/market/ratesCache';
import type { BaseCurrency } from './prefs.types';
import {
  formatBaseChange,
  formatBaseValue,
  usdToBase,
  convertSeriesToBase,
} from '@/lib/money';
import type { HistoryPoint } from '@/features/history/history.api';
import type { Point } from '@/components/chart/chartMath';

export function useBaseCurrency() {
  const { prefs } = usePrefs();
  const rates = useRatesCache();
  const base = prefs.baseCurrency;

  const helpers = useMemo(
    () => ({
      base,
      formatTotal: (usd: number) => formatBaseValue(usd, base, rates.data),
      formatChange: (usdAmount: number, pct: number) => formatBaseChange(usdAmount, pct, base, rates.data),
      toBase: (usd: number) => usdToBase(usd, base, rates.data),
      convertSeries: (points: HistoryPoint[] | undefined): Point[] | undefined => {
        if (!points?.length) return undefined;
        return convertSeriesToBase(points, base, rates.data);
      },
      rateReady: isRateReadyForBase(base, rates.data),
      ratesStale: rates.isFetching && !!rates.data,
    }),
    [base, rates.data, rates.isFetching],
  );

  return helpers;
}

function isRateReadyForBase(base: BaseCurrency, snap: RatesSnapshot | undefined): boolean {
  if (base === 'USD') return true;
  const r = rateUsd(snap, base);
  return r != null && r > 0;
}
