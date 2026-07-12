import React from 'react';
import { View, Text, Switch } from 'react-native';
import { color, radius, shadow, font } from '@/theme';
import { useCora } from '@/features/cora/useCora';

/** Settings toggle: show/hide Cora as a visible assistant. */
export function CoraSettingRow() {
  const { enabled, toggle } = useCora();
  return (
    <View style={[{ backgroundColor: color.surface, borderRadius: radius.card, padding: 16, flexDirection: 'row', alignItems: 'center', gap: 12 }, shadow.card]}>
      <View style={{ flex: 1 }}>
        <Text style={{ fontWeight: '700', fontSize: 15, color: color.text }}>Cora assistant</Text>
        <Text style={{ fontSize: 13, color: color.textSubtle, marginTop: 2 }}>Show Cora's floating tips across the app.</Text>
      </View>
      <Switch value={enabled} onValueChange={toggle} trackColor={{ true: color.gold }} />
    </View>
  );
}
