import React from 'react';
import { View, Text } from 'react-native';
import { color, radius, shadow, font } from '@/theme';
import { CompareChart, Series } from '../chart/CompareChart';
import { RangeToggle, Range } from '../chart/RangeToggle';
import { Skeleton } from '../primitives/Skeleton';
import { StaleBadge } from '../primitives/StaleBadge';

/** Wallet value over time vs comparison assets (indexed to % so they share one axis). */
export function PerformanceCard({
  series,
  loading,
  range,
  onRange,
  stale,
}: {
  series: Series[];
  loading?: boolean;
  range: Range;
  onRange: (r: Range) => void;
  stale?: boolean;
}) {
  return (
    <View style={[{ backgroundColor: color.surface, borderRadius: radius.card, padding: 16, gap: 12 }, shadow.card]}>
      <View style={{ flexDirection: 'row', justifyContent: 'space-between', alignItems: 'center' }}>
        <Text style={{ fontWeight: '800', fontSize: 15, color: color.text, fontFamily: font.body }}>Performance</Text>
        <Text style={{ fontFamily: font.mono, fontSize: 11, color: color.textSubtle }}>% change</Text>
      </View>
      <View style={{ position: 'relative', minHeight: 180 }}>
        <StaleBadge visible={stale && !loading && series.length > 0} />
        {loading ? <Skeleton style={{ width: '100%', height: 180 } as any} /> : <CompareChart series={series} />}
      </View>
      <RangeToggle value={range} onChange={onRange} />
    </View>
  );
}
