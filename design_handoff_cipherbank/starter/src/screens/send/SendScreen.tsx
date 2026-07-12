import React, { useState } from 'react';
import { View, Text, Pressable, ScrollView } from 'react-native';
import { color, radius, font } from '@/theme';
import { Header } from '@/components/chrome/Header';
import { CoraBar } from '@/components/cora/CoraBar';
import { CoraAssistant } from '@/components/cora/CoraAssistant';
import { Card } from '@/components/primitives/Card';
import { Button } from '@/components/primitives/Button';
import { useSend } from '@/features/transfers/useSend';
import { useToast } from '@/components/primitives/Toast';
import { useCora } from '@/features/cora/useCora';
import { usePrefs } from '@/features/prefs/usePrefs';
import type { Speed } from '@/features/transfers/transfers.api';

export function SendScreen({ navigation }: any) {
  const { prefs } = usePrefs();
  const [speed, setSpeed] = useState<Speed>(prefs.defaultSendSpeed);
  const send = useSend();
  const toast = useToast();
  const { lineFor } = useCora();

  const onSend = () => {
    toast({ kind: 'pending', title: 'Sending $1,200.00', sub: 'Submitting to the Cipherbank rail…' });
    send.mutate(
      { recipient: 'maya', amount: '1200.00', source: 'USD', speed },
      {
        onSuccess: () =>
          toast({
            kind: 'ok',
            title: speed === 'instant' ? 'Sent · arrived instantly' : 'Send queued · ACH',
            sub: speed === 'instant' ? 'Maya received it · fee $0.00' : 'Arrives in 1–2 business days',
          }),
        onError: () => toast({ kind: 'error', title: 'Send failed', sub: 'Nothing moved. Try again.' }),
      },
    );
  };

  const Seg = ({ v, label }: { v: Speed; label: string }) => (
    <Pressable
      onPress={() => setSpeed(v)}
      style={{
        flex: 1,
        alignItems: 'center',
        paddingVertical: 11,
        borderRadius: 10,
        backgroundColor: speed === v ? color.gold : 'transparent',
      }}
    >
      <Text
        style={{
          fontWeight: speed === v ? '800' : '700',
          fontSize: 13,
          fontFamily: font.body,
          color: speed === v ? color.ink : color.textSubtle,
        }}
      >
        {label}
      </Text>
    </Pressable>
  );

  return (
    <View style={{ flex: 1, backgroundColor: color.canvas }}>
      <Header title="Send" onBack={() => navigation.goBack?.()} />
      <ScrollView contentContainerStyle={{ flexGrow: 1, padding: 18, paddingBottom: 110, gap: 12 }}>
        <CoraBar line={lineFor('send')} />

        <Card>
          <Text style={{ fontSize: 12, color: color.textSubtle, marginBottom: 10, fontFamily: font.body }}>To</Text>
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }}>
            <View
              style={{
                width: 42,
                height: 42,
                borderRadius: 21,
                backgroundColor: '#7B4DFF1a',
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Text style={{ color: '#5B34D6', fontWeight: '800', fontFamily: font.display }}>MC</Text>
            </View>
            <View style={{ flex: 1 }}>
              <Text style={{ fontWeight: '700', fontSize: 15, fontFamily: font.body }}>Maya Chen</Text>
              <Text style={{ fontFamily: font.mono, fontSize: 12, color: color.textSubtle }}>Chase ••4021 · ACH</Text>
            </View>
          </View>
        </Card>

        <Card dark style={{ alignItems: 'center' }}>
          <Text style={{ fontSize: 12, color: color.onDarkSubtle, marginBottom: 6, fontFamily: font.body }}>Amount</Text>
          <Text
            style={{
              fontFamily: font.display,
              fontWeight: '700',
              fontSize: 42,
              letterSpacing: -1.5,
              color: '#fff',
            }}
          >
            $1,200.00
          </Text>
          <View
            style={{
              marginTop: 10,
              backgroundColor: '#ffffff14',
              borderRadius: radius.pill,
              paddingHorizontal: 12,
              paddingVertical: 5,
            }}
          >
            <Text style={{ color: color.onDarkMuted, fontSize: 12, fontFamily: font.body }}>From USD balance</Text>
          </View>
        </Card>

        <View style={{ flexDirection: 'row', gap: 8, backgroundColor: color.track, borderRadius: radius.button, padding: 5 }}>
          <Seg v="instant" label="Instant" />
          <Seg v="ach" label="Standard ACH" />
        </View>

        <Card style={{ paddingVertical: 4 }}>
          {[
            ['Arrives', speed === 'instant' ? 'Instantly · Cipherbank rail' : '1–2 business days · ACH'],
            ['Fee', '$0.00'],
            ['Privacy', 'They see a handle, not you'],
          ].map(([k, v], i, a) => (
            <View
              key={k}
              style={{
                flexDirection: 'row',
                justifyContent: 'space-between',
                paddingVertical: 12,
                borderBottomWidth: i === a.length - 1 ? 0 : 1,
                borderBottomColor: color.hairline,
              }}
            >
              <Text style={{ color: color.textSubtle, fontSize: 14, fontFamily: font.body }}>{k}</Text>
              <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body }}>{v}</Text>
            </View>
          ))}
        </Card>

        <Button label="Send $1,200.00" busy={send.isPending} onPress={onSend} />
      </ScrollView>
      <CoraAssistant screen="send" />
    </View>
  );
}
