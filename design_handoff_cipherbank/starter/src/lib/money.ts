import { color } from '@/theme';
import { assetSpec } from '@/features/assets/assetConfig';

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
