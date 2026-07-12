import React, { useMemo, useState } from 'react';
import { View, Text, TextInput, ScrollView } from 'react-native';
import { color, radius, shadow, font } from '@/theme';
import { Header } from '@/components/chrome/Header';
import { Button } from '@/components/primitives/Button';
import { useToast } from '@/components/primitives/Toast';
import { mnemonicWords } from '@/features/vault/bip39';
import { getPendingMnemonic } from '@/features/vault/custody';

function pickQuizIndices(n: number, count: number): number[] {
  const idxs = Array.from({ length: n }, (_, i) => i);
  for (let i = idxs.length - 1; i > 0; i--) {
    const j = Math.floor(Math.random() * (i + 1));
    [idxs[i], idxs[j]] = [idxs[j], idxs[i]];
  }
  return idxs.slice(0, count).sort((a, b) => a - b);
}

export function BackupQuizScreen({ navigation }: any) {
  const toast = useToast();
  const phrase = getPendingMnemonic() ?? '';
  const words = useMemo(() => mnemonicWords(phrase), [phrase]);
  const quiz = useMemo(() => pickQuizIndices(words.length || 12, 3), [words.length]);
  const [answers, setAnswers] = useState<Record<number, string>>({});

  const onContinue = () => {
    if (words.length !== 12) {
      toast({ kind: 'error', title: 'Missing phrase', sub: 'Go back and generate keys again.' });
      navigation.navigate('Keys');
      return;
    }
    const ok = quiz.every((i) => (answers[i] ?? '').trim().toLowerCase() === words[i]);
    if (!ok) {
      toast({ kind: 'error', title: 'Words do not match', sub: 'Check your written backup and try again.' });
      return;
    }
    navigation.navigate('SetPin');
  };

  return (
    <View style={{ flex: 1, backgroundColor: color.canvas }}>
      <Header title="Verify backup" onBack={() => navigation.goBack?.()} />
      <ScrollView contentContainerStyle={{ flexGrow: 1, padding: 22, paddingBottom: 40, gap: 16 }}>
        <View style={{ flexDirection: 'row', gap: 6 }}>
          {[0, 1, 2, 3].map((i) => (
            <View
              key={i}
              style={{ flex: 1, height: 4, borderRadius: 2, backgroundColor: i < 3 ? color.gold : '#E3E0DB' }}
            />
          ))}
        </View>

        <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 24, letterSpacing: -0.6, color: color.text }}>
          Confirm three words
        </Text>
        <Text style={{ fontSize: 15, color: color.textMuted, lineHeight: 22, fontFamily: font.body }}>
          Enter the words from your written recovery phrase at the positions below. This proves you saved a backup.
        </Text>

        <View style={[{ backgroundColor: color.surface, borderRadius: radius.card, padding: 16, gap: 14 }, shadow.card]}>
          {quiz.map((idx) => (
            <View key={idx} style={{ gap: 6 }}>
              <Text style={{ fontFamily: font.mono, fontSize: 12, color: color.textSubtle }}>
                Word {String(idx + 1).padStart(2, '0')}
              </Text>
              <TextInput
                value={answers[idx] ?? ''}
                onChangeText={(t) => setAnswers((prev) => ({ ...prev, [idx]: t }))}
                autoCapitalize="none"
                autoCorrect={false}
                placeholder="••••"
                placeholderTextColor={color.textSubtle}
                style={{
                  backgroundColor: color.track,
                  borderRadius: radius.button,
                  paddingHorizontal: 14,
                  paddingVertical: 12,
                  color: color.text,
                  fontFamily: font.mono,
                  fontSize: 15,
                }}
              />
            </View>
          ))}
        </View>

        <Button label="Continue to PIN" onPress={onContinue} />
      </ScrollView>
    </View>
  );
}
