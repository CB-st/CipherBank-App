import React, { useState } from 'react';
import { View, Text, LayoutChangeEvent } from 'react-native';
import { color, radius, font } from '@/theme';
import { Skeleton } from '../primitives/Skeleton';
import { LineChart } from '../chart/LineChart';
import { formatUSD } from '@/lib/money';
import type { Point } from '../chart/chartMath';

export function BalanceHero({
  total,
  change,
  series,
  up = true,
  hidden,
}: {
  total: number;
  change: string;
  series?: Point[];
  up?: boolean;
  hidden?: boolean;
}) {
  const [chartW, setChartW] = useState(0);

  const onLayout = (e: LayoutChangeEvent) => {
    const w = Math.floor(e.nativeEvent.layout.width);
    if (w > 0 && w !== chartW) setChartW(w);
  };

  return (
    <View
      onLayout={onLayout}
      style={{ backgroundColor: color.deepPurple, borderRadius: radius.panel, padding: 20, overflow: 'hidden' }}
    >
      <Text style={{ color: color.onDarkSubtle, fontSize: 12, fontFamily: font.body }}>Total balance</Text>
      <Text
        style={{
          color: color.onDark,
          fontFamily: font.display,
          fontWeight: '700',
          fontSize: 38,
          letterSpacing: -1.5,
          marginVertical: 6,
        }}
      >
        {hidden ? '••••••' : formatUSD(total)}
      </Text>
      <View
        style={{
          alignSelf: 'flex-start',
          backgroundColor: up ? '#3FA46A22' : '#C0574B22',
          borderRadius: 20,
          paddingHorizontal: 9,
          paddingVertical: 4,
        }}
      >
        <Text style={{ color: up ? '#5FCE8F' : '#E08A7E', fontSize: 12, fontWeight: '700', fontFamily: font.body }}>
          {change}
        </Text>
      </View>
      {series && series.length > 1 && chartW > 0 ? (
        <View style={{ marginTop: 14, marginHorizontal: -20, marginBottom: -20 }}>
          <LineChart
            data={series}
            width={chartW}
            height={90}
            stroke={up ? '#5FCE8F' : color.red}
          />
        </View>
      ) : null}
    </View>
  );
}

BalanceHero.Skeleton = function () {
  return (
    <View style={{ backgroundColor: color.deepPurple, borderRadius: radius.panel, padding: 22 }}>
      <Skeleton style={{ width: 110, height: 12, marginBottom: 14 } as any} />
      <Skeleton style={{ width: 200, height: 34, marginBottom: 12 } as any} />
      <Skeleton style={{ width: 150, height: 20, marginBottom: 16 } as any} />
      <Skeleton style={{ width: '100%', height: 70 } as any} />
    </View>
  );
};
