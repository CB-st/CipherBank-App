import React from 'react';
import { View, Text, Image } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { color, font } from '@/theme';
import { Icon, IconName } from '../primitives/Icon';
import { ConnectionChip } from './ConnectionChip';
import { PressableScale } from '../primitives/PressableScale';

const brandMark = require('../../../assets/logo/cipherbank-app-icon.png');

/** Screen header. Either brand lockup (home) or back + title. Right slot: online chip / actions. */
export function Header({
  title,
  brand,
  online = true,
  onBack,
  rightIcon,
  onRight,
}: {
  title?: string;
  brand?: boolean;
  online?: boolean;
  onBack?: () => void;
  rightIcon?: IconName;
  onRight?: () => void;
}) {
  const insets = useSafeAreaInsets();
  return (
    <View
      style={{
        paddingTop: Math.max(insets.top, 12) + 8,
        paddingHorizontal: 20,
        paddingBottom: 8,
        flexDirection: 'row',
        alignItems: 'center',
        justifyContent: 'space-between',
      }}
    >
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 10 }}>
        {onBack ? (
          <PressableScale onPress={onBack} hitSlop={10}>
            <Icon name="back" color={color.text} strokeWidth={2.4} />
          </PressableScale>
        ) : null}
        {brand ? (
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
            <Image source={brandMark} style={{ width: 28, height: 28, borderRadius: 8 }} />
            <Text style={{ fontFamily: font.display, fontWeight: '800', fontSize: 16, letterSpacing: -0.4, color: color.text }}>
              cipher<Text style={{ color: color.goldDark }}>bank</Text>
            </Text>
          </View>
        ) : (
          <Text style={{ fontFamily: font.display, fontWeight: '800', fontSize: 20, color: color.text }}>{title}</Text>
        )}
      </View>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
        <ConnectionChip online={online} />
        {rightIcon ? (
          <PressableScale onPress={onRight} hitSlop={10}>
            <Icon name={rightIcon} color={color.text} />
          </PressableScale>
        ) : null}
      </View>
    </View>
  );
}
