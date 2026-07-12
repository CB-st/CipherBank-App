import { api } from '@/lib/apiClient';
import type { Range } from '@/components/chart/RangeToggle';

export type HistoryGranularity = '1m' | '5m' | '1h' | '1d';

export type HistoryPoint = {
  t: number;
  /** Chart close — required for existing chart math. */
  v: number;
  o?: number;
  h?: number;
  l?: number;
  c?: number;
};

export interface HistorySeries {
  label: string;
  symbol: string;
  granularity?: HistoryGranularity;
  points: HistoryPoint[];
}

export type HistoryMeta = { source: string; generatedAt: number };

export type HistoryQuery = {
  range: Range;
  symbols?: string[];
  /** @deprecated use symbols */
  compare?: string[];
  granularity?: HistoryGranularity;
  from?: number;
  to?: number;
};

export type HistoryResponse = { series: HistorySeries[]; meta?: HistoryMeta };

function buildQuery(q: HistoryQuery): string {
  const params = new URLSearchParams();
  params.set('range', q.range);
  const syms = q.symbols?.length ? q.symbols : q.compare;
  if (syms?.length) {
    params.set('symbols', syms.join(','));
    params.set('compare', syms.join(',')); // legacy mock / backend alias
  }
  if (q.granularity) params.set('granularity', q.granularity);
  if (q.from != null) params.set('from', String(q.from));
  if (q.to != null) params.set('to', String(q.to));
  return '/history?' + params.toString();
}

/** Bulk historical series (range + granularity + optional custom window). */
export const getHistory = (range: Range, compare: string[] = [], extra: Omit<HistoryQuery, 'range' | 'compare' | 'symbols'> = {}) =>
  api.get<HistoryResponse>(buildQuery({ range, symbols: compare, ...extra }));

export const getHistoryBulk = (q: HistoryQuery) => api.get<HistoryResponse>(buildQuery(q));
