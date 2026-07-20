import { useQuery } from '@tanstack/react-query';
import { useMock } from '@/mocks';
import { postCurrencies } from '@/features/market/publicMarket.api';
import { toAppSymbol } from '@/lib/publicCurrency';
import { listAssets } from '@/features/assets/assetConfig';

export const CURRENCIES_QUERY_KEY = ['public-currencies'] as const;

/** Supported public API currencies as app tickers; falls back to static catalog in mock mode. */
export function useCurrencies() {
  return useQuery({
    queryKey: CURRENCIES_QUERY_KEY,
    queryFn: async () => {
      if (useMock()) {
        return listAssets()
          .filter((a) => a.enabled !== false)
          .map((a) => a.symbol);
      }
      const { CURRENCIES } = await postCurrencies();
      const list = Array.isArray(CURRENCIES) ? CURRENCIES : [];
      return list.map((code) => toAppSymbol(code));
    },
    staleTime: 60_000,
  });
}
