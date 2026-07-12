import React from 'react';
import { Pressable, StyleProp, ViewStyle } from 'react-native';
import Animated, {
  useAnimatedStyle,
  useSharedValue,
  withSpring,
} from 'react-native-reanimated';

const AnimatedPressable = Animated.createAnimatedComponent(Pressable);

type Props = {
  children: React.ReactNode;
  onPress?: () => void;
  disabled?: boolean;
  style?: StyleProp<ViewStyle>;
  scaleTo?: number;
  hitSlop?: number;
};

/** Spring scale + opacity press feedback for CTAs and tiles. */
export function PressableScale({
  children,
  onPress,
  disabled,
  style,
  scaleTo = 0.97,
  hitSlop,
}: Props) {
  const scale = useSharedValue(1);
  const opacity = useSharedValue(1);

  const animatedStyle = useAnimatedStyle(() => ({
    transform: [{ scale: scale.value }],
    opacity: opacity.value,
  }));

  return (
    <AnimatedPressable
      hitSlop={hitSlop}
      disabled={disabled}
      onPress={disabled ? undefined : onPress}
      onPressIn={() => {
        if (disabled) return;
        scale.value = withSpring(scaleTo, { damping: 18, stiffness: 320 });
        opacity.value = withSpring(0.92, { damping: 18, stiffness: 320 });
      }}
      onPressOut={() => {
        scale.value = withSpring(1, { damping: 14, stiffness: 280 });
        opacity.value = withSpring(1, { damping: 14, stiffness: 280 });
      }}
      style={[style, animatedStyle]}
    >
      {children}
    </AnimatedPressable>
  );
}
