import React from 'react';
import { View, Text, Pressable } from 'react-native';
import { color, radius, font } from '@/theme';

export function ErrorCard({ message = "Couldn't load.", onRetry }: { message?: string; onRetry?: () => void }) {
  return (
    <View
      style={{
        backgroundColor: color.surface,
        borderRadius: radius.card,
        padding: 22,
        alignItems: 'center',
        gap: 10,
      }}
    >
      <Text style={{ color: color.textMuted, fontSize: 14 }}>{message}</Text>
      {onRetry ? (
        <Pressable
          onPress={onRetry}
          style={{
            backgroundColor: color.track,
            borderRadius: radius.button,
            paddingVertical: 10,
            paddingHorizontal: 18,
          }}
        >
          <Text style={{ color: color.violet, fontWeight: '700', fontFamily: font.body }}>Retry</Text>
        </Pressable>
      ) : null}
    </View>
  );
}
