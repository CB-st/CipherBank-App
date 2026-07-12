import React from 'react';
import { View, Text } from 'react-native';
import { color, radius, font } from '@/theme';

/** Dumb: renders the server quote + client countdown. Gold when locked, red when expired. */
export function RateLockStrip({ rateLabel, secondsLeft, expired }: { rateLabel: string; secondsLeft: number; expired: boolean }) {
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between',
      backgroundColor: expired ? '#C0574B14' : '#F2C14E1A', borderWidth: 1,
      borderColor: expired ? '#C0574B44' : '#F2C14E44', borderRadius: radius.button, paddingHorizontal: 14, paddingVertical: 11 }}>
      <Text style={{ fontFamily: font.mono, fontSize: 13, fontWeight: '600', color: color.ink }}>{rateLabel}</Text>
      <View style={{ backgroundColor: expired ? '#C0574B22' : '#F2C14E33', borderRadius: 20, paddingHorizontal: 10, paddingVertical: 4 }}>
        <Text style={{ color: expired ? color.red : color.goldDark, fontSize: 12, fontWeight: '700' }}>
          {expired ? '● Rate expired — re-locking' : '● Rate locked · ' + secondsLeft + 's'}
        </Text>
      </View>
    </View>
  );
}
