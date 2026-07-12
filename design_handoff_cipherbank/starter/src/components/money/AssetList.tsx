import React, { useState } from 'react';
import { View, Text, TextInput, Modal, Pressable } from 'react-native';
import { color, radius, shadow, font } from '@/theme';
import { AssetRow } from './AssetRow';
import { Skeleton } from '../primitives/Skeleton';
import { PressableScale } from '../primitives/PressableScale';
import { Button } from '../primitives/Button';
import { useLocalWallets } from '@/features/wallets/useLocalWallets';
import { isDerivableSymbol } from '@/features/wallets/derive';
import { useToast } from '../primitives/Toast';
import type { Holding } from '@/features/portfolio/portfolio.types';

export function AssetList({ holdings, hidden }: { holdings: Holding[]; hidden?: boolean }) {
  const { add, deriveNext } = useLocalWallets();
  const toast = useToast();
  const [target, setTarget] = useState<Holding | null>(null);
  const [label, setLabel] = useState('');
  const [address, setAddress] = useState('');
  const [mode, setMode] = useState<'derive' | 'watch'>('derive');

  const openAdd = (h: Holding) => {
    setTarget(h);
    setLabel('');
    setAddress('');
    setMode(isDerivableSymbol(h.symbol) ? 'derive' : 'watch');
  };

  const submitWatch = () => {
    if (!target) return;
    add.mutate(
      {
        symbol: target.symbol,
        label: label || `Wallet ${(target.wallets?.length ?? 0) + 1}`,
        address: address || undefined,
        source: address ? 'watch' : 'local',
      },
      {
        onSuccess: () => {
          toast({
            kind: 'ok',
            title: 'Wallet added',
            sub: address ? 'Watch address saved locally' : 'Slot saved locally',
          });
          setTarget(null);
        },
        onError: () => toast({ kind: 'error', title: 'Could not add wallet', sub: 'Try again.' }),
      },
    );
  };

  const submitDerive = () => {
    if (!target) return;
    deriveNext.mutate(
      { symbol: target.symbol, label: label || undefined },
      {
        onSuccess: (w) => {
          toast({
            kind: 'ok',
            title: 'Derived wallet',
            sub: w.address ? w.address.slice(0, 12) + '…' : w.derivationPath ?? 'Saved',
          });
          setTarget(null);
        },
        onError: () =>
          toast({
            kind: 'error',
            title: 'Derive failed',
            sub: 'Unlock custody (biometrics/PIN) and try again.',
          }),
      },
    );
  };

  const canDerive = target ? isDerivableSymbol(target.symbol) : false;

  return (
    <>
      <View style={[{ backgroundColor: color.surface, borderRadius: radius.card, paddingHorizontal: 14 }, shadow.card]}>
        {holdings.map((h, i) => (
          <AssetRow
            key={h.symbol}
            h={h}
            last={i === holdings.length - 1}
            hidden={hidden}
            onAddWallet={h.type === 'crypto' ? () => openAdd(h) : undefined}
          />
        ))}
      </View>

      <Modal visible={!!target} transparent animationType="fade" onRequestClose={() => setTarget(null)}>
        <Pressable
          style={{ flex: 1, backgroundColor: '#00000088', justifyContent: 'flex-end' }}
          onPress={() => setTarget(null)}
        >
          <Pressable
            onPress={(e) => e.stopPropagation()}
            style={{
              backgroundColor: color.surfaceRaised,
              borderTopLeftRadius: radius.panel,
              borderTopRightRadius: radius.panel,
              padding: 20,
              gap: 12,
              paddingBottom: 32,
            }}
          >
            <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 20, color: color.text }}>
              Add {target?.symbol} wallet
            </Text>
            <Text style={{ fontSize: 13, color: color.textMuted, fontFamily: font.body }}>
              {canDerive
                ? 'Derive the next account from your on-device seed, or paste a watch-only address.'
                : 'Paste a watch-only address. Full derivation for this asset arrives in a later sprint.'}
            </Text>

            {canDerive ? (
              <View style={{ flexDirection: 'row', gap: 8 }}>
                <PressableScale
                  onPress={() => setMode('derive')}
                  style={{
                    flex: 1,
                    paddingVertical: 10,
                    borderRadius: radius.button,
                    backgroundColor: mode === 'derive' ? color.deepPurple : color.track,
                    alignItems: 'center',
                  }}
                >
                  <Text
                    style={{
                      fontFamily: font.body,
                      fontWeight: '700',
                      fontSize: 13,
                      color: mode === 'derive' ? color.gold : color.textMuted,
                    }}
                  >
                    Derive next
                  </Text>
                </PressableScale>
                <PressableScale
                  onPress={() => setMode('watch')}
                  style={{
                    flex: 1,
                    paddingVertical: 10,
                    borderRadius: radius.button,
                    backgroundColor: mode === 'watch' ? color.deepPurple : color.track,
                    alignItems: 'center',
                  }}
                >
                  <Text
                    style={{
                      fontFamily: font.body,
                      fontWeight: '700',
                      fontSize: 13,
                      color: mode === 'watch' ? color.gold : color.textMuted,
                    }}
                  >
                    Watch address
                  </Text>
                </PressableScale>
              </View>
            ) : null}

            <TextInput
              value={label}
              onChangeText={setLabel}
              placeholder="Label (e.g. Cold, Trading)"
              placeholderTextColor={color.textSubtle}
              style={{
                backgroundColor: color.track,
                borderRadius: radius.button,
                paddingHorizontal: 14,
                paddingVertical: 12,
                color: color.text,
                fontFamily: font.body,
                fontSize: 14,
              }}
            />
            {mode === 'watch' || !canDerive ? (
              <TextInput
                value={address}
                onChangeText={setAddress}
                placeholder="Watch address"
                placeholderTextColor={color.textSubtle}
                autoCapitalize="none"
                autoCorrect={false}
                style={{
                  backgroundColor: color.track,
                  borderRadius: radius.button,
                  paddingHorizontal: 14,
                  paddingVertical: 12,
                  color: color.text,
                  fontFamily: font.mono,
                  fontSize: 13,
                }}
              />
            ) : null}
            <Button
              label={mode === 'derive' && canDerive ? 'Derive next account' : 'Add wallet'}
              busy={add.isPending || deriveNext.isPending}
              onPress={mode === 'derive' && canDerive ? submitDerive : submitWatch}
            />
            <PressableScale onPress={() => setTarget(null)} style={{ alignItems: 'center', paddingVertical: 8 }}>
              <Text style={{ color: color.textSubtle, fontWeight: '600', fontFamily: font.body }}>Cancel</Text>
            </PressableScale>
          </Pressable>
        </Pressable>
      </Modal>
    </>
  );
}

AssetList.Skeleton = function ({ rows = 4 }: { rows?: number }) {
  return (
    <View style={[{ backgroundColor: color.surface, borderRadius: radius.card, paddingHorizontal: 14 }, shadow.card]}>
      {Array.from({ length: rows }).map((_, i) => (
        <View
          key={i}
          style={{
            flexDirection: 'row',
            alignItems: 'center',
            gap: 12,
            paddingVertical: 12,
            borderBottomWidth: i === rows - 1 ? 0 : 1,
            borderBottomColor: color.hairline,
          }}
        >
          <Skeleton style={{ width: 36, height: 36, borderRadius: 10 } as any} />
          <View style={{ flex: 1, gap: 7 }}>
            <Skeleton style={{ width: 90, height: 12 } as any} />
            <Skeleton style={{ width: 60, height: 10 } as any} />
          </View>
          <Skeleton style={{ width: 66, height: 14 } as any} />
        </View>
      ))}
    </View>
  );
};
