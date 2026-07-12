import React from 'react';
import { View, ScrollView } from 'react-native';
import { color } from '@/theme';

/** Shell wrapper: fixed header, scrollable body. Renders synchronously — never awaits data. */
export function ScreenScaffold({ header, children }: { header?: React.ReactNode; children: React.ReactNode }) {
  return (
    <View style={{ flex: 1, backgroundColor: color.canvas }}>
      {header}
      <ScrollView contentContainerStyle={{ padding: 18, paddingBottom: 96, gap: 13 }}>
        {children}
      </ScrollView>
    </View>
  );
}
