import React from 'react';
import { View, Text, Image, ScrollView, Pressable } from 'react-native';
import Svg, { Rect } from 'react-native-svg';
import { color, font } from '@/theme';
import { Button } from '@/components/primitives/Button';
import { useCora } from '@/features/cora/useCora';
import { beginSetupPath } from '@/features/account/setupState';

export function WelcomeScreen({ navigation }: any) {
  const { source, lineFor } = useCora();

  const startNew = async () => {
    await beginSetupPath('new');
    navigation.navigate('Keys');
  };

  const startReturning = async () => {
    await beginSetupPath('returning');
    navigation.navigate('Keys');
  };

  return (
    <View style={{ flex: 1, backgroundColor: color.deepPurple }}>
      <ScrollView contentContainerStyle={{ flexGrow: 1, padding: 26, paddingTop: 70, paddingBottom: 40 }}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 10 }}>
          <Svg width={30} height={30} viewBox="0 0 46 46">
            <Rect
              x={10}
              y={10}
              width={26}
              height={26}
              rx={6}
              transform="rotate(45 23 23)"
              fill="none"
              stroke={color.gold}
              strokeWidth={2.8}
            />
            <Rect
              x={16.5}
              y={16.5}
              width={13}
              height={13}
              rx={3}
              transform="rotate(45 23 23)"
              fill={color.violet}
            />
          </Svg>
          <Text style={{ fontFamily: font.display, fontWeight: '800', fontSize: 18, color: '#fff' }}>
            cipher<Text style={{ color: color.gold }}>bank</Text>
          </Text>
        </View>

        <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', minHeight: 280, marginVertical: 24 }}>
          <View
            style={{
              width: 220,
              height: 280,
              borderRadius: 24,
              backgroundColor: '#1C1430',
              overflow: 'hidden',
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            {source ? (
              <Image source={source} style={{ width: 220, height: 280 }} />
            ) : (
              <Text style={{ color: '#8A7FA8', fontFamily: font.mono, fontSize: 12 }}>Cora</Text>
            )}
          </View>
        </View>

        <Text
          style={{
            fontFamily: font.mono,
            fontSize: 11,
            letterSpacing: 1.5,
            color: color.gold,
            marginBottom: 10,
          }}
        >
          CORA BYTE · YOUR DIGITAL TELLER
        </Text>
        <Text
          style={{
            fontFamily: font.display,
            fontWeight: '700',
            fontSize: 30,
            letterSpacing: -1,
            color: '#fff',
            lineHeight: 33,
          }}
        >
          Money in any form.{'\n'}Yours to keep.
        </Text>
        <Text style={{ fontSize: 15, color: color.onDarkMuted, lineHeight: 23, marginVertical: 12, fontFamily: font.body }}>
          {lineFor('welcomeNew')}
        </Text>

        <View style={{ flexDirection: 'row', gap: 6, marginBottom: 20 }}>
          {[0, 1, 2, 3].map((i) => (
            <View
              key={i}
              style={{ flex: 1, height: 4, borderRadius: 2, backgroundColor: i === 0 ? color.gold : '#ffffff22' }}
            />
          ))}
        </View>
        <Button label="Create my account" onPress={startNew} />
        <Pressable onPress={startReturning} style={{ marginTop: 16, paddingVertical: 8 }}>
          <Text style={{ textAlign: 'center', fontSize: 13, color: color.onDarkSubtle, fontFamily: font.body }}>
            Already use CipherBank?{' '}
            <Text style={{ color: color.gold, fontWeight: '700' }}>Set up this device</Text>
          </Text>
          <Text
            style={{
              textAlign: 'center',
              fontSize: 12,
              color: color.onDarkSubtle,
              fontFamily: font.body,
              marginTop: 6,
              lineHeight: 17,
              paddingHorizontal: 12,
            }}
          >
            {lineFor('welcomeReturning')}
          </Text>
        </Pressable>
      </ScrollView>
    </View>
  );
}
