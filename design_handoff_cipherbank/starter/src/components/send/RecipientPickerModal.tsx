import React, { useMemo, useState } from 'react';
import {
  View,
  Text,
  Modal,
  Pressable,
  ScrollView,
  TextInput,
  KeyboardAvoidingView,
  Platform,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { color, radius, font, shadow } from '@/theme';
import { Button } from '@/components/primitives/Button';
import { Icon } from '@/components/primitives/Icon';
import {
  initialsFromName,
  recipientSubtitle,
  type AchAccountType,
  type AchRecipient,
  type AchRecipientInput,
} from '@/features/recipients/ach.types';

type Mode = 'list' | 'add';

export function RecipientPickerModal({
  visible,
  recipients,
  selectedId,
  onClose,
  onSelect,
  onSaveNew,
}: {
  visible: boolean;
  recipients: AchRecipient[];
  selectedId?: string;
  onClose: () => void;
  onSelect: (r: AchRecipient) => void;
  onSaveNew: (input: AchRecipientInput) => Promise<AchRecipient>;
}) {
  const insets = useSafeAreaInsets();
  const [mode, setMode] = useState<Mode>('list');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [displayName, setDisplayName] = useState('');
  const [holderName, setHolderName] = useState('');
  const [bankName, setBankName] = useState('');
  const [routing, setRouting] = useState('');
  const [account, setAccount] = useState('');
  const [accountType, setAccountType] = useState<AchAccountType>('checking');
  const [memo, setMemo] = useState('');

  const resetAdd = () => {
    setDisplayName('');
    setHolderName('');
    setBankName('');
    setRouting('');
    setAccount('');
    setAccountType('checking');
    setMemo('');
    setError(null);
  };

  const close = () => {
    setMode('list');
    resetAdd();
    onClose();
  };

  const canSave = useMemo(() => {
    const name = displayName.trim() || holderName.trim();
    return (
      name.length > 0 &&
      /^\d{9}$/.test(routing.replace(/\D/g, '')) &&
      account.replace(/\s/g, '').length >= 4
    );
  }, [displayName, holderName, routing, account]);

  const submitNew = async () => {
    if (!canSave || busy) return;
    setBusy(true);
    setError(null);
    try {
      const name = displayName.trim() || holderName.trim();
      const holder = holderName.trim() || name;
      const row = await onSaveNew({
        displayName: name,
        accountHolderName: holder,
        routingNumber: routing.replace(/\D/g, ''),
        accountNumber: account.replace(/\s/g, ''),
        accountType,
        bankName: bankName.trim() || undefined,
        rail: 'ACH',
        memo: memo.trim() || undefined,
        initials: initialsFromName(name),
      });
      resetAdd();
      setMode('list');
      onSelect(row);
      onClose();
    } catch {
      setError('Could not save this account');
    } finally {
      setBusy(false);
    }
  };

  const Field = ({
    label,
    value,
    onChangeText,
    placeholder,
    keyboardType,
    maxLength,
    secure,
  }: {
    label: string;
    value: string;
    onChangeText: (t: string) => void;
    placeholder?: string;
    keyboardType?: 'default' | 'number-pad';
    maxLength?: number;
    secure?: boolean;
  }) => (
    <View style={{ gap: 6 }}>
      <Text style={{ fontFamily: font.body, fontWeight: '700', fontSize: 12, color: color.textSubtle }}>{label}</Text>
      <TextInput
        value={value}
        onChangeText={onChangeText}
        placeholder={placeholder}
        placeholderTextColor={color.textSubtle}
        keyboardType={keyboardType}
        maxLength={maxLength}
        secureTextEntry={secure}
        autoCapitalize="words"
        style={{
          backgroundColor: color.track,
          borderRadius: radius.button,
          paddingHorizontal: 14,
          paddingVertical: 12,
          color: color.text,
          fontFamily: font.body,
          fontSize: 15,
        }}
      />
    </View>
  );

  return (
    <Modal visible={visible} animationType="slide" transparent onRequestClose={close}>
      <KeyboardAvoidingView
        style={{ flex: 1, justifyContent: 'flex-end', backgroundColor: '#00000066' }}
        behavior={Platform.OS === 'ios' ? 'padding' : undefined}
      >
        <Pressable style={{ flex: 1 }} onPress={close} />
        <View
          style={[
            {
              backgroundColor: color.surface,
              borderTopLeftRadius: radius.panel,
              borderTopRightRadius: radius.panel,
              maxHeight: '88%',
              paddingBottom: Math.max(insets.bottom, 16),
            },
            shadow.card,
          ]}
        >
          <View style={{ alignItems: 'center', paddingTop: 10, paddingBottom: 6 }}>
            <View style={{ width: 36, height: 4, borderRadius: 2, backgroundColor: color.track }} />
          </View>

          <View style={{ flexDirection: 'row', alignItems: 'center', paddingHorizontal: 18, paddingBottom: 12 }}>
            {mode === 'add' ? (
              <Pressable onPress={() => { setMode('list'); setError(null); }} hitSlop={10}>
                <Icon name="back" color={color.text} />
              </Pressable>
            ) : (
              <View style={{ width: 24 }} />
            )}
            <Text
              style={{
                flex: 1,
                textAlign: 'center',
                fontFamily: font.display,
                fontWeight: '700',
                fontSize: 18,
                color: color.text,
              }}
            >
              {mode === 'list' ? 'Choose contact' : 'Add ACH account'}
            </Text>
            <Pressable onPress={close} hitSlop={10}>
              <Icon name="close" color={color.textSubtle} />
            </Pressable>
          </View>

          {mode === 'list' ? (
            <ScrollView contentContainerStyle={{ paddingHorizontal: 18, paddingBottom: 20, gap: 8 }}>
              <Pressable
                onPress={() => { resetAdd(); setMode('add'); }}
                style={{
                  flexDirection: 'row',
                  alignItems: 'center',
                  gap: 12,
                  paddingVertical: 14,
                  paddingHorizontal: 12,
                  borderRadius: radius.card,
                  borderWidth: 1,
                  borderColor: color.goldDark,
                  borderStyle: 'dashed',
                }}
              >
                <View
                  style={{
                    width: 42,
                    height: 42,
                    borderRadius: 21,
                    backgroundColor: color.gold + '22',
                    alignItems: 'center',
                    justifyContent: 'center',
                  }}
                >
                  <Icon name="plus" color={color.goldDark} size={20} />
                </View>
                <View style={{ flex: 1 }}>
                  <Text style={{ fontWeight: '700', fontSize: 15, fontFamily: font.body, color: color.text }}>
                    Add new recipient
                  </Text>
                  <Text style={{ fontSize: 12, color: color.textSubtle, fontFamily: font.body }}>
                    Routing + account for ACH
                  </Text>
                </View>
              </Pressable>

              {recipients.map((r) => {
                const selected = r.id === selectedId;
                return (
                  <Pressable
                    key={r.id}
                    onPress={() => {
                      onSelect(r);
                      close();
                    }}
                    style={{
                      flexDirection: 'row',
                      alignItems: 'center',
                      gap: 12,
                      paddingVertical: 12,
                      paddingHorizontal: 12,
                      borderRadius: radius.card,
                      backgroundColor: selected ? color.gold + '18' : color.track,
                    }}
                  >
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
                      <Text style={{ color: '#5B34D6', fontWeight: '800', fontFamily: font.display }}>{r.initials}</Text>
                    </View>
                    <View style={{ flex: 1 }}>
                      <Text style={{ fontWeight: '700', fontSize: 15, fontFamily: font.body, color: color.text }}>
                        {r.displayName}
                      </Text>
                      <Text style={{ fontFamily: font.mono, fontSize: 12, color: color.textSubtle }}>
                        {recipientSubtitle(r)}
                      </Text>
                    </View>
                    {selected ? <Icon name="check" color={color.goldDark} size={18} /> : null}
                  </Pressable>
                );
              })}
            </ScrollView>
          ) : (
            <ScrollView
              keyboardShouldPersistTaps="handled"
              contentContainerStyle={{ paddingHorizontal: 18, paddingBottom: 24, gap: 12 }}
            >
              <Text style={{ fontSize: 13, color: color.textMuted, fontFamily: font.body, lineHeight: 18 }}>
                Stored on this device for ACH credits: legal name, ABA routing number, account number, and account type.
              </Text>
              <Field label="Display name" value={displayName} onChangeText={setDisplayName} placeholder="Maya Chen" />
              <Field
                label="Account holder name"
                value={holderName}
                onChangeText={setHolderName}
                placeholder="Same as on the bank account"
              />
              <Field label="Bank name" value={bankName} onChangeText={setBankName} placeholder="Chase" />
              <Field
                label="Routing number"
                value={routing}
                onChangeText={(t) => setRouting(t.replace(/\D/g, '').slice(0, 9))}
                placeholder="9 digits"
                keyboardType="number-pad"
                maxLength={9}
              />
              <Field
                label="Account number"
                value={account}
                onChangeText={(t) => setAccount(t.replace(/\s/g, ''))}
                placeholder="Checking or savings"
                keyboardType="number-pad"
                secure
              />
              <View style={{ gap: 6 }}>
                <Text style={{ fontFamily: font.body, fontWeight: '700', fontSize: 12, color: color.textSubtle }}>
                  Account type
                </Text>
                <View style={{ flexDirection: 'row', gap: 8, backgroundColor: color.track, borderRadius: radius.button, padding: 4 }}>
                  {(['checking', 'savings'] as AchAccountType[]).map((t) => (
                    <Pressable
                      key={t}
                      onPress={() => setAccountType(t)}
                      style={{
                        flex: 1,
                        alignItems: 'center',
                        paddingVertical: 10,
                        borderRadius: 10,
                        backgroundColor: accountType === t ? color.gold : 'transparent',
                      }}
                    >
                      <Text
                        style={{
                          fontWeight: '700',
                          fontSize: 13,
                          fontFamily: font.body,
                          color: accountType === t ? color.ink : color.textSubtle,
                          textTransform: 'capitalize',
                        }}
                      >
                        {t}
                      </Text>
                    </Pressable>
                  ))}
                </View>
              </View>
              <Field label="Memo (optional)" value={memo} onChangeText={setMemo} placeholder="Rent, invoice…" />
              {error ? (
                <Text style={{ color: color.red, fontFamily: font.body, fontSize: 13 }}>{error}</Text>
              ) : null}
              <Button label="Save contact" busy={busy} onPress={submitNew} disabled={!canSave} />
            </ScrollView>
          )}
        </View>
      </KeyboardAvoidingView>
    </Modal>
  );
}
