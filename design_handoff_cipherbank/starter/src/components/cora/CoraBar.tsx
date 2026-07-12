import React from 'react';
import { View, Text, Image } from 'react-native';
import { color, radius, font } from '@/theme';

/** Cora's slot + one dry line. Supply the transparent-PNG cutout via 'source'. */
export function CoraBar({ line, source }: { line: string; source?: any }) {
  return (
    <View style={{ flexDirection: 'row', alignItems: 'center', gap: 11,
      backgroundColor: color.deepPurple, borderRadius: radius.card, padding: 10 }}>
      <View style={{ width: 42, height: 42, borderRadius: 21, backgroundColor: '#4A3D63', overflow: 'hidden' }}>
        {source ? <Image source={source} style={{ width: 42, height: 42 }} /> : null}
      </View>
      <Text style={{ flex: 1, color: '#E9E4F2', fontFamily: font.body, fontSize: 13, lineHeight: 18 }}>{line}</Text>
    </View>
  );
}
