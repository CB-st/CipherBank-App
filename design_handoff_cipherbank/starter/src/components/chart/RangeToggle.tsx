import React from 'react';
import { View, Pressable, Text } from 'react-native';
import { color, radius, font } from '@/theme';

export type Range = '1D' | '1W' | '1M' | '1Y' | 'ALL';
export const RANGES: Range[] = ['1D', '1W', '1M', '1Y', 'ALL'];

export function RangeToggle({ value, onChange, onDark }: { value: Range; onChange: (r: Range) => void; onDark?: boolean }) {
  return (
    <View style={{ flexDirection: 'row', gap: 4, backgroundColor: onDark ? '#ffffff12' : color.track, borderRadius: radius.button, padding: 4 }}>
      {RANGES.map(r => {
        const on = r === value;
        return (
          <Pressable key={r} onPress={() => onChange(r)} style={{ flex: 1, alignItems: 'center', paddingVertical: 7, borderRadius: 8,
            backgroundColor: on ? color.gold : 'transparent' }}>
            <Text style={{ fontFamily: font.mono, fontSize: 11, fontWeight: '700', color: on ? color.ink : (onDark ? color.onDarkSubtle : color.textSubtle) }}>{r}</Text>
          </Pressable>
        );
      })}
    </View>
  );
}
