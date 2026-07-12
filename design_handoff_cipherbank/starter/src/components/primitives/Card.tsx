import React from 'react';
import { View, ViewStyle } from 'react-native';
import { color, radius, shadow } from '@/theme';

export function Card({ children, dark, style }: { children: React.ReactNode; dark?: boolean; style?: ViewStyle }) {
  return (
    <View
      style={[
        {
          borderRadius: radius.card,
          padding: 18,
          backgroundColor: dark ? color.deepPurple : color.surface,
        },
        dark ? null : shadow.card,
        style,
      ]}
    >
      {children}
    </View>
  );
}
