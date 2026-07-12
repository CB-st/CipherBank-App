import { useQuery } from '@tanstack/react-query';
import { api } from '@/lib/apiClient';

export type RateRow = { symbol: string; usd: number; change24h: number };

export type RatesSnapshot = {
  rates: RateRow[];
  generatedAt?: number;
  ttlMs?: number;
};

export const RATES_QUERY_KEY = ['rates'] as const;

/** Live price cache — Convert / Home should prefer this over ad-hoc fixtures. */
export function useRatesCache() {
  return useQuery({
    queryKey: RATES_QUERY_KEY,
    queryFn: () => api.get<RatesSnapshot>('/rates'),
    staleTime: 10_000,
    refetchInterval: 15_000,
  });
}

export function rateUsd(snapshot: RatesSnapshot | undefined, symbol: string): number | undefined {
  return snapshot?.rates.find((r) => r.symbol === symbol.toUpperCase())?.usd;
}
