import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { getHistoryBulk, type HistoryGranularity, type HistoryQuery } from './history.api';
import type { Range } from '@/components/chart/RangeToggle';

export function useHistory(
  range: Range,
  compare: string[] = [],
  opts: { granularity?: HistoryGranularity; from?: number; to?: number } = {},
) {
  const query: HistoryQuery = {
    range,
    symbols: compare,
    granularity: opts.granularity ?? defaultGranularity(range),
    from: opts.from,
    to: opts.to,
  };
  return useQuery({
    queryKey: ['history', query.range, query.symbols?.join(','), query.granularity, query.from, query.to],
    queryFn: () => getHistoryBulk(query),
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
