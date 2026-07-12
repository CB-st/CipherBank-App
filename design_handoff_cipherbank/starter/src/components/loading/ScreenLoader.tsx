import React from 'react';
import { View } from 'react-native';
import { Skeleton } from '../primitives/Skeleton';

/** Generic full-screen skeleton stack for screens that load a form/detail. */
export function ScreenLoader({ blocks = 3 }: { blocks?: number }) {
  return (
    <View style={{ gap: 13 }}>
      {Array.from({ length: blocks }).map((_, i) => (
        <Skeleton key={i} style={{ width: '100%', height: i === 0 ? 120 : 72, borderRadius: 20 } as any} />
      ))}
    </View>
  );
}
