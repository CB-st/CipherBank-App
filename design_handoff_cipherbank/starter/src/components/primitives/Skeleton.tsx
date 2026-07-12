import React, { useEffect, useRef } from 'react';
import { Animated, View, ViewStyle } from 'react-native';
import { color, radius } from '@/theme';

/** Shimmer placeholder. Mirror the final layout so nothing reflows when data lands. */
export function Skeleton({ style }: { style?: ViewStyle }) {
  const x = useRef(new Animated.Value(0)).current;
  useEffect(() => {
    Animated.loop(Animated.timing(x, { toValue: 1, duration: 1200, useNativeDriver: true })).start();
  }, []);
  return (
    <View style={[{ backgroundColor: color.skeleton, borderRadius: radius.chip, overflow: 'hidden' }, style]}>
      <Animated.View
        style={{
          ...(style as object),
          position: 'absolute',
          top: 0,
          left: 0,
          right: 0,
          bottom: 0,
          backgroundColor: color.skeletonShine,
          opacity: 0.6,
          transform: [{ translateX: x.interpolate({ inputRange: [0, 1], outputRange: [-200, 200] }) }],
        }}
      />
    </View>
  );
}
