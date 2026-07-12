import React from 'react';
import { View, Text } from 'react-native';
import { color, font } from '@/theme';

export function Pill({
  label,
  bg,
  fg,
  gold,
}: {
  label: string;
  bg?: string;
  fg?: string;
  gold?: boolean;
}) {
  const backgroundColor = gold ? '#F2C14E33' : bg ?? color.hairline;
  const textColor = gold ? color.goldDark : fg ?? color.textMuted;
  return (
    <View
      style={{
        backgroundColor,
        borderRadius: 20,
        paddingHorizontal: 10,
        paddingVertical: 4,
        alignSelf: 'flex-start',
      }}
    >
      <Text style={{ color: textColor, fontSize: 11, fontWeight: '700', fontFamily: font.body }}>{label}</Text>
    </View>
  );
}
