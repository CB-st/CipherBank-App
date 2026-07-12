import React, { useEffect, useMemo, useState } from 'react';
import { View, Text, ScrollView, TextInput } from 'react-native';
import { color, radius, font, shadow } from '@/theme';
import { Header } from '@/components/chrome/Header';
import { Card } from '@/components/primitives/Card';
import { Button } from '@/components/primitives/Button';
import { PressableScale } from '@/components/primitives/PressableScale';
import { FadeIn } from '@/components/primitives/FadeIn';
import { Pill } from '@/components/primitives/Pill';
import { usePosPay } from '@/features/pos/usePosPay';
import {
  getActivePosCardId,
  isHardwareTestCard,
  pickLabCard,
  setActivePosCardId,
  type HardwareCard,
} from '@/features/pos/hardwareCards';
import { useVault } from '@/features/vault/useVault';
import { useToast } from '@/components/primitives/Toast';
import { formatUSD } from '@/lib/money';
import type { ExchangeStage } from '@/features/pos/nfcExchange';

function stageColor(s: ExchangeStage['status']) {
  if (s === 'ok') return color.green;
  if (s === 'active') return color.gold;
  if (s === 'fail') return color.red;
  return color.textSubtle;
}

function ExchangeTimeline({ stages }: { stages: ExchangeStage[] }) {
  if (!stages.length) return null;
  return (
    <View style={{ alignSelf: 'stretch', gap: 8, marginTop: 4 }}>
      <Text style={{ color: color.gold, fontFamily: font.mono, fontSize: 11, letterSpacing: 1 }}>
        NFC EXCHANGE (LAB)
      </Text>
      {stages.map((st) => (
        <View
          key={st.id}
          style={{
            flexDirection: 'row',
            gap: 10,
            alignItems: 'flex-start',
            opacity: st.status === 'pending' ? 0.45 : 1,
          }}
        >
          <View
            style={{
              width: 8,
              height: 8,
              borderRadius: 4,
              marginTop: 5,
              backgroundColor: stageColor(st.status),
            }}
          />
          <View style={{ flex: 1 }}>
            <Text style={{ color: color.onDark, fontWeight: '700', fontSize: 13, fontFamily: font.body }}>
              {st.label}
            </Text>
            <Text style={{ color: color.onDarkSubtle, fontSize: 11, fontFamily: font.body, marginTop: 2 }}>
              {st.detail}
            </Text>
            <Text style={{ color: color.onDarkMuted, fontSize: 10, fontFamily: font.mono, marginTop: 2 }}>
              {st.apduHint}
            </Text>
          </View>
        </View>
      ))}
    </View>
  );
}

export function MockPosScreen({ navigation }: any) {
  const vault = useVault();
  const toast = useToast();
  const pos = usePosPay();
  const [amount, setAmount] = useState('24.00');
  const [label, setLabel] = useState('Lab purchase');
  const [asset, setAsset] = useState('USD');
  const [activeCardId, setActiveCardId] = useState<string | null>(null);
  const [nfcHint, setNfcHint] = useState<string>('');
  const [ttlLeft, setTtlLeft] = useState(0);

  const cards = vault.cards as HardwareCard[];
  const labCard = useMemo(() => pickLabCard(cards, activeCardId), [cards, activeCardId]);

  useEffect(() => {
    getActivePosCardId().then(setActiveCardId);
    pos.checkNfc().then((s) => {
      setNfcHint(
        s.supported && s.enabled
          ? 'NFC ready on this device'
          : s.reason ?? 'NFC unavailable — use Simulate exchange',
      );
    });
  }, []);

  useEffect(() => {
    if (!pos.auth || (pos.phase !== 'presenting' && pos.phase !== 'exchanging')) {
      setTtlLeft(0);
      return;
    }
    const ends = Date.now() + pos.auth.presentment.ttlMs;
    const tick = () => setTtlLeft(Math.max(0, Math.round((ends - Date.now()) / 1000)));
    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id);
  }, [pos.auth, pos.phase]);

  const selectCard = async (id: string) => {
    await setActivePosCardId(id);
    setActiveCardId(id);
    toast({ kind: 'ok', title: 'POS lab card set', sub: id });
  };

  const onAuthorize = async () => {
    if (!labCard) {
      toast({ kind: 'error', title: 'No card', sub: 'Add or select a vault card first.' });
      return;
    }
    const result = await pos.startAndAuthorize({
      amount,
      label,
      sources: [{ asset, value: amount }],
      card: labCard,
    });
    if (result) {
      toast({
        kind: 'ok',
        title: 'Authorized',
        sub: result.presentment.brand + ' ·••• ' + result.presentment.last4,
      });
    }
  };

  const onMockTap = async () => {
    await pos.presentMock();
    toast({ kind: 'ok', title: 'Exchange complete', sub: 'EMV-shaped lab stages + tokenRef settled' });
  };

  const onNfc = async () => {
    const res = await pos.presentNfc();
    if (res && 'ok' in res && res.ok) {
      toast({
        kind: 'ok',
        title: res.mode === 'nfc' ? 'NFC presentment' : 'Simulated NFC',
        sub: res.mode === 'nfc' ? 'Hold near reader completed' : 'Emulator/lab path — staged exchange',
      });
    } else {
      toast({
        kind: 'error',
        title: 'NFC',
        sub: (res && 'detail' in res && res.detail) || pos.error || 'Failed',
      });
    }
  };

  return (
    <View style={{ flex: 1, backgroundColor: color.canvas }}>
      <Header title="Tap to pay lab" onBack={() => navigation.goBack?.()} />
      <ScrollView contentContainerStyle={{ padding: 18, paddingBottom: 48, gap: 14 }} keyboardShouldPersistTaps="handled">
        <FadeIn>
          <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 22, color: color.text }}>
            Mock POS terminal
          </Text>
          <Text style={{ fontSize: 13, color: color.textMuted, marginTop: 6, fontFamily: font.body, lineHeight: 19 }}>
            Authorize crypto against a vault card, then run a Visa/MC-shaped contactless exchange. Payload is
            sessionId + tokenRef only — never a PAN. See docs/DIGITAL_CARDS_NFC.md.
          </Text>
        </FadeIn>

        <FadeIn delay={40}>
          <Card>
            <Text style={{ fontWeight: '800', fontSize: 13, fontFamily: font.body, marginBottom: 10, color: color.text }}>
              Merchant request
            </Text>
            <Text style={{ fontSize: 12, color: color.textSubtle, marginBottom: 4 }}>Amount (USD)</Text>
            <TextInput
              value={amount}
              onChangeText={setAmount}
              keyboardType="decimal-pad"
              style={{
                fontFamily: font.display,
                fontSize: 28,
                fontWeight: '700',
                color: color.text,
                marginBottom: 12,
                padding: 0,
              }}
            />
            <Text style={{ fontSize: 12, color: color.textSubtle, marginBottom: 4 }}>Label</Text>
            <TextInput
              value={label}
              onChangeText={setLabel}
              style={{
                fontFamily: font.body,
                fontSize: 15,
                color: color.text,
                backgroundColor: color.track,
                borderRadius: radius.chip,
                paddingHorizontal: 12,
                paddingVertical: 10,
                marginBottom: 12,
              }}
            />
            <Text style={{ fontSize: 12, color: color.textSubtle, marginBottom: 8 }}>Fund from</Text>
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
              {['USD', 'BTC', 'ETH'].map((a) => (
                <PressableScale
                  key={a}
                  onPress={() => setAsset(a)}
                  style={{
                    paddingHorizontal: 14,
                    paddingVertical: 8,
                    borderRadius: radius.pill,
                    backgroundColor: asset === a ? color.gold : color.track,
                  }}
                >
                  <Text style={{ fontWeight: '800', fontFamily: font.mono, fontSize: 12, color: color.text }}>{a}</Text>
                </PressableScale>
              ))}
            </View>
          </Card>
        </FadeIn>

        <FadeIn delay={80}>
          <Card style={{ paddingVertical: 4 }}>
            <Text
              style={{
                fontWeight: '800',
                fontSize: 13,
                fontFamily: font.body,
                paddingTop: 8,
                marginBottom: 4,
                color: color.text,
              }}
            >
              Presentment card
            </Text>
            {cards.map((c, i) => (
              <PressableScale
                key={c.id}
                onPress={() => selectCard(c.id)}
                style={{
                  paddingVertical: 12,
                  borderBottomWidth: i === cards.length - 1 ? 0 : 1,
                  borderBottomColor: color.hairline,
                  opacity: labCard?.id === c.id ? 1 : 0.75,
                }}
              >
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8 }}>
                  <Text style={{ flex: 1, fontWeight: '700', fontFamily: font.body, color: color.text }}>
                    {c.label ?? c.brand + ' ·••• ' + c.last4}
                  </Text>
                  {isHardwareTestCard(c) ? <Pill label="HW TEST" gold /> : null}
                  {labCard?.id === c.id ? <Pill label="ACTIVE" bg="#3FA46A22" fg={color.green} /> : null}
                </View>
              </PressableScale>
            ))}
          </Card>
        </FadeIn>

        <FadeIn delay={100}>
          <Text style={{ fontSize: 12, color: color.textSubtle, fontFamily: font.mono }}>{nfcHint}</Text>
          {pos.error ? (
            <Text style={{ color: color.red, fontSize: 13, fontFamily: font.body }}>{pos.error}</Text>
          ) : null}
          <Button
            label={
              pos.phase === 'unlocking'
                ? 'Unlocking…'
                : pos.phase === 'authorizing'
                  ? 'Authorizing…'
                  : 'Authorize ' + formatUSD(Number(amount) || 0)
            }
            busy={pos.phase === 'unlocking' || pos.phase === 'authorizing'}
            onPress={onAuthorize}
          />
        </FadeIn>

        {(pos.phase === 'presenting' || pos.phase === 'exchanging' || pos.phase === 'done') && pos.auth ? (
          <FadeIn>
            <View
              style={[
                {
                  backgroundColor: color.deepPurple,
                  borderRadius: radius.panel,
                  padding: 20,
                  alignItems: 'center',
                  gap: 10,
                },
                shadow.card,
              ]}
            >
              <Text style={{ color: color.gold, fontFamily: font.mono, fontSize: 11, letterSpacing: 1 }}>
                PRESENT CARD
              </Text>
              <Text style={{ color: color.onDark, fontFamily: font.display, fontSize: 28, fontWeight: '700' }}>
                {pos.auth.presentment.brand} ·••• {pos.auth.presentment.last4}
              </Text>
              <Text style={{ color: color.onDarkMuted, fontFamily: font.mono, fontSize: 12 }}>
                TTL {ttlLeft}s · {pos.auth.presentment.tokenRef.slice(0, 16)}…
              </Text>

              <ExchangeTimeline stages={pos.exchangeStages} />

              <View style={{ flexDirection: 'row', gap: 10, marginTop: 8, alignSelf: 'stretch' }}>
                <View style={{ flex: 1 }}>
                  <Button
                    label={pos.phase === 'exchanging' ? 'Exchanging…' : 'Simulate exchange'}
                    busy={pos.phase === 'exchanging'}
                    onPress={onMockTap}
                  />
                </View>
                <View style={{ flex: 1 }}>
                  <Button
                    label="Start NFC"
                    variant="ghost"
                    onPress={onNfc}
                    style={{ borderColor: color.gold }}
                    busy={pos.phase === 'exchanging'}
                  />
                </View>
              </View>
              {pos.lastTap ? (
                <Text style={{ color: color.onDarkSubtle, fontSize: 11, fontFamily: font.mono, marginTop: 6 }}>
                  Last tap · {pos.lastTap.sessionId.slice(0, 12)}… · tokenRef ok
                </Text>
              ) : null}
              <PressableScale onPress={pos.reset} style={{ marginTop: 8 }}>
                <Text style={{ color: color.gold, fontWeight: '700' }}>Reset lab session</Text>
              </PressableScale>
            </View>
          </FadeIn>
        ) : null}
      </ScrollView>
    </View>
  );
}
