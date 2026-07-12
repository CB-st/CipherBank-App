import type { Point } from '@/components/chart/chartMath';
import type { Range } from '@/components/chart/RangeToggle';

const N: Record<Range, number> = { '1D': 24, '1W': 28, '1M': 30, '1Y': 52, ALL: 60 };

/** Deterministic-ish walk for local dev before /history is live. */
export function mockSeries(range: Range, start = 100000, drift = 0.02, vol = 0.03): Point[] {
  const n = N[range]; const now = Date.now(); const step = (86400e3) / 4;
  let v = start; const out: Point[] = [];
  for (let i = 0; i < n; i++) {
    v = v * (1 + drift / n + (Math.sin(i * 1.7) + Math.cos(i * 0.6)) * vol / n);
    out.push({ t: now - (n - i) * step, v: Math.round(v) });
  }
  return out;
}
