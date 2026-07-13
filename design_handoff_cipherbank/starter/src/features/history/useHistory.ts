import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { getHistoryBulk, type HistoryGranularity, type HistoryQuery } from './history.api';
import type { Range } from '@/components/chart/RangeToggle';
import { jobQueue } from '@/features/bootstrap/jobQueue';
import { upsertOhlcPoints } from '@/features/persist/marketRepo';

export function useHistory(
  range: Range,
  compare: string[] = [],
  opts: { granularity?: HistoryGranularity; from?: number; to?: number } = {},
) {
  const granularity = opts.granularity ?? defaultGranularity(range);
  const query: HistoryQuery = {
    range,
    symbols: compare,
    granularity,
    from: opts.from,
    to: opts.to,
  };
  return useQuery({
    queryKey: ['history', query.range, query.symbols?.join(','), query.granularity, query.from, query.to],
    queryFn: async () => {
      const data = await getHistoryBulk(query);
      // P1 write-through: persist requested series into SQLite (no bulk RAM beyond this response).
      for (const series of data.series) {
        if (series.symbol === 'WALLET') continue;
        const sym = series.symbol.toUpperCase();
        jobQueue.enqueue({
          id: `p1-ohlc-write-${sym}-${granularity}`,
          priority: 1,
          symbol: sym,
          run: async () => {
            await upsertOhlcPoints(sym, series.granularity ?? granularity, series.points);
          },
        });
      }
      return data;
    },
    enabled: compare.length > 0,
    placeholderData: keepPreviousData,
    staleTime: 30_000,
  });
}

function defaultGranularity(range: Range): HistoryGranularity {
  if (range === '1D') return '5m';
  if (range === '1W') return '1h';
  if (range === '1M') return '1h';
  return '1d';
}
