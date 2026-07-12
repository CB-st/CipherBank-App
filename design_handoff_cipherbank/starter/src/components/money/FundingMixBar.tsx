import React from 'react';
import { View, Text } from 'react-native';
import { color, font } from '@/theme';
import { assetSpec } from '@/features/assets/assetConfig';

export interface MixSource { asset: string; value: number; }

/** Stacked coverage bar for pay-with-a-mix. Colors come from each asset's fg. */
export function FundingMixBar({ sources, total }: { sources: MixSource[]; total: number }) {
  const covered = sources.reduce((s, x) => s + x.value, 0);
  const pct = Math.min(100, Math.round((covered / total) * 100));
  return (
    <View>
      <View style={{ flexDirection: 'row', justifyContent: 'space-between', marginBottom: 8 }}>
        <Text style={{ fontWeight: '800', fontSize: 15 }}>Funding mix</Text>
        <Text style={{ fontFamily: font.mono, fontSize: 12, color: pct >= 100 ? color.green : color.red }}>covers {pct}%</Text>
      </View>
      <View style={{ flexDirection: 'row', height: 14, borderRadius: 8, overflow: 'hidden', gap: 2 }}>
        {sources.map((s) => (
          <View key={s.asset} style={{ flex: s.value, backgroundColor: assetSpec(s.asset).fg }} />
        ))}
      </View>
    </View>
  );
}
