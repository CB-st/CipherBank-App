import React, { useState } from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import { color, radius, font } from '@/theme';
import { Button } from '@/components/primitives/Button';
import { PressableScale } from '@/components/primitives/PressableScale';
import { RecipientPickerModal } from '@/components/send/RecipientPickerModal';
import { useCora } from '@/features/cora/useCora';
import { useSetupState } from '@/features/account/useSetupState';
import { pullAccountBootstrap } from '@/features/account/bootstrapAccount';
import { useAchRecipients } from '@/features/recipients/useAchRecipients';
import { useToast } from '@/components/primitives/Toast';

/** Quiet Home prompt until first-run setup is marked complete. */
export function HomeSetupPrompt({ onNavigateSend }: { onNavigateSend?: () => void }) {
  const { lineFor } = useCora();
  const { needsSetup, completeSetup, refresh, path } = useSetupState();
  const { recipients, save, refresh: refreshRecipients } = useAchRecipients();
  const toast = useToast();
  const [pickerOpen, setPickerOpen] = useState(false);
  const [busy, setBusy] = useState(false);

  if (!needsSetup) return null;

  const onPull = async () => {
    setBusy(true);
    try {
      const boot = await pullAccountBootstrap();
      await refreshRecipients();
      await refresh();
      const n = boot.recipients?.length ?? 0;
      toast({
        kind: 'ok',
        title: n ? `Pulled ${n} contacts` : 'Nothing new from CipherBank',
        sub: lineFor('bootstrapPull'),
      });
    } catch {
      toast({ kind: 'error', title: 'Pull failed', sub: 'Try again when you are online.' });
    } finally {
      setBusy(false);
    }
  };

  const onSkip = async () => {
    await completeSetup();
    toast({ kind: 'ok', title: 'Setup saved for later', sub: lineFor('setupDone') });
  };

  return (
    <View
      style={{
        padding: 16,
        borderRadius: radius.panel,
        backgroundColor: color.deepPurple,
        gap: 12,
      }}
    >
      <Text style={{ fontFamily: font.mono, fontSize: 10, letterSpacing: 1, color: color.gold }}>CORA · SETUP</Text>
      <Text style={{ color: '#E9E4F2', fontFamily: font.body, fontSize: 14, lineHeight: 20 }}>
        {path === 'returning' ? lineFor('bootstrapPull') : lineFor('homeSetup')}
      </Text>
      {busy ? <ActivityIndicator color={color.gold} /> : null}
      <View style={{ gap: 8 }}>
        <Button
          label="Pull from CipherBank"
          busy={busy}
          onPress={onPull}
        />
        <Button
          label="Add ACH contact"
          variant="ghost"
          disabled={busy}
          onPress={() => setPickerOpen(true)}
          style={{ borderColor: '#ffffff2e' }}
        />
        <PressableScale onPress={onSkip} disabled={busy}>
          <Text
            style={{
              textAlign: 'center',
              color: color.onDarkSubtle,
              fontFamily: font.body,
              fontSize: 13,
              paddingVertical: 8,
            }}
          >
            I will do this later
          </Text>
        </PressableScale>
      </View>

      <RecipientPickerModal
        visible={pickerOpen}
        recipients={recipients}
        onClose={() => setPickerOpen(false)}
        onSelect={async () => {
          setPickerOpen(false);
          await completeSetup();
          await refresh();
          toast({ kind: 'ok', title: 'Contact ready', sub: lineFor('setupAch') });
          onNavigateSend?.();
        }}
        onSaveNew={async (input) => {
          const row = await save(input);
          await completeSetup();
          await refresh();
          return row;
        }}
      />
    </View>
  );
}
