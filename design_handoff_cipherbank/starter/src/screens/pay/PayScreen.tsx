import React, { useMemo } from 'react';
import { View, Text, ScrollView } from 'react-native';
import { color, radius, font } from '@/theme';
import { Header } from '@/components/chrome/Header';
import { CoraBar } from '@/components/cora/CoraBar';
import { CoraAssistant } from '@/components/cora/CoraAssistant';
import { Card } from '@/components/primitives/Card';
import { Button } from '@/components/primitives/Button';
import { Pill } from '@/components/primitives/Pill';
import { Icon } from '@/components/primitives/Icon';
import { AssetGlyph } from '@/components/money/AssetGlyph';
import { FundingMixBar, MixSource } from '@/components/money/FundingMixBar';
import { usePayMix } from '@/features/transfers/usePayMix';
import { useToast } from '@/components/primitives/Toast';
import { useCora } from '@/features/cora/useCora';
import { formatUSD } from '@/lib/money';
import { assetSpec } from '@/features/assets/assetConfig';

const TOTAL = 2400;
const SOURCES: MixSource[] = [
  { asset: 'DOGE', value: 1000 },
  { asset: 'ETH', value: 800 },
  { asset: 'USD', value: 400 },
  { asset: 'AAPL', value: 200 },
];

export function PayScreen({ navigation }: any) {
  const pay = usePayMix();
  const toast = useToast();
  const { lineFor } = useCora();
  const covered = useMemo(() => SOURCES.reduce((s, x) => s + x.value, 0), []);
  const ok = covered >= TOTAL;

  const onPay = () => {
    if (!ok) return;
    toast({ kind: 'pending', title: 'Paying ' + formatUSD(TOTAL), sub: 'Mediating the exchange…' });
    pay.mutate(
      {
        recipient: 'sunset',
        total: String(TOTAL),
        sources: SOURCES.map((s) => ({ asset: s.asset, value: String(s.value) })),
      },
      {
        onSuccess: () => toast({ kind: 'ok', title: 'Paid ' + formatUSD(TOTAL), sub: 'Sunset received clean USD' }),
        onError: () => toast({ kind: 'error', title: 'Payment failed', sub: 'Nothing moved. Try again.' }),
      },
    );
  };

  return (
    <View style={{ flex: 1, backgroundColor: color.canvas }}>
      <Header title="Pay" onBack={() => navigation.goBack?.()} />
      <ScrollView contentContainerStyle={{ flexGrow: 1, padding: 18, paddingBottom: 110, gap: 12 }}>
        <Card>
          <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }}>
            <View
              style={{
                width: 44,
                height: 44,
                borderRadius: 12,
                backgroundColor: color.deepPurple,
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Text style={{ color: color.gold, fontWeight: '800', fontFamily: font.display }}>SP</Text>
            </View>
            <View style={{ flex: 1 }}>
              <Text style={{ fontWeight: '700', fontSize: 15, fontFamily: font.body }}>Sunset Property Mgmt</Text>
              <Text style={{ fontSize: 12, color: color.textSubtle, fontFamily: font.body }}>Rent · due Jul 1</Text>
            </View>
          </View>
          <Text
            style={{
              textAlign: 'center',
              marginTop: 16,
              fontFamily: font.display,
              fontWeight: '700',
              fontSize: 42,
              letterSpacing: -1.5,
            }}
          >
            {formatUSD(TOTAL)}
          </Text>
        </Card>

        <FundingMixBar sources={SOURCES} total={TOTAL} />
        <Text style={{ fontSize: 12, color: ok ? color.green : color.red, fontFamily: font.mono, textAlign: 'center' }}>
          {ok ? '100% covered' : formatUSD(covered) + ' of ' + formatUSD(TOTAL) + ' covered'}
        </Text>

        <Card style={{ paddingVertical: 4 }}>
          {SOURCES.map((s, i) => {
            const spec = assetSpec(s.asset);
            return (
              <View
                key={s.asset}
                style={{
                  flexDirection: 'row',
                  alignItems: 'center',
                  gap: 12,
                  paddingVertical: 12,
                  borderBottomWidth: i === SOURCES.length - 1 ? 0 : 1,
                  borderBottomColor: color.hairline,
                }}
              >
                <AssetGlyph symbol={s.asset} size={32} />
                <View style={{ flex: 1, gap: 4 }}>
                  <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                    <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body }}>{spec.name}</Text>
                    {spec.badge ? <Pill label={spec.badge} gold /> : null}
                  </View>
                  <Text style={{ fontFamily: font.mono, fontSize: 11, color: color.textSubtle }}>{s.asset}</Text>
                </View>
                <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body }}>{formatUSD(s.value)}</Text>
              </View>
            );
          })}
        </Card>

        <View
          style={{
            flexDirection: 'row',
            gap: 10,
            alignItems: 'center',
            backgroundColor: '#7B4DFF12',
            borderRadius: radius.button,
            padding: 12,
          }}
        >
          <Icon name="shield" size={18} color="#5B34D6" strokeWidth={2} />
          <Text style={{ flex: 1, fontSize: 12, color: '#4B3A6B', lineHeight: 17, fontFamily: font.body }}>
            We mediate the exchange in real time. Sunset receives clean USD — they never see the mix.
          </Text>
        </View>

        <CoraBar line={lineFor('pay')} />
        <Button label={'Pay ' + formatUSD(TOTAL)} busy={pay.isPending} disabled={!ok} onPress={onPay} />
      </ScrollView>
      <CoraAssistant screen="pay" />
    </View>
  );
}
