import React, { useState } from 'react';
import { View, Text, ScrollView, TextInput, Pressable } from 'react-native';
import { color, radius, shadow, font } from '@/theme';
import { Header } from '@/components/chrome/Header';
import { CoraBar } from '@/components/cora/CoraBar';
import { CoraAssistant } from '@/components/cora/CoraAssistant';
import { AssetAmountCard } from '@/components/money/AssetAmountCard';
import { AssetSelector } from '@/components/money/AssetSelector';
import { RateLockStrip } from '@/components/money/RateLockStrip';
import { Card } from '@/components/primitives/Card';
import { Button } from '@/components/primitives/Button';
import { Icon } from '@/components/primitives/Icon';
import { useQuoteLock } from '@/features/quotes/useQuoteLock';
import { useConvert } from '@/features/convert/useConvert';
import { useToast } from '@/components/primitives/Toast';
import { useCora } from '@/features/cora/useCora';
import { formatUSD } from '@/lib/money';

export function ConvertScreen({ navigation }: any) {
  const [from, setFrom] = useState('BTC');
  const [to, setTo] = useState('USD');
  const [amount, setAmount] = useState('0.5');
  const [pick, setPick] = useState<null | 'from' | 'to'>(null);
  const { quote, secondsLeft, expired } = useQuoteLock(from, to, amount);
  const convert = useConvert();
  const toast = useToast();
  const { lineFor } = useCora();

  const out = quote?.amountOut
    ? Number(quote.amountOut)
    : quote
      ? Number(amount) * quote.rate
      : 0;

  const swap = () => {
    setFrom(to);
    setTo(from);
  };

  const onConvert = () => {
    if (!quote || expired) return;
    toast({
      kind: 'pending',
      title: 'Converting ' + amount + ' ' + from + ' → ' + to,
      sub: 'Locking settlement…',
    });
    convert.mutate(
      { quoteId: quote.quoteId, amount },
      {
        onSuccess: () =>
          toast({
            kind: 'ok',
            title: 'Converted · ' + formatUSD(out),
            sub: 'Settled · fee $0.00',
          }),
        onError: () =>
          toast({ kind: 'error', title: 'Convert failed', sub: 'Nothing moved. Try again.' }),
      },
    );
  };

  return (
    <View style={{ flex: 1, backgroundColor: color.canvas }}>
      <Header title="Convert" onBack={() => navigation.goBack?.()} />
      <ScrollView contentContainerStyle={{ flexGrow: 1, padding: 18, paddingBottom: 110, gap: 12 }} keyboardShouldPersistTaps="handled">
        <CoraBar line={lineFor('convert')} />

        <View style={{ gap: 10 }}>
          <View style={[{ borderRadius: radius.panel, padding: 16, backgroundColor: color.surface }, shadow.card]}>
            <Text style={{ fontSize: 12, color: color.textSubtle, marginBottom: 8, fontFamily: font.body }}>You pay</Text>
            <View style={{ flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between' }}>
              <TextInput
                value={amount}
                onChangeText={setAmount}
                keyboardType="decimal-pad"
                style={{
                  flex: 1,
                  fontFamily: font.display,
                  fontWeight: '700',
                  fontSize: 32,
                  letterSpacing: -1,
                  color: color.text,
                  padding: 0,
                }}
              />
              <Pressable
                onPress={() => setPick('from')}
                style={{
                  flexDirection: 'row',
                  alignItems: 'center',
                  gap: 7,
                  backgroundColor: color.canvas,
                  borderRadius: radius.pill,
                  paddingVertical: 6,
                  paddingHorizontal: 11,
                }}
              >
                <Text style={{ fontWeight: '700', fontSize: 14 }}>{from}</Text>
                <Icon name="caret-down" size={12} color={color.textSubtle} strokeWidth={3} />
              </Pressable>
            </View>
          </View>

          <View style={{ alignItems: 'center', marginVertical: -6, zIndex: 2 }}>
            <Pressable
              onPress={swap}
              style={[
                {
                  width: 44,
                  height: 44,
                  borderRadius: 22,
                  backgroundColor: color.gold,
                  borderWidth: 4,
                  borderColor: color.canvas,
                  alignItems: 'center',
                  justifyContent: 'center',
                },
                shadow.gold,
              ]}
            >
              <Icon name="convert" size={18} color={color.ink} strokeWidth={2.4} />
            </Pressable>
          </View>

          <AssetAmountCard
            label="You receive"
            amount={out ? out.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }) : '—'}
            symbol={to}
            dark
            sub="instant settlement"
            onPickAsset={() => setPick('to')}
          />
        </View>

        <RateLockStrip
          rateLabel={quote ? '1 ' + from + ' = ' + formatUSD(quote.rate) : 'Fetching rate…'}
          secondsLeft={secondsLeft}
          expired={expired}
        />

        <Card style={{ paddingVertical: 4 }}>
          {[
            ['Network fee', '$0.00 we cover it'],
            ['Privacy', from === 'XMR' || to === 'XMR' ? 'Shielded swap' : 'Private by default'],
            ['Settlement', 'Instant'],
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

        <Button
          label="Convert instantly"
          busy={convert.isPending}
          disabled={!quote || expired || !amount || Number(amount) <= 0}
          onPress={onConvert}
        />
        <Text style={{ textAlign: 'center', fontSize: 12, color: color.textSubtle, fontFamily: font.body }}>
          Funds land instantly — ready for ACH, payments, or withdrawal.
        </Text>
      </ScrollView>

      <AssetSelector visible={pick === 'from'} type="crypto" onClose={() => setPick(null)} onPick={setFrom} />
      <AssetSelector visible={pick === 'to'} type="fiat" onClose={() => setPick(null)} onPick={setTo} />
      <CoraAssistant screen="convert" />
    </View>
  );
}
