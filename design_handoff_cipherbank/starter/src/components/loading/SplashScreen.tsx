import React, { useEffect, useRef } from 'react';
import { View, Text, Animated } from 'react-native';
import Svg, { Rect } from 'react-native-svg';
import { color, font } from '@/theme';

/** Cold-start splash. Mark shown while session/keys hydrate. Pulses gently. */
export function SplashScreen({ label = 'Securing your session…' }: { label?: string }) {
  const a = useRef(new Animated.Value(0.6)).current;
  useEffect(() => {
    Animated.loop(Animated.sequence([
      Animated.timing(a, { toValue: 1, duration: 800, useNativeDriver: true }),
      Animated.timing(a, { toValue: 0.6, duration: 800, useNativeDriver: true }),
    ])).start();
  }, []);
  return (
    <View style={{ flex: 1, backgroundColor: color.ink, alignItems: 'center', justifyContent: 'center', gap: 20 }}>
      <Animated.View style={{ opacity: a }}>
        <Svg width={64} height={64} viewBox="0 0 46 46">
          <Rect x={10} y={10} width={26} height={26} rx={6} transform="rotate(45 23 23)" fill="none" stroke={color.gold} strokeWidth={2.8} />
          <Rect x={16.5} y={16.5} width={13} height={13} rx={3} transform="rotate(45 23 23)" fill={color.violet} />
        </Svg>
      </Animated.View>
      <Text style={{ fontFamily: font.mono, fontSize: 12, letterSpacing: 1, color: color.onDarkSubtle }}>{label}</Text>
    </View>
  );
}
