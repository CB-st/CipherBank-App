import React, { useEffect, useState } from 'react';
import { View, Text, ScrollView, Switch } from 'react-native';
import { color, radius, font } from '@/theme';
import { Header } from '@/components/chrome/Header';
import { Card } from '@/components/primitives/Card';
import { Button } from '@/components/primitives/Button';
import { PressableScale } from '@/components/primitives/PressableScale';
import { FadeIn } from '@/components/primitives/FadeIn';
import { Icon } from '@/components/primitives/Icon';
import { usePrefs } from '@/features/prefs/usePrefs';
import { HOME_SECTION_LABELS, BASE_CURRENCY_OPTIONS, type HomeSection } from '@/features/prefs/prefs.types';
import { baseCurrencyLabel } from '@/features/prefs/localeCurrency';
import { listAssets } from '@/features/assets/assetConfig';
import { useVault } from '@/features/vault/useVault';
import { exportMnemonic } from '@/features/vault/custody';
import { useSession } from '@/features/session/useSession';
import { useToast } from '@/components/primitives/Toast';
import { useCora } from '@/features/cora/useCora';
import { CoraAssistant } from '@/components/cora/CoraAssistant';
import { Pill } from '@/components/primitives/Pill';
import {
  getActivePosCardId,
  isHardwareTestCard,
  setActivePosCardId,
  type HardwareCard,
} from '@/features/pos/hardwareCards';

function RowSwitch({
  label,
  sub,
  value,
  onChange,
}: {
  label: string;
  sub?: string;
  value: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <View
      style={{
        flexDirection: 'row',
        alignItems: 'center',
        gap: 12,
        paddingVertical: 12,
        borderBottomWidth: 1,
        borderBottomColor: color.hairline,
      }}
    >
      <View style={{ flex: 1 }}>
        <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body, color: color.text }}>{label}</Text>
        {sub ? (
          <Text style={{ fontSize: 12, color: color.textSubtle, marginTop: 2, fontFamily: font.body }}>{sub}</Text>
        ) : null}
      </View>
      <Switch
        value={value}
        onValueChange={onChange}
        trackColor={{ false: color.track, true: '#F2C14E88' }}
        thumbColor={value ? color.gold : color.surfaceRaised}
      />
    </View>
  );
}

export function ProfileScreen({ navigation }: any) {
  const {
    prefs,
    toggleSection,
    moveSection,
    setValuesHiddenOnLaunch,
    setDefaultSendSpeed,
    setAppearance,
    setBaseCurrency,
    toggleEnabledCurrency,
    setPref,
  } = usePrefs();
  const { setEnabled: setCoraEnabled } = useCora();
  const { lock } = useSession();
  const vault = useVault();
  const toast = useToast();
  const [posCardId, setPosCardId] = useState<string | null>(null);

  useEffect(() => {
    getActivePosCardId().then(setPosCardId);
  }, []);

  const revealPhrase = async () => {
    // exportMnemonic always force-prompts OS biometrics / PIN before returning the phrase.
    const phrase = await exportMnemonic();
    if (!phrase) {
      toast({ kind: 'error', title: 'Unlock cancelled', sub: 'Biometrics or PIN required.' });
      return;
    }
    toast({ kind: 'ok', title: 'Phrase unlocked locally', sub: 'Never shared with CipherBank servers.' });
  };

  const addDemoCard = () => {
    vault.addCard.mutate(
      { brand: 'Visa', last4: '1234', expMonth: 9, expYear: 2029 },
      {
        onSuccess: () =>
          toast({ kind: 'ok', title: 'Card token saved', sub: 'Processor token only — no PAN stored.' }),
        onError: () => toast({ kind: 'error', title: 'Could not save card', sub: 'Try again.' }),
      },
    );
  };

  return (
    <View style={{ flex: 1, backgroundColor: color.canvas }}>
      <Header title="Profile" />
      <ScrollView contentContainerStyle={{ padding: 18, paddingBottom: 120, gap: 14 }} showsVerticalScrollIndicator={false}>
        <FadeIn>
          <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 22, letterSpacing: -0.5, color: color.text }}>
            Preferences
          </Text>
          <Text style={{ fontSize: 13, color: color.textMuted, marginTop: 4, fontFamily: font.body, marginBottom: 8 }}>
            Customize what Home shows and how privacy defaults work on this device.
          </Text>
        </FadeIn>

        <FadeIn delay={20}>
          <Card style={{ paddingVertical: 4 }}>
            <Text style={{ fontWeight: '800', fontSize: 13, fontFamily: font.body, paddingTop: 8, color: color.text }}>
              Appearance
            </Text>
            <Text style={{ fontSize: 12, color: color.textSubtle, fontFamily: font.body, marginBottom: 10 }}>
              Dark is the default CipherBank chrome. Switch to light if you prefer a brighter canvas.
            </Text>
            <View
              style={{
                flexDirection: 'row',
                gap: 8,
                backgroundColor: color.track,
                borderRadius: radius.button,
                padding: 5,
                marginBottom: 8,
              }}
            >
              {(['dark', 'light'] as const).map((v) => (
                <PressableScale
                  key={v}
                  onPress={() => setAppearance(v)}
                  style={{
                    flex: 1,
                    alignItems: 'center',
                    paddingVertical: 11,
                    borderRadius: 10,
                    backgroundColor: prefs.appearance === v ? color.gold : 'transparent',
                  }}
                >
                  <Text
                    style={{
                      fontWeight: '800',
                      fontSize: 13,
                      fontFamily: font.body,
                      color: prefs.appearance === v ? color.ink : color.textSubtle,
                    }}
                  >
                    {v === 'dark' ? 'Dark' : 'Light'}
                  </Text>
                </PressableScale>
              ))}
            </View>
          </Card>
        </FadeIn>

        <FadeIn delay={30}>
          <Card style={{ paddingVertical: 4 }}>
            <Text style={{ fontWeight: '800', fontSize: 13, fontFamily: font.body, paddingTop: 8, color: color.text }}>
              Money display
            </Text>
            <Text style={{ fontSize: 12, color: color.textSubtle, fontFamily: font.body, marginBottom: 10 }}>
              Portfolio total and hero chart use this unit.{' '}
              {prefs.localeInferredBase && prefs.localeInferredBase === prefs.baseCurrency
                ? 'Defaulted from your device locale.'
                : ''}
            </Text>
            <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8, marginBottom: 8 }}>
              {BASE_CURRENCY_OPTIONS.map((v) => (
                <PressableScale
                  key={v}
                  onPress={() => setBaseCurrency(v)}
                  style={{
                    flexGrow: 1,
                    minWidth: '45%',
                    alignItems: 'center',
                    paddingVertical: 11,
                    borderRadius: 10,
                    backgroundColor: prefs.baseCurrency === v ? color.gold : color.track,
                  }}
                >
                  <Text
                    style={{
                      fontWeight: '800',
                      fontSize: 13,
                      fontFamily: font.body,
                      color: prefs.baseCurrency === v ? color.ink : color.textSubtle,
                    }}
                  >
                    {baseCurrencyLabel(v)}
                  </Text>
                </PressableScale>
              ))}
            </View>
          </Card>
        </FadeIn>

        <FadeIn delay={35}>
          <Card style={{ paddingVertical: 4 }}>
            <Text style={{ fontWeight: '800', fontSize: 13, fontFamily: font.body, paddingTop: 8, color: color.text }}>
              Currencies on Home
            </Text>
            <Text style={{ fontSize: 12, color: color.textSubtle, fontFamily: font.body, marginBottom: 6 }}>
              Turn off to hide an asset from the main list. Hidden wallets stay on-device under Other assets.
            </Text>
            {listAssets({ enabledOnly: true })
              .filter((a) => a.type !== 'security')
              .map((a, i, arr) => {
                const on = prefs.enabledCurrencies.includes(a.symbol);
                return (
                  <RowSwitch
                    key={a.symbol}
                    label={a.name + ' (' + a.symbol + ')'}
                    sub={a.note}
                    value={on}
                    onChange={() => toggleEnabledCurrency(a.symbol)}
                  />
                );
              })}
          </Card>
        </FadeIn>

        <FadeIn delay={40}>
          <Card style={{ paddingVertical: 4 }}>
            <Text style={{ fontWeight: '800', fontSize: 13, fontFamily: font.body, marginBottom: 4, paddingTop: 8, color: color.text }}>
              Home layout
            </Text>
            {prefs.homeOrder.map((section: HomeSection, i) => (
              <View
                key={section}
                style={{
                  flexDirection: 'row',
                  alignItems: 'center',
                  gap: 8,
                  paddingVertical: 10,
                  borderBottomWidth: i === prefs.homeOrder.length - 1 ? 0 : 1,
                  borderBottomColor: color.hairline,
                }}
              >
                <View style={{ flex: 1 }}>
                  <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body, color: color.text }}>
                    {HOME_SECTION_LABELS[section]}
                  </Text>
                </View>
                <PressableScale onPress={() => moveSection(section, -1)} hitSlop={8} style={{ padding: 6 }}>
                  <Icon name="back" size={16} color={i === 0 ? color.hairline : color.deepPurple} />
                </PressableScale>
                <PressableScale
                  onPress={() => moveSection(section, 1)}
                  hitSlop={8}
                  style={{ padding: 6, transform: [{ rotate: '180deg' }] }}
                >
                  <Icon
                    name="back"
                    size={16}
                    color={i === prefs.homeOrder.length - 1 ? color.hairline : color.deepPurple}
                  />
                </PressableScale>
                <Switch
                  value={prefs.homeVisible[section]}
                  onValueChange={() => toggleSection(section)}
                  trackColor={{ false: color.track, true: '#F2C14E88' }}
                  thumbColor={prefs.homeVisible[section] ? color.gold : color.surfaceRaised}
                />
              </View>
            ))}
          </Card>
        </FadeIn>

        <FadeIn delay={80}>
          <Card style={{ paddingVertical: 4 }}>
            <Text style={{ fontWeight: '800', fontSize: 13, fontFamily: font.body, paddingTop: 8 }}>Privacy & lock</Text>
            <RowSwitch
              label="Hide balances on launch"
              sub="Start with values masked until you reveal them"
              value={prefs.valuesHiddenOnLaunch}
              onChange={setValuesHiddenOnLaunch}
            />
            <RowSwitch
              label="Cora assistant"
              sub="Show Cora lines and floating assistant"
              value={prefs.coraEnabled}
              onChange={setCoraEnabled}
            />
            <View style={{ paddingVertical: 12, borderBottomWidth: 1, borderBottomColor: color.hairline }}>
              <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body, marginBottom: 8, color: color.text }}>
                Auto-lock after idle
              </Text>
              <Text style={{ fontSize: 12, color: color.textSubtle, fontFamily: font.body, marginBottom: 10 }}>
                Locks the whole app (and clears the unlock session). Background always locks immediately.
              </Text>
              <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
                {([30, 60, 120, 300] as const).map((sec) => (
                  <PressableScale
                    key={sec}
                    onPress={() => setPref('appLockIdleSec', sec)}
                    style={{
                      paddingVertical: 10,
                      paddingHorizontal: 14,
                      borderRadius: 10,
                      backgroundColor: prefs.appLockIdleSec === sec ? color.gold : color.track,
                    }}
                  >
                    <Text
                      style={{
                        fontWeight: '800',
                        fontSize: 12,
                        fontFamily: font.body,
                        color: prefs.appLockIdleSec === sec ? color.ink : color.textSubtle,
                      }}
                    >
                      {sec < 60 ? sec + 's' : sec / 60 + 'm'}
                    </Text>
                  </PressableScale>
                ))}
              </View>
            </View>
            <View style={{ paddingVertical: 12 }}>
              <Button label="Lock now" variant="ghost" onPress={() => lock()} />
            </View>
            <View style={{ paddingVertical: 12 }}>
              <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body, marginBottom: 8 }}>
                Default send speed
              </Text>
              <View
                style={{
                  flexDirection: 'row',
                  gap: 8,
                  backgroundColor: color.track,
                  borderRadius: radius.button,
                  padding: 5,
                }}
              >
                {(['instant', 'ach'] as const).map((v) => (
                  <PressableScale
                    key={v}
                    onPress={() => setDefaultSendSpeed(v)}
                    style={{
                      flex: 1,
                      alignItems: 'center',
                      paddingVertical: 11,
                      borderRadius: 10,
                      backgroundColor: prefs.defaultSendSpeed === v ? color.gold : 'transparent',
                    }}
                  >
                    <Text
                      style={{
                        fontWeight: '800',
                        fontSize: 13,
                        fontFamily: font.body,
                        color: prefs.defaultSendSpeed === v ? color.ink : color.textSubtle,
                      }}
                    >
                      {v === 'instant' ? 'Instant' : 'Standard ACH'}
                    </Text>
                  </PressableScale>
                ))}
              </View>
            </View>
          </Card>
        </FadeIn>

        <FadeIn delay={100}>
          <Button
            label="Tap to pay lab"
            onPress={() => {
              const parent = navigation.getParent();
              if (parent) parent.navigate('PosLab');
              else navigation.navigate('PosLab');
            }}
          />
          <Text style={{ fontSize: 12, color: color.textSubtle, fontFamily: font.body, marginTop: 6 }}>
            Mock POS + NFC presentment. See docs/TESTING.md and mocks/POS_API.md.
          </Text>
        </FadeIn>

        <FadeIn delay={120}>
          <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 22, letterSpacing: -0.5, marginTop: 8 }}>
            Wallets and cards
          </Text>
          <Text style={{ fontSize: 12, color: color.textMuted, marginTop: 4, marginBottom: 8, fontFamily: font.body }}>
            Recovery phrase never leaves this device. Server binaries and card processor tokens are hybrid vault
            material.
          </Text>

          <Card>
            <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12 }}>
              <View
                style={{
                  width: 40,
                  height: 40,
                  borderRadius: 12,
                  backgroundColor: color.deepPurple,
                  alignItems: 'center',
                  justifyContent: 'center',
                }}
              >
                <Icon name="shield-check" size={20} color={color.gold} />
              </View>
              <View style={{ flex: 1 }}>
                <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body }}>Local custody</Text>
                <Text style={{ fontSize: 12, color: color.textSubtle, fontFamily: font.mono }}>
                  {vault.hasLocal ? 'Keys present on this device' : 'No local keys yet'}
                </Text>
              </View>
            </View>
            <View style={{ marginTop: 12, gap: 8 }}>
              {!vault.hasLocal ? (
                <Button
                  label="Create local keys"
                  onPress={() => navigation.navigate('Keys')}
                />
              ) : (
                <Button
                  label="Reveal recovery phrase"
                  variant="ghost"
                  onPress={revealPhrase}
                  style={{ borderColor: color.violet }}
                />
              )}
            </View>
          </Card>

          <Card style={{ marginTop: 10, paddingVertical: 4 }}>
            <Text style={{ fontWeight: '800', fontSize: 13, fontFamily: font.body, paddingTop: 8, paddingBottom: 4 }}>
              Server wallet binaries
            </Text>
            {vault.binaries.map((b, i) => (
              <View
                key={b.id}
                style={{
                  paddingVertical: 12,
                  borderBottomWidth: i === vault.binaries.length - 1 ? 0 : 1,
                  borderBottomColor: color.hairline,
                }}
              >
                <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body }}>{b.label}</Text>
                <Text style={{ fontFamily: font.mono, fontSize: 11, color: color.textSubtle, marginTop: 2 }}>
                  {b.id} · {b.status}
                </Text>
              </View>
            ))}
          </Card>

          <Card style={{ marginTop: 10, paddingVertical: 4 }}>
            <Text style={{ fontWeight: '800', fontSize: 13, fontFamily: font.body, paddingTop: 8, paddingBottom: 4 }}>
              Card payment tokens
            </Text>
            {vault.cards.map((c, i) => {
              const card = c as HardwareCard;
              return (
              <View
                key={c.id}
                style={{
                  flexDirection: 'row',
                  alignItems: 'center',
                  paddingVertical: 12,
                  borderBottomWidth: i === vault.cards.length - 1 ? 0 : 1,
                  borderBottomColor: color.hairline,
                }}
              >
                <View style={{ flex: 1, gap: 4 }}>
                  <View style={{ flexDirection: 'row', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
                    <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body }}>
                      {card.label ?? c.brand + ' ·••• ' + c.last4}
                    </Text>
                    {isHardwareTestCard(card) ? <Pill label="HW TEST" gold /> : null}
                    {posCardId === c.id ? <Pill label="POS LAB" bg="#3FA46A22" fg={color.green} /> : null}
                  </View>
                  <Text style={{ fontFamily: font.mono, fontSize: 11, color: color.textSubtle }}>
                    exp {c.expMonth}/{c.expYear}
                  </Text>
                  {isHardwareTestCard(card) ? (
                    <PressableScale
                      onPress={async () => {
                        await setActivePosCardId(c.id);
                        setPosCardId(c.id);
                        toast({ kind: 'ok', title: 'POS lab card', sub: 'Will be used for tap-to-pay lab' });
                      }}
                      style={{ marginTop: 4 }}
                    >
                      <Text style={{ color: color.violet, fontWeight: '700', fontSize: 12 }}>Use for POS lab</Text>
                    </PressableScale>
                  ) : null}
                </View>
                <PressableScale
                  onPress={() =>
                    vault.removeCard.mutate(c.id, {
                      onSuccess: () => toast({ kind: 'ok', title: 'Card removed', sub: 'Token deleted from vault.' }),
                    })
                  }
                  hitSlop={8}
                >
                  <Icon name="close" size={16} color={color.red} />
                </PressableScale>
              </View>
            );
            })}
            <View style={{ paddingVertical: 10 }}>
              <Button label="Add card token" busy={vault.addCard.isPending} onPress={addDemoCard} />
            </View>
          </Card>
        </FadeIn>
      </ScrollView>
      <CoraAssistant screen="profile" />
    </View>
  );
}
