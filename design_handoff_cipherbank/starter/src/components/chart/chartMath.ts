export interface Point { t: number; v: number; }

/** Map a series to SVG path 'd' within w x h, with vertical padding. */
export function toPath(series: Point[], w: number, h: number, pad = 6, min?: number, max?: number) {
  if (series.length < 2) return { line: '', area: '', pts: [] as { x: number; y: number }[] };
  const xs = series.map(p => p.t), vs = series.map(p => p.v);
  const x0 = Math.min(...xs), x1 = Math.max(...xs);
  const lo = min ?? Math.min(...vs), hi = max ?? Math.max(...vs);
  const sx = (t: number) => ((t - x0) / (x1 - x0 || 1)) * w;
  const sy = (v: number) => h - pad - ((v - lo) / (hi - lo || 1)) * (h - pad * 2);
  const pts = series.map(p => ({ x: sx(p.t), y: sy(p.v) }));
  const line = pts.map((p, i) => (i ? 'L' : 'M') + p.x.toFixed(1) + ' ' + p.y.toFixed(1)).join(' ');
  const area = line + ' L' + w + ' ' + h + ' L0 ' + h + ' Z';
  return { line, area, pts };
}

/** Normalize a series to % change vs its first point (for comparing assets on one axis). */
export function toIndexed(series: Point[]): Point[] {
  if (!series.length) return series;
  const base = series[0].v || 1;
  return series.map(p => ({ t: p.t, v: (p.v / base - 1) * 100 }));
}
