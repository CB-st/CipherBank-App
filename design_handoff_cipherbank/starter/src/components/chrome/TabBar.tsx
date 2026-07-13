import React from 'react';
import { View, Text } from 'react-native';
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

/**
 * Bottom tabs. Background is flush to the physical screen bottom; icons sit above
 * the system gesture / home-indicator inset so nothing is clipped.
 */
export function TabBar({ state, navigation, insets }: BottomTabBarProps) {
  useTheme();
  const bottomInset = Math.max(insets.bottom, 8);

  return (
    <View
      style={{
        backgroundColor: color.tabBar,
        borderTopWidth: 1,
        borderTopColor: color.tabBarBorder,
        // Extend the bar to the screen edge; pad only the interactive row.
        paddingBottom: bottomInset,
      }}
    >
      <View
        style={{
          flexDirection: 'row',
          justifyContent: 'space-between',
          paddingHorizontal: 10,
          paddingTop: 10,
          paddingBottom: 6,
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
              style={{ alignItems: 'center', gap: 3, flex: 1, minHeight: 44, justifyContent: 'center' }}
            >
              <Icon name={ICONS[route.name] ?? 'home'} size={20} color={c} strokeWidth={2.2} />
              <Text
                numberOfLines={1}
                style={{ fontSize: 9, fontWeight: active ? '700' : '600', fontFamily: font.body, color: c }}
              >
                {route.name}
              </Text>
            </PressableScale>
          );
        })}
      </View>
    </View>
  );
}
