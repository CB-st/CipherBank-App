import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { getHistory } from './history.api';
import type { Range } from '@/components/chart/RangeToggle';

export function useHistory(range: Range, compare: string[] = []) {
  return useQuery({
    queryKey: ['history', range, compare.join(',')],
    queryFn: () => getHistory(range, compare),
    placeholderData: keepPreviousData,   // keep the old chart while the new range loads
    staleTime: 30_000,
  });
}
