import { color } from '@/theme';
import { assetSpec } from '@/features/assets/assetConfig';
import type { BaseCurrency } from '@/features/prefs/prefs.types';
import { rateUsd, type RatesSnapshot } from '@/features/market/ratesCache';
import type { HistoryPoint } from '@/features/history/history.api';
import type { Point } from '@/components/chart/chartMath';

/** Format an amount in ASSET units, using that asset's display decimals. Strings = bignumber-safe. */
export function formatAsset(symbol: string, amount: string | number): string {
  const spec = assetSpec(symbol);
  const n = typeof amount === 'string' ? Number(amount) : amount;
  const s = n.toLocaleString('en-US', { minimumFractionDigits: spec.decimals, maximumFractionDigits: spec.decimals });
  return spec.type === 'fiat' ? (spec.fiatSymbol ?? '') + s : s + ' ' + symbol;
}

/** Format a USD value for display. */
export const formatUSD = (n: number) =>
  '$' + n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

/** Format any fiat by symbol. */
export function formatFiat(symbol: string, n: number): string {
  const spec = assetSpec(symbol);
  return (spec.fiatSymbol ?? '') + n.toLocaleString('en-US', { minimumFractionDigits: spec.decimals, maximumFractionDigits: spec.decimals });
}

export const signedPct = (pct: number) => (pct >= 0 ? '+' : '') + pct + '%';
export const changeColor = (pct: number) => (pct >= 0 ? color.green : color.red);

/** Convert USD notional to user's base currency using cached rates. */
export function usdToBase(usd: number, base: BaseCurrency, rates: RatesSnapshot | undefined): number {
  if (base === 'USD') return usd;
  const rate = rateUsd(rates, base);
  if (rate == null || rate <= 0) return usd;
  return usd / rate;
}

export function formatBaseValue(usd: number, base: BaseCurrency, rates: RatesSnapshot | undefined): string {
  const amount = usdToBase(usd, base, rates);
  if (base === 'BTC') return formatAsset('BTC', amount);
  if (base === 'USD') return formatUSD(amount);
  return formatFiat(base, amount);
}

export function formatBaseChange(
  usdAmount: number,
  pct: number,
  base: BaseCurrency,
  rates: RatesSnapshot | undefined,
): string {
  const arrow = pct >= 0 ? '▲ +' : '▼ ';
  const amt = formatBaseValue(Math.abs(usdAmount), base, rates);
  return arrow + amt + ' · ' + pct + '% today';
}

/** Client-side WALLET series conversion — present USD until base rate is ready. */
export function convertSeriesToBase(
  points: HistoryPoint[],
  base: BaseCurrency,
  rates: RatesSnapshot | undefined,
): Point[] {
  if (base === 'USD') return points.map((p) => ({ t: p.t, v: p.v }));
  const rate = rateUsd(rates, base);
  if (rate == null || rate <= 0) return points.map((p) => ({ t: p.t, v: p.v }));
  return points.map((p) => ({ t: p.t, v: p.v / rate }));
}
