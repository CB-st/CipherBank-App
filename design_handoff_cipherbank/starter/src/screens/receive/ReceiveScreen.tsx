import React, { useEffect, useMemo, useState } from 'react';
import { View, Text, Pressable, ScrollView, Share } from 'react-native';
import Svg, { Rect } from 'react-native-svg';
import { color, radius, font, shadow } from '@/theme';
import { Header } from '@/components/chrome/Header';
import { CoraBar } from '@/components/cora/CoraBar';
import { CoraAssistant } from '@/components/cora/CoraAssistant';
import { AssetGlyph } from '@/components/money/AssetGlyph';
import { Icon } from '@/components/primitives/Icon';
import { PressableScale } from '@/components/primitives/PressableScale';
import { useReceive } from '@/features/receive/useReceive';
import { usePortfolio } from '@/features/portfolio/usePortfolio';
import { useCora } from '@/features/cora/useCora';
import { useToast } from '@/components/primitives/Toast';
import { listAssets } from '@/features/assets/assetConfig';

export function ReceiveScreen({ navigation }: any) {
  const [asset, setAsset] = useState('BTC');
  const [walletId, setWalletId] = useState<string | null>(null);
  const { data, isLoading } = useReceive(asset);
  const portfolio = usePortfolio();
  const { lineFor } = useCora();
  const toast = useToast();
  const primary = listAssets().filter((a) => ['BTC', 'ETH', 'USD'].includes(a.symbol));
  const more = listAssets().filter((a) => !['BTC', 'ETH', 'USD'].includes(a.symbol) && a.type !== 'security');

  const wallets = useMemo(() => {
    const h = portfolio.data?.holdings.find((x) => x.symbol === asset);
    return h?.wallets ?? [];
  }, [portfolio.data, asset]);

  useEffect(() => {
    setWalletId(wallets[0]?.id ?? null);
  }, [asset, wallets]);

  const selected = wallets.find((w) => w.id === walletId) ?? wallets[0];
  const handle = data?.handle ?? 'cora@cipherbank.id';
  const address = selected?.address ?? data?.address ?? '';
  const display =
    selected?.label && address
      ? selected.label + ' · ' + address.slice(0, 8) + '…'
      : asset === 'BTC' && address
        ? 'bc1q · ' + handle
        : handle;

  const onCopy = () => {
    toast({ kind: 'ok', title: 'Copied', sub: address || display });
  };

  const onShare = async () => {
    try {
      await Share.share({ message: address || data?.uri || handle });
    } catch {
      toast({ kind: 'error', title: 'Share cancelled', sub: '' });
    }
  };

  return (
    <View style={{ flex: 1, backgroundColor: color.canvas }}>
      <Header title="Receive" onBack={() => navigation.goBack?.()} />
      <ScrollView contentContainerStyle={{ flexGrow: 1, padding: 18, paddingBottom: 110, gap: 12 }}>
        <View style={[{ flexDirection: 'row', gap: 6, backgroundColor: color.surface, borderRadius: radius.card, padding: 6 }, shadow.card]}>
          {primary.map((a) => (
            <Pressable
              key={a.symbol}
              onPress={() => setAsset(a.symbol)}
              style={{
                flex: 1,
                alignItems: 'center',
                gap: 5,
                paddingVertical: 11,
                borderRadius: 13,
                backgroundColor: asset === a.symbol ? a.tint : 'transparent',
              }}
            >
              <AssetGlyph symbol={a.symbol} size={30} />
              <Text
                style={{
                  fontFamily: font.mono,
                  fontSize: 11,
                  fontWeight: asset === a.symbol ? '800' : '600',
                  color: asset === a.symbol ? color.text : color.textSubtle,
                }}
              >
                {a.symbol}
              </Text>
            </Pressable>
          ))}
          <Pressable
            onPress={() => setAsset(more[0]?.symbol ?? 'DOGE')}
            style={{
              flex: 1,
              alignItems: 'center',
              gap: 5,
              paddingVertical: 11,
              borderRadius: 13,
              backgroundColor: !primary.find((p) => p.symbol === asset) ? color.track : 'transparent',
            }}
          >
            <View
              style={{
                width: 30,
                height: 30,
                borderRadius: 8,
                backgroundColor: color.hairline,
                alignItems: 'center',
                justifyContent: 'center',
              }}
            >
              <Icon name="plus" size={14} color={color.textSubtle} />
            </View>
            <Text style={{ fontFamily: font.mono, fontSize: 11, fontWeight: '600', color: color.textSubtle }}>More</Text>
          </Pressable>
        </View>

        {!primary.find((p) => p.symbol === asset) ? (
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
            {more.map((a) => (
              <Pressable
                key={a.symbol}
                onPress={() => setAsset(a.symbol)}
                style={{
                  flexDirection: 'row',
                  alignItems: 'center',
                  gap: 8,
                  paddingVertical: 8,
                  paddingHorizontal: 12,
                  borderRadius: radius.pill,
                  backgroundColor: asset === a.symbol ? a.tint : color.surface,
                }}
              >
                <AssetGlyph symbol={a.symbol} size={24} />
                <Text style={{ fontFamily: font.mono, fontSize: 12, fontWeight: '700', color: color.text }}>{a.symbol}</Text>
              </Pressable>
            ))}
          </View>
        ) : null}

        {wallets.length > 1 ? (
          <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>
            {wallets.map((w) => (
              <PressableScale
                key={w.id}
                onPress={() => setWalletId(w.id)}
                style={{
                  paddingVertical: 8,
                  paddingHorizontal: 12,
                  borderRadius: radius.pill,
                  backgroundColor: walletId === w.id ? color.gold : color.surface,
                }}
              >
                <Text
                  style={{
                    fontFamily: font.body,
                    fontWeight: '700',
                    fontSize: 12,
                    color: walletId === w.id ? color.ink : color.text,
                  }}
                >
                  {w.label}
                </Text>
              </PressableScale>
            ))}
          </View>
        ) : null}

        <View style={{ backgroundColor: color.deepPurple, borderRadius: radius.panel, padding: 22, alignItems: 'center' }}>
          <Text style={{ color: color.onDarkSubtle, fontSize: 13, marginBottom: 14, fontFamily: font.body }}>
            {isLoading ? 'Loading…' : 'Scan to send ' + asset + (selected ? ' · ' + selected.label : '')}
          </Text>
          <View
            style={{
              backgroundColor: '#fff',
              width: 172,
              height: 172,
              borderRadius: 18,
              alignItems: 'center',
              justifyContent: 'center',
            }}
          >
            <Svg width={132} height={132} viewBox="0 0 132 132">
              <Rect width={132} height={132} fill="#fff" />
              <Rect x={8} y={8} width={36} height={36} fill="none" stroke={color.ink} strokeWidth={8} />
              <Rect x={88} y={8} width={36} height={36} fill="none" stroke={color.ink} strokeWidth={8} />
              <Rect x={8} y={88} width={36} height={36} fill="none" stroke={color.ink} strokeWidth={8} />
              <Rect x={56} y={56} width={20} height={20} rx={4} fill="none" stroke={color.violet} strokeWidth={4} />
            </Svg>
          </View>
          <Pressable
            onPress={onCopy}
            style={{
              marginTop: 14,
              backgroundColor: '#ffffff10',
              borderRadius: radius.button,
              paddingHorizontal: 14,
              paddingVertical: 11,
              flexDirection: 'row',
              alignItems: 'center',
              gap: 10,
              alignSelf: 'stretch',
            }}
          >
            <Text style={{ flex: 1, fontFamily: font.mono, fontSize: 12, color: '#E9E4F2' }} numberOfLines={1}>
              {display}
            </Text>
            <Icon name="copy" size={16} color={color.gold} strokeWidth={2.2} />
          </Pressable>
        </View>

        <View style={{ flexDirection: 'row', gap: 10 }}>
          <Pressable
            style={{
              flex: 1,
              backgroundColor: color.surface,
              borderRadius: radius.button,
              paddingVertical: 13,
              alignItems: 'center',
              flexDirection: 'row',
              justifyContent: 'center',
              gap: 8,
            }}
            onPress={() => toast({ kind: 'pending', title: 'Request amount', sub: 'Coming in a follow-up polish pass' })}
          >
            <Icon name="request" size={16} color={color.text} />
            <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body, color: color.text }}>Request amount</Text>
          </Pressable>
          <Pressable
            onPress={onShare}
            style={{
              flex: 1,
              backgroundColor: color.gold,
              borderRadius: radius.button,
              paddingVertical: 13,
              alignItems: 'center',
              flexDirection: 'row',
              justifyContent: 'center',
              gap: 8,
            }}
          >
            <Icon name="share" size={16} color={color.ink} />
            <Text style={{ fontWeight: '800', fontSize: 14, fontFamily: font.body }}>Share</Text>
          </Pressable>
        </View>

        <CoraBar line={lineFor('receive')} />
      </ScrollView>
      <CoraAssistant screen="receive" />
    </View>
  );
}
