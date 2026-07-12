import React, { useState } from 'react';
import { View, Text, TextInput, Modal, Pressable } from 'react-native';
import { color, radius, shadow, font } from '@/theme';
import { AssetRow } from './AssetRow';
import { Skeleton } from '../primitives/Skeleton';
import { PressableScale } from '../primitives/PressableScale';
import { Button } from '../primitives/Button';
import { useLocalWallets } from '@/features/wallets/useLocalWallets';
import { useToast } from '../primitives/Toast';
import type { Holding } from '@/features/portfolio/portfolio.types';

export function AssetList({ holdings, hidden }: { holdings: Holding[]; hidden?: boolean }) {
  const { add } = useLocalWallets();
  const toast = useToast();
  const [target, setTarget] = useState<Holding | null>(null);
  const [label, setLabel] = useState('');
  const [address, setAddress] = useState('');

  const openAdd = (h: Holding) => {
    setTarget(h);
    setLabel('');
    setAddress('');
  };

  const submit = () => {
    if (!target) return;
    add.mutate(
      {
        symbol: target.symbol,
        label: label || `Wallet ${(target.wallets?.length ?? 0) + 1}`,
        address: address || undefined,
      },
      {
        onSuccess: () => {
          toast({
            kind: 'ok',
            title: 'Wallet added',
            sub: address ? 'Watch address saved locally' : 'Slot ready for local derivation',
          });
          setTarget(null);
        },
        onError: () => toast({ kind: 'error', title: 'Could not add wallet', sub: 'Try again.' }),
      },
    );
  };

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
              Creates a local slot for derivation or a watch-only address. Balances sync once chain read is wired.
            </Text>
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
            <TextInput
              value={address}
              onChangeText={setAddress}
              placeholder="Watch address (optional)"
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
            <Button label="Add wallet" busy={add.isPending} onPress={submit} />
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
