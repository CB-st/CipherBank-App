import React from 'react';
import { View, Text, Pressable, ScrollView } from 'react-native';
import { color, radius, shadow, font } from '@/theme';
import { Header } from '@/components/chrome/Header';
import { CoraBar } from '@/components/cora/CoraBar';
import { Button } from '@/components/primitives/Button';
import { Icon } from '@/components/primitives/Icon';
import { useSession } from '@/features/session/useSession';
import { useCora } from '@/features/cora/useCora';
import { useToast } from '@/components/primitives/Toast';

/** Demo recovery phrase for UI — real keygen will replace this with on-device entropy. */
const PHRASE = [
  'cipher',
  'ledger',
  'violet',
  'anchor',
  'orbit',
  'quartz',
  'harbor',
  'nebula',
  'velvet',
  'prism',
  'ember',
  'signal',
];

export function KeysScreen({ navigation }: any) {
  const { createWallet } = useSession();
  const { lineFor } = useCora();
  const toast = useToast();

  return (
    <View style={{ flex: 1, backgroundColor: color.canvas }}>
      <Header title="Secure keys" onBack={() => navigation.goBack?.()} />
      <ScrollView contentContainerStyle={{ flexGrow: 1, padding: 22, paddingBottom: 40, gap: 16 }}>
        <View style={{ flexDirection: 'row', gap: 6 }}>
          {[0, 1, 2, 3].map((i) => (
            <View
              key={i}
              style={{ flex: 1, height: 4, borderRadius: 2, backgroundColor: i < 2 ? color.gold : '#E3E0DB' }}
            />
          ))}
        </View>

        <View
          style={{
            width: 60,
            height: 60,
            borderRadius: 18,
            backgroundColor: color.deepPurple,
            alignItems: 'center',
            justifyContent: 'center',
          }}
        >
          <Icon name="shield-check" size={28} color={color.gold} strokeWidth={2} />
        </View>
        <View>
          <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 26, letterSpacing: -0.8 }}>
            Your keys. Your money.
          </Text>
          <Text style={{ fontSize: 15, color: color.textMuted, lineHeight: 23, marginTop: 8, fontFamily: font.body }}>
            Cipherbank is self-custodied — we generate your recovery phrase on this device and never see it. Write it
            down.
          </Text>
        </View>

        <View
          style={[
            {
              backgroundColor: color.surface,
              borderRadius: radius.card,
              padding: 16,
              flexDirection: 'row',
              flexWrap: 'wrap',
              gap: 9,
            },
            shadow.card,
          ]}
        >
          {PHRASE.map((w, i) => (
            <View
              key={i}
              style={{
                width: '47%',
                flexDirection: 'row',
                alignItems: 'center',
                gap: 8,
                backgroundColor: color.canvas,
                borderRadius: 10,
                paddingVertical: 9,
                paddingHorizontal: 11,
              }}
            >
              <Text style={{ fontFamily: font.mono, fontSize: 11, color: '#A8A2B0' }}>
                {String(i + 1).padStart(2, '0')}
              </Text>
              <Text style={{ fontFamily: font.mono, fontSize: 13, fontWeight: '700' }}>{w}</Text>
            </View>
          ))}
        </View>

        <Pressable
          onPress={() => toast({ kind: 'ok', title: 'Phrase copied', sub: 'Store it offline — never in a screenshot.' })}
          style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'center', gap: 8 }}
        >
          <Icon name="copy" size={15} color={color.violet} strokeWidth={2.2} />
          <Text style={{ color: color.violet, fontWeight: '700', fontSize: 13, fontFamily: font.body }}>Copy phrase</Text>
        </Pressable>

        <CoraBar line={lineFor('keys')} />
        <Button label="I've saved it — continue" onPress={createWallet} />
      </ScrollView>
    </View>
  );
}
