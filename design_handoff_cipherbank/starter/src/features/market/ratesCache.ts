import { useQuery } from '@tanstack/react-query';
import {
  fetchRatesViaPublicApi,
  type RateRow,
  type RatesSnapshot,
} from '@/features/market/publicMarket.api';

export type { RateRow, RatesSnapshot };

export const RATES_QUERY_KEY = ['rates'] as const;

/**
 * Live price cache from CipherBank public API
 * (POST /currencies + POST /iquote → USD), not legacy GET /rates.
 */
export function useRatesCache() {
  return useQuery({
    queryKey: RATES_QUERY_KEY,
    queryFn: () => fetchRatesViaPublicApi(),
    staleTime: 10_000,
    refetchInterval: 15_000,
  });
}

export function rateUsd(snapshot: RatesSnapshot | undefined, symbol: string): number | undefined {
  return snapshot?.rates.find((r) => r.symbol === symbol.toUpperCase())?.usd;
}
