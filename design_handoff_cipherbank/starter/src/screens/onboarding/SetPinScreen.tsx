import React, { useState } from 'react';
import { View, Text, TextInput, ScrollView } from 'react-native';
import { color, radius, shadow, font } from '@/theme';
import { Header } from '@/components/chrome/Header';
import { Button } from '@/components/primitives/Button';
import { useToast } from '@/components/primitives/Toast';
import { useSession } from '@/features/session/useSession';
import { sealPendingCustody } from '@/features/vault/custody';
import { ensureDerivedWallets } from '@/features/wallets/localWallets';
import { getSetupPath } from '@/features/account/setupState';
import { pullAccountBootstrap } from '@/features/account/bootstrapAccount';
import { beginSetupPath } from '@/features/account/setupState';

export function SetPinScreen({ navigation }: any) {
  const toast = useToast();
  const { finishCustodySetup } = useSession();
  const [pin, setPin] = useState('');
  const [confirm, setConfirm] = useState('');
  const [busy, setBusy] = useState(false);

  const onSave = async () => {
    if (!/^\d{6}$/.test(pin)) {
      toast({ kind: 'error', title: 'Use a 6-digit PIN', sub: '' });
      return;
    }
    if (pin !== confirm) {
      toast({ kind: 'error', title: 'PINs do not match', sub: 'Re-enter confirmation.' });
      return;
    }
    setBusy(true);
    try {
      await sealPendingCustody(pin);
    } catch {
      toast({ kind: 'error', title: 'Could not seal keys', sub: 'Try again from Secure keys.' });
      navigation.navigate('Keys');
      setBusy(false);
      return;
    }

    // Derivation / SQLite / cloud bootstrap must not undo a sealed vault.
    try {
      await ensureDerivedWallets();
    } catch {
      /* Home / unlock can derive later */
    }

    try {
      let path = await getSetupPath();
      if (!path) {
        await beginSetupPath('new');
        path = 'new';
      }

      if (path === 'returning') {
        toast({
          kind: 'pending',
          title: 'Pulling CipherBank account…',
          sub: 'Contacts and preferences — never your seed.',
        });
        try {
          const boot = await pullAccountBootstrap();
          const n = boot.recipients?.length ?? 0;
          toast({
            kind: 'ok',
            title: n ? `Restored ${n} contacts` : 'Vault secured',
            sub: n ? 'Account metadata synced to this device.' : 'No cloud contacts yet — add them from Home.',
          });
        } catch {
          toast({
            kind: 'ok',
            title: 'Vault secured',
            sub: 'Could not reach CipherBank yet — pull contacts from Home when online.',
          });
        }
      } else {
        toast({ kind: 'ok', title: 'Vault secured', sub: 'PIN + on-device encryption enabled.' });
      }
    } catch {
      toast({ kind: 'ok', title: 'Vault secured', sub: 'PIN + on-device encryption enabled.' });
    }

    try {
      await finishCustodySetup();
    } catch {
      toast({ kind: 'error', title: 'Could not open app', sub: 'Restart and unlock with your PIN.' });
    } finally {
      setBusy(false);
    }
  };

  return (
    <View style={{ flex: 1, backgroundColor: color.canvas }} testID="set-pin-screen">
      <Header title="Set PIN" onBack={() => navigation.goBack?.()} />
      <ScrollView contentContainerStyle={{ flexGrow: 1, padding: 22, paddingBottom: 40, gap: 16 }}>
        <View style={{ flexDirection: 'row', gap: 6 }}>
          {[0, 1, 2, 3].map((i) => (
            <View key={i} style={{ flex: 1, height: 4, borderRadius: 2, backgroundColor: color.gold }} />
          ))}
        </View>

        <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 24, letterSpacing: -0.6, color: color.text }}>
          Protect this device
        </Text>
        <Text style={{ fontSize: 15, color: color.textMuted, lineHeight: 22, fontFamily: font.body }}>
          Your recovery phrase is encrypted on-device. Use this PIN when biometrics are unavailable. CipherBank never
          receives your PIN or phrase.
        </Text>

        <View style={[{ backgroundColor: color.surface, borderRadius: radius.card, padding: 16, gap: 14 }, shadow.card]}>
          <View style={{ gap: 6 }}>
            <Text style={{ fontFamily: font.body, fontWeight: '700', fontSize: 13, color: color.text }}>PIN</Text>
            <TextInput
              testID="pin-input"
              value={pin}
              onChangeText={(t) => setPin(t.replace(/\D/g, '').slice(0, 6))}
              keyboardType="number-pad"
              secureTextEntry
              maxLength={6}
              placeholder="6 digits"
              placeholderTextColor={color.textSubtle}
              style={{
                backgroundColor: color.track,
                borderRadius: radius.button,
                paddingHorizontal: 14,
                paddingVertical: 12,
                color: color.text,
                fontFamily: font.mono,
                fontSize: 18,
                letterSpacing: 8,
              }}
            />
          </View>
          <View style={{ gap: 6 }}>
            <Text style={{ fontFamily: font.body, fontWeight: '700', fontSize: 13, color: color.text }}>Confirm PIN</Text>
            <TextInput
              testID="pin-confirm"
              value={confirm}
              onChangeText={(t) => setConfirm(t.replace(/\D/g, '').slice(0, 6))}
              keyboardType="number-pad"
              secureTextEntry
              maxLength={6}
              placeholder="6 digits"
              placeholderTextColor={color.textSubtle}
              style={{
                backgroundColor: color.track,
                borderRadius: radius.button,
                paddingHorizontal: 14,
                paddingVertical: 12,
                color: color.text,
                fontFamily: font.mono,
                fontSize: 18,
                letterSpacing: 8,
              }}
            />
          </View>
        </View>

        <Button testID="pin-finish" label="Finish setup" busy={busy} onPress={onSave} />
      </ScrollView>
    </View>
  );
}
