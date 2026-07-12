import React, { useState } from 'react';
import { View, Text, LayoutChangeEvent } from 'react-native';
import Svg, { Path } from 'react-native-svg';
import { color, font } from '@/theme';
import { toPath, toIndexed, Point } from './chartMath';
import { SERIES_COLORS } from './LineChart';

export interface Series { label: string; points: Point[]; }

/**
 * Overlays multiple series on one axis, each normalized to % change from its start so
 * a wallet and BTC/ETH/etc. are comparable regardless of absolute value.
 * Width fills the parent (measured via onLayout).
 */
export function CompareChart({ series, height = 180 }: { series: Series[]; width?: number; height?: number }) {
  const [width, setWidth] = useState(0);
  const onLayout = (e: LayoutChangeEvent) => {
    const w = Math.floor(e.nativeEvent.layout.width);
    if (w > 0 && w !== width) setWidth(w);
  };

  const indexed = series.map((s) => toIndexed(s.points));
  const all = indexed.flat().map((p) => p.v);
  const lo = Math.min(...all, 0);
  const hi = Math.max(...all, 0);

  return (
    <View style={{ gap: 12 }} onLayout={onLayout}>
      {width > 0 ? (
        <Svg width={width} height={height}>
          {indexed.map((pts, i) => {
            const { line } = toPath(pts as Point[], width, height, 10, lo, hi);
            return (
              <Path
                key={i}
                d={line}
                stroke={SERIES_COLORS[i % SERIES_COLORS.length]}
                strokeWidth={2.2}
                fill="none"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            );
          })}
        </Svg>
      ) : (
        <View style={{ height }} />
      )}
      <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 12 }}>
        {series.map((s, i) => (
          <View key={s.label} style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
            <View
              style={{
                width: 10,
                height: 10,
                borderRadius: 3,
                backgroundColor: SERIES_COLORS[i % SERIES_COLORS.length],
              }}
            />
            <Text style={{ fontSize: 12, color: color.textMuted, fontFamily: font.body, fontWeight: '600' }}>
              {s.label}
            </Text>
          </View>
        ))}
      </View>
    </View>
  );
}
