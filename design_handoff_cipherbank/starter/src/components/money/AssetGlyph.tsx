import React from 'react';
import { View, Text } from 'react-native';
import { assetSpec } from '@/features/assets/assetConfig';

export function AssetGlyph({ symbol, size = 36 }: { symbol: string; size?: number }) {
  const s = assetSpec(symbol);
  return (
    <View style={{ width: size, height: size, borderRadius: size * 0.28, backgroundColor: s.tint,
      alignItems: 'center', justifyContent: 'center' }}>
      <Text style={{ color: s.fg, fontWeight: '700', fontSize: size * 0.5 }}>{s.glyph}</Text>
    </View>
  );
}
