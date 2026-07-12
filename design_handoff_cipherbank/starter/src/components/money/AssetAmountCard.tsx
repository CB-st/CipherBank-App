import React from 'react';
import { View, Text, Pressable } from 'react-native';
import { color, radius, shadow, font } from '@/theme';
import { AssetGlyph } from './AssetGlyph';
import { Icon } from '../primitives/Icon';

/** From/To amount card. dark=receive side. onPickAsset opens the selector. */
export function AssetAmountCard({ label, amount, symbol, sub, dark, onPickAsset }:
  { label: string; amount: string; symbol: string; sub?: string; dark?: boolean; onPickAsset?: () => void }) {
  const txt = dark ? color.onDark : color.text;
  return (
    <View style={[{ borderRadius: radius.panel, padding: 16, backgroundColor: dark ? color.deepPurple : color.surface }, dark ? null : shadow.card]}>
      <Text style={{ fontSize: 12, color: dark ? color.onDarkSubtle : color.textSubtle, marginBottom: 8 }}>{label}</Text>
      <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
        <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 32, letterSpacing: -1, color: txt }}>{amount}</Text>
        <Pressable onPress={onPickAsset} style={{ flexDirection: 'row', alignItems: 'center', gap: 7,
          backgroundColor: dark ? '#ffffff14' : color.canvas, borderRadius: radius.pill, paddingVertical: 6, paddingHorizontal: 11 }}>
          <AssetGlyph symbol={symbol} size={26} />
          <Text style={{ fontWeight: '700', fontSize: 14, color: txt }}>{symbol}</Text>
          <Icon name="caret-down" size={12} color={dark ? color.onDarkSubtle : color.textSubtle} strokeWidth={3} />
        </Pressable>
      </View>
      {sub ? <Text style={{ fontFamily: font.mono, fontSize: 13, color: dark ? color.onDarkSubtle : color.textSubtle, marginTop: 6 }}>{sub}</Text> : null}
    </View>
  );
}
