import React from 'react';
import { View, Text } from 'react-native';
import { color, font } from '@/theme';
import { AssetGlyph } from './AssetGlyph';
import { Pill } from '../primitives/Pill';
import { PressableScale } from '../primitives/PressableScale';
import { Icon } from '../primitives/Icon';
import { assetSpec } from '@/features/assets/assetConfig';
import { formatAsset, formatUSD, signedPct, changeColor } from '@/lib/money';
import type { Holding, WalletAccount } from '@/features/portfolio/portfolio.types';

function shortAddr(a?: string) {
  if (!a) return 'pending address';
  if (a.length <= 14) return a;
  return a.slice(0, 6) + '…' + a.slice(-4);
}

function WalletLine({
  symbol,
  w,
  hidden,
  last,
}: {
  symbol: string;
  w: WalletAccount;
  hidden?: boolean;
  last?: boolean;
}) {
  return (
    <View
      style={{
        flexDirection: 'row',
        alignItems: 'center',
        gap: 10,
        paddingVertical: 8,
        paddingLeft: 48,
        borderBottomWidth: last ? 0 : 1,
        borderBottomColor: color.hairline,
      }}
    >
      <View style={{ flex: 1 }}>
        <View style={{ flexDirection: 'row', alignItems: 'center', gap: 6 }}>
          <Text style={{ fontWeight: '700', fontSize: 13, color: color.text, fontFamily: font.body }}>{w.label}</Text>
          <Pill
            label={w.source}
            bg={w.source === 'watch' ? '#7B4DFF22' : '#F2C14E22'}
            fg={w.source === 'watch' ? color.violet : color.goldDark}
          />
        </View>
        <Text style={{ fontFamily: font.mono, fontSize: 11, color: color.textSubtle, marginTop: 2 }}>
          {hidden ? '••••' : formatAsset(symbol, w.amount)}
          {!hidden ? ' · ' + shortAddr(w.address) : ''}
        </Text>
      </View>
      <Text style={{ fontWeight: '700', fontSize: 13, color: color.text }}>
        {hidden ? '••••' : formatUSD(w.usdValue)}
      </Text>
    </View>
  );
}

export function AssetRow({
  h,
  last,
  hidden,
  onAddWallet,
}: {
  h: Holding;
  last?: boolean;
  hidden?: boolean;
  onAddWallet?: () => void;
}) {
  const spec = assetSpec(h.symbol);
  const wallets = h.wallets ?? [];
  const showWallets = h.type === 'crypto' && wallets.length > 0;

  return (
    <View style={{ borderBottomWidth: last ? 0 : 1, borderBottomColor: color.hairline }}>
      <View style={{ flexDirection: 'row', alignItems: 'center', gap: 12, paddingVertical: 11 }}>
        <AssetGlyph symbol={h.symbol} />
        <View style={{ flex: 1 }}>
          <Text style={{ fontWeight: '700', fontSize: 14, color: color.text }}>{h.name}</Text>
          <Text style={{ fontFamily: font.mono, fontSize: 12, color: color.textSubtle }}>
            {hidden ? '••••' : formatAsset(h.symbol, h.amount)}
            {!hidden && spec.note ? ' · ' + spec.note : ''}
            {!hidden && showWallets ? ' · ' + wallets.length + ' wallets' : ''}
          </Text>
        </View>
        <View style={{ alignItems: 'flex-end', gap: 3 }}>
          <Text style={{ fontWeight: '700', fontSize: 14, color: color.text }}>
            {hidden ? '••••' : formatUSD(h.usdValue)}
          </Text>
          {hidden ? null : h.change24h !== 0 ? (
            <Text style={{ fontSize: 12, fontWeight: '600', color: changeColor(h.change24h) }}>
              {signedPct(h.change24h)}
            </Text>
          ) : spec.badge ? (
            <Pill label={spec.badge} bg="#F2C14E33" fg="#B8860B" />
          ) : null}
        </View>
      </View>

      {showWallets
        ? wallets.map((w, i) => (
            <WalletLine
              key={w.id}
              symbol={h.symbol}
              w={w}
              hidden={hidden}
              last={i === wallets.length - 1 && !onAddWallet}
            />
          ))
        : null}

      {onAddWallet ? (
        <PressableScale
          onPress={onAddWallet}
          style={{
            flexDirection: 'row',
            alignItems: 'center',
            gap: 8,
            paddingVertical: 10,
            paddingLeft: 48,
            marginBottom: last ? 4 : 0,
          }}
        >
          <Icon name="plus" size={14} color={color.violet} />
          <Text style={{ fontSize: 13, fontWeight: '700', color: color.violet, fontFamily: font.body }}>
            Add {h.symbol} wallet
          </Text>
        </PressableScale>
      ) : null}
    </View>
  );
}
