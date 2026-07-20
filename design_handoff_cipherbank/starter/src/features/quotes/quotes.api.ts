import { iquoteAppPair } from '@/features/market/publicMarket.api';
import { toAppSymbol } from '@/lib/publicCurrency';

/**
 * App-facing quote lock. Wire format uses public API POST /iquote
 * (SCREAMING_SNAKE · BITCOIN/MONERO/USD · number amounts).
 * `quoteId` / `expiresAt` are client-side lock metadata for Convert settle UX
 * until a durable product lock endpoint exists on CipherBank-src.
 */
export interface Quote {
  quoteId: string;
  rate: number;
  expiresAt: number;
  from: string;
  to: string;
  amountOut?: string;
  fee?: string;
  /** True when the quote is client-indicative (live /iquote) rather than server-locked. */
  indicative?: boolean;
}

const QUOTE_TTL_MS = 15_000;

export async function requestQuote(from: string, to: string, amount: string): Promise<Quote> {
  const input = Number(amount);
  if (!Number.isFinite(input) || input <= 0) {
    return {
      quoteId: `q_empty_${Date.now()}`,
      from,
      to,
      rate: 0,
      amountOut: '0',
      expiresAt: Date.now() + QUOTE_TTL_MS,
      fee: '0.00',
      indicative: true,
    };
  }

  const pub = await iquoteAppPair(from, to, input);
  const out = Number(pub.OUTPUT_AMOUNT);
  const rate = input === 0 ? 0 : out / input;

  return {
    quoteId: `q_${Date.now()}_${toAppSymbol(pub.INPUT_CURRENCY)}_${toAppSymbol(pub.OUTPUT_CURRENCY)}`,
    from: toAppSymbol(pub.INPUT_CURRENCY),
    to: toAppSymbol(pub.OUTPUT_CURRENCY),
    rate,
    amountOut: String(out),
    expiresAt: Date.now() + QUOTE_TTL_MS,
    fee: '0.00',
    indicative: true,
  };
}
