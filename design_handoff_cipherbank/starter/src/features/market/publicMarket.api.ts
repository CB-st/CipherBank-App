import { publicApi } from '@/lib/publicApiClient';
import { toAppSymbol, toPublicCurrency } from '@/lib/publicCurrency';

export type RateRow = { symbol: string; usd: number; change24h: number };

export type RatesSnapshot = {
  rates: RateRow[];
  generatedAt?: number;
  ttlMs?: number;
};

/** POST /currencies — CB_InitialAPIRef */
export type CurrenciesResponse = { CURRENCIES: string[] };

/** POST /iquote and POST /quote response — CB_InitialAPIRef */
export type PublicQuoteResponse = {
  INPUT_AMOUNT: number;
  INPUT_CURRENCY: string;
  OUTPUT_AMOUNT: number;
  OUTPUT_CURRENCY: string;
};

export type IquoteRequest = {
  INPUT_AMOUNT: number;
  INPUT_CURRENCY: string;
  OUTPUT_CURRENCY: string;
};

export type QuoteRequest = {
  INPUT_CURRENCY: string;
  OUTPUT_AMOUNT: number;
  OUTPUT_CURRENCY: string;
};

export function postCurrencies() {
  return publicApi.post<CurrenciesResponse>('/currencies', {});
}

export function postIquote(body: IquoteRequest) {
  return publicApi.post<PublicQuoteResponse>('/iquote', body);
}

export function postQuote(body: QuoteRequest) {
  return publicApi.post<PublicQuoteResponse>('/quote', body);
}

export function postTest() {
  return publicApi.post<Record<string, never>>('/test', {});
}

/**
 * Build a Home/Convert rates snapshot from the public API:
 * POST /currencies + POST /iquote (1 unit → USD) per currency.
 */
export async function fetchRatesViaPublicApi(): Promise<RatesSnapshot> {
  const { CURRENCIES } = await postCurrencies();
  const list = Array.isArray(CURRENCIES) ? CURRENCIES : [];
  const rows = await Promise.all(
    list.map(async (code) => {
      const pub = code.toUpperCase();
      if (pub === 'USD') {
        return { symbol: 'USD' as const, usd: 1, change24h: 0 };
      }
      const q = await postIquote({
        INPUT_AMOUNT: 1,
        INPUT_CURRENCY: pub,
        OUTPUT_CURRENCY: 'USD',
      });
      return {
        symbol: toAppSymbol(q.INPUT_CURRENCY),
        usd: Number(q.OUTPUT_AMOUNT),
        change24h: 0,
      };
    }),
  );

  if (!rows.some((r) => r.symbol === 'USD')) {
    rows.push({ symbol: 'USD', usd: 1, change24h: 0 });
  }

  return {
    rates: rows,
    generatedAt: Date.now(),
    ttlMs: 10_000,
  };
}

/** Input-amount quote (Convert: user typed "from" amount). */
export async function iquoteAppPair(fromSymbol: string, toSymbol: string, amount: number) {
  return postIquote({
    INPUT_AMOUNT: amount,
    INPUT_CURRENCY: toPublicCurrency(fromSymbol),
    OUTPUT_CURRENCY: toPublicCurrency(toSymbol),
  });
}

/** Output-amount quote (Convert: user typed "to" target). */
export async function quoteAppPair(fromSymbol: string, toSymbol: string, outputAmount: number) {
  return postQuote({
    INPUT_CURRENCY: toPublicCurrency(fromSymbol),
    OUTPUT_AMOUNT: outputAmount,
    OUTPUT_CURRENCY: toPublicCurrency(toSymbol),
  });
}
