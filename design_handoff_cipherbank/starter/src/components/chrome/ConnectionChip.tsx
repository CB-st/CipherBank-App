import React from 'react';
import { View, Text } from 'react-native';
import { color, font } from '@/theme';

export function ConnectionChip({ online }: { online: boolean }) {
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: 5, backgroundColor: online ? '#3FA46A14' : '#C0574B14', borderRadius: 20, paddingHorizontal: 9, paddingVertical: 4 }}>
      <View style={{ width: 6, height: 6, borderRadius: 3, backgroundColor: online ? color.green : color.red }} />
      <Text style={{ fontFamily: font.mono, fontSize: 10, color: online ? '#2E7D51' : color.red }}>{online ? 'live' : 'offline'}</Text>
    </View>
  );
}
