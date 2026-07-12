import React from 'react';
import { View, Text } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import type { BottomTabBarProps } from '@react-navigation/bottom-tabs';
import { color, font, useTheme } from '@/theme';
import { Icon, IconName } from '../primitives/Icon';
import { PressableScale } from '../primitives/PressableScale';

const ICONS: Record<string, IconName> = {
  Home: 'home',
  Convert: 'convert',
  Pay: 'pay',
  Send: 'send',
  Receive: 'receive',
  Profile: 'profile',
};

export function TabBar({ state, navigation }: BottomTabBarProps) {
  const insets = useSafeAreaInsets();
  useTheme(); // re-render on appearance change
  return (
    <View
      style={{
        flexDirection: 'row',
        justifyContent: 'space-between',
        paddingHorizontal: 12,
        paddingTop: 10,
        paddingBottom: Math.max(insets.bottom, 12),
        backgroundColor: color.tabBar,
        borderTopWidth: 1,
        borderTopColor: color.tabBarBorder,
      }}
    >
      {state.routes.map((route, i) => {
        const active = state.index === i;
        const c = active ? color.goldDark : color.textSubtle;
        return (
          <PressableScale
            key={route.key}
            onPress={() => navigation.navigate(route.name)}
            scaleTo={0.92}
            style={{ alignItems: 'center', gap: 4, flex: 1 }}
          >
            <Icon name={ICONS[route.name] ?? 'home'} size={20} color={c} strokeWidth={2.2} />
            <Text style={{ fontSize: 9, fontWeight: active ? '700' : '600', fontFamily: font.body, color: c }}>
              {route.name}
            </Text>
          </PressableScale>
        );
      })}
    </View>
  );
}
