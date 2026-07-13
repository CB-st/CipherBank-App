import React, { useState } from 'react';
import { View, Text, Pressable, Image } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { color, radius, font } from '@/theme';
import { Icon } from '../primitives/Icon';
import { useCora } from '@/features/cora/useCora';

/**
 * Optional floating assistant. When Cora is enabled, a circular avatar FAB sits above the
 * tab bar; tapping toggles a speech bubble with the current screen's line. Fully dismissable.
 */
export function CoraAssistant({ screen }: { screen: string }) {
  const { enabled, lineFor, source } = useCora();
  const insets = useSafeAreaInsets();
  const [open, setOpen] = useState(false);
  if (!enabled) return null;
  // Tab bar ~56px content + safe inset — keep FAB clear of both.
  const bottom = 64 + Math.max(insets.bottom, 8);
  return (
    <View
      pointerEvents="box-none"
      style={{ position: 'absolute', right: 16, bottom, alignItems: 'flex-end', gap: 10 }}
    >
      {open ? (
        <View style={{ maxWidth: 240, backgroundColor: color.deepPurple, borderRadius: radius.card, padding: 14, gap: 6 }}>
          <Text style={{ fontFamily: font.mono, fontSize: 10, letterSpacing: 1, color: color.gold }}>CORA BYTE</Text>
          <Text style={{ color: '#E9E4F2', fontFamily: font.body, fontSize: 13, lineHeight: 18 }}>{lineFor(screen)}</Text>
        </View>
      ) : null}
      <Pressable
        onPress={() => setOpen((o) => !o)}
        style={{
          width: 56,
          height: 56,
          borderRadius: 28,
          backgroundColor: '#1C1430',
          borderWidth: 2,
          borderColor: color.gold,
          alignItems: 'center',
          justifyContent: 'center',
          overflow: 'hidden',
        }}
      >
        {source ? <Image source={source} style={{ width: 56, height: 56 }} /> : <Icon name="profile" color={color.gold} />}
      </Pressable>
    </View>
  );
}
