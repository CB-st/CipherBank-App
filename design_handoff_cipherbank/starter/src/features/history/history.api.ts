import { api } from '@/lib/apiClient';
import type { Point } from '@/components/chart/chartMath';
import type { Range } from '@/components/chart/RangeToggle';

export interface HistorySeries { label: string; symbol: string; points: Point[]; }

/** Portfolio value over time + optional comparison assets, for a range. */
export const getHistory = (range: Range, compare: string[] = []) =>
  api.get<{ series: HistorySeries[] }>('/history?range=' + range + (compare.length ? '&compare=' + compare.join(',') : ''));
