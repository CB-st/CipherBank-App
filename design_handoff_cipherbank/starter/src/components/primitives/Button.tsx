import React from 'react';
import { Text, ActivityIndicator, ViewStyle } from 'react-native';
import { color, radius, shadow, font } from '@/theme';
import { PressableScale } from './PressableScale';

type Props = {
  label: string;
  onPress?: () => void;
  busy?: boolean;
  disabled?: boolean;
  variant?: 'gold' | 'ghost';
  style?: ViewStyle;
  testID?: string;
};

/** Primary action = gold. One per screen. Busy -> inline spinner + disabled (optimistic pending). */
export function Button({ label, onPress, busy, disabled, variant = 'gold', style, testID }: Props) {
  const gold = variant === 'gold';
  const inactive = busy || disabled;
  return (
    <PressableScale
      testID={testID}
      accessibilityLabel={label}
      onPress={onPress}
      disabled={inactive}
      scaleTo={0.97}
      style={[
        {
          borderRadius: radius.button,
          paddingVertical: 16,
          alignItems: 'center',
          justifyContent: 'center',
          flexDirection: 'row',
          gap: 9,
          backgroundColor: gold ? (busy ? '#F0D894' : color.gold) : 'transparent',
          borderWidth: gold ? 0 : 1,
          borderColor: '#ffffff2e',
          opacity: disabled && !busy ? 0.55 : 1,
        },
        gold ? shadow.gold : null,
        style,
      ]}
    >
      {busy ? <ActivityIndicator size="small" color={color.ink} /> : null}
      <Text style={{ fontFamily: font.body, fontWeight: '800', fontSize: 16, color: gold ? color.ink : color.onDark }}>
        {label}
      </Text>
    </PressableScale>
  );
}
