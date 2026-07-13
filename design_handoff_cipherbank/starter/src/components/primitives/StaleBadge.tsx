import React from 'react';
import { View, ActivityIndicator, StyleSheet } from 'react-native';
import { color } from '@/theme';

/** Non-blocking corner hint that fresher data is loading. */
export function StaleBadge({ visible }: { visible?: boolean }) {
  if (!visible) return null;
  return (
    <View style={styles.wrap} pointerEvents="none">
      <ActivityIndicator size="small" color={color.goldDark} />
    </View>
  );
}

const styles = StyleSheet.create({
  wrap: {
    position: 'absolute',
    top: 10,
    right: 10,
    width: 22,
    height: 22,
    borderRadius: 11,
    backgroundColor: 'rgba(0,0,0,0.25)',
    alignItems: 'center',
    justifyContent: 'center',
  },
});
