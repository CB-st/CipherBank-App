import React, { useEffect, useMemo, useState } from 'react';
import { View, Text, Pressable, ScrollView, Share, TextInput } from 'react-native';
import * as Clipboard from 'expo-clipboard';
import { color, radius, font, shadow } from '@/theme';
import { Header } from '@/components/chrome/Header';
import { CoraBar } from '@/components/cora/CoraBar';
import { CoraAssistant } from '@/components/cora/CoraAssistant';
import { AssetGlyph } from '@/components/money/AssetGlyph';
import { Icon } from '@/components/primitives/Icon';
import { PressableScale } from '@/components/primitives/PressableScale';
import { Button } from '@/components/primitives/Button';
import { QrCode } from '@/components/primitives/QrCode';
import { useReceive } from '@/features/receive/useReceive';
import { usePortfolio } from '@/features/portfolio/usePortfolio';
import { useLocalWallets } from '@/features/wallets/useLocalWallets';
import { usePrefs } from '@/features/prefs/usePrefs';
import { buildPaymentUri, shortenAddress } from '@/features/wallets/paymentUri';
import { canGenerateAddress } from '@/features/wallets/addressValidate';
import { useCora } from '@/features/cora/useCora';
import { useToast } from '@/components/primitives/Toast';
import { listAssets } from '@/features/assets/assetConfig';
import { useActivation } from '@/features/bootstrap';
import { useFocusEffect } from '@react-navigation/native';

export function ReceiveScreen({ navigation }: any) {
  const { prefs } = usePrefs();
  const enabled = prefs.enabledCurrencies;
  const catalog = listAssets().filter((a) => a.type !== 'security');
  const primary = catalog.filter((a) => ['BTC', 'ETH', 'USD'].includes(a.symbol));
  const more = catalog.filter((a) => !['BTC', 'ETH', 'USD'].includes(a.symbol));

  const [asset, setAsset] = useState(enabled.includes('BTC') ? 'BTC' : enabled[0] ?? 'BTC');
  const [walletId, setWalletId] = useState<string | null>(null);
  const [amount, setAmount] = useState('');
  const { data, isLoading } = useReceive(asset);
  const portfolio = usePortfolio();
  const { drafts, deriveNext, ensurePrimary } = useLocalWallets();
  const { setActivation } = useActivation();
  const { lineFor } = useCora();
  const toast = useToast();

  useFocusEffect(
    React.useCallback(() => {
      setActivation('convert'); // P1 interactive — receive needs address/QR
      return () => setActivation('shell');
    }, [setActivation]),
  );

  const wallets = useMemo(() => {
    const h = portfolio.data?.holdings.find((x) => x.symbol === asset);
    const fromPortfolio = h?.wallets ?? [];
    const fromDrafts = drafts.filter((d) => d.symbol === asset);
    // Prefer drafts with addresses merged via portfolio; surface draft-only too
    if (fromPortfolio.length) return fromPortfolio;
    return fromDrafts.map((d) => ({
      id: d.id,
      label: d.label,
      amount: '0',
      usdValue: 0,
      address: d.address,
      derivationPath: d.derivationPath,
      source: d.source,
    }));
  }, [portfolio.data, asset, drafts]);

  useEffect(() => {
    setWalletId(wallets[0]?.id ?? null);
  }, [asset, wallets]);

  const selected = wallets.find((w) => w.id === walletId) ?? wallets[0];
  const address = (selected?.address ?? data?.address ?? '').trim();
  const qrPayload = address
    ? buildPaymentUri(asset, address, amount.trim() ? { amount: amount.trim() } : {})
    : data?.uri ?? '';
  const display = address ? shortenAddress(address, 12, 8) : data?.handle ?? '—';
  const canDerive = canGenerateAddress(asset);

  const onCopy = async () => {
    const text = address || qrPayload || data?.handle || '';
    if (!text) {
      toast({ kind: 'error', title: 'Nothing to copy', sub: 'Generate or select a wallet first.' });
      return;
    }
    await Clipboard.setStringAsync(text);
    toast({ kind: 'ok', title: 'Copied', sub: shortenAddress(text, 14, 8) });
  };

  const onShare = async () => {
    try {
      await Share.share({ message: qrPayload || address || data?.uri || '' });
    } catch {
      toast({ kind: 'error', title: 'Share cancelled', sub: '' });
    }
  };

  const onCreateWallet = () => {
    if (!canDerive) {
      toast({
        kind: 'error',
        title: 'Cannot derive here',
        sub: asset === 'XMR' ? 'Use Home → Add wallet for Monero modes.' : 'Watch-only or fiat — paste on Home.',
      });
      return;
    }
    const hasLocal = drafts.some((d) => d.symbol === asset && d.source === 'local');
    const onOk = (w: { id: string; address?: string; derivationPath?: string }) => {
      setWalletId(w.id);
      toast({
        kind: 'ok',
        title: 'Address ready',
        sub: w.address ? shortenAddress(w.address) : w.derivationPath ?? 'Saved',
      });
    };
    const onErr = () =>
      toast({
        kind: 'error',
        title: 'Could not create wallet',
        sub: 'Unlock custody (PIN / biometrics) and try again.',
      });
    if (hasLocal) {
      deriveNext.mutate({ symbol: asset }, { onSuccess: onOk, onError: onErr });
    } else {
      ensurePrimary.mutate({ symbol: asset }, { onSuccess: onOk, onError: onErr });
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
            {isLoading && !address
              ? 'Loading…'
              : address
                ? 'Scan to send ' + asset + (selected ? ' · ' + selected.label : '')
                : 'No receive address yet'}
          </Text>

          {address ? (
            <QrCode value={qrPayload} size={172} />
          ) : (
            <View
              style={{
                width: 172,
                height: 172,
                borderRadius: 18,
                backgroundColor: '#fff',
                alignItems: 'center',
                justifyContent: 'center',
                padding: 16,
                gap: 10,
              }}
            >
              <Text style={{ textAlign: 'center', fontSize: 12, color: color.textMuted, fontFamily: font.body }}>
                {canDerive
                  ? 'Derive a new address from your on-device seed to show a QR.'
                  : 'Add a wallet on Home, or use your CipherBank handle for fiat.'}
              </Text>
              {canDerive ? (
                <Button
                  label="Create address"
                  busy={deriveNext.isPending || ensurePrimary.isPending}
                  onPress={onCreateWallet}
                />
              ) : null}
            </View>
          )}

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
            <Text style={{ flex: 1, fontFamily: font.mono, fontSize: 12, color: '#E9E4F2' }} numberOfLines={2}>
              {address || display}
            </Text>
            <Icon name="copy" size={16} color={color.gold} strokeWidth={2.2} />
          </Pressable>

          {selected?.derivationPath ? (
            <Text style={{ marginTop: 8, fontFamily: font.mono, fontSize: 10, color: color.onDarkMuted }}>
              {selected.derivationPath}
            </Text>
          ) : null}
        </View>

        {address && asset !== 'USD' && asset !== 'EUR' && asset !== 'JPY' ? (
          <View style={[{ backgroundColor: color.surface, borderRadius: radius.card, padding: 14, gap: 8 }, shadow.card]}>
            <Text style={{ fontWeight: '700', fontSize: 13, fontFamily: font.body, color: color.text }}>
              Request amount (optional)
            </Text>
            <TextInput
              value={amount}
              onChangeText={setAmount}
              placeholder={'Amount in ' + asset}
              placeholderTextColor={color.textSubtle}
              keyboardType="decimal-pad"
              style={{
                backgroundColor: color.track,
                borderRadius: radius.button,
                paddingHorizontal: 14,
                paddingVertical: 12,
                color: color.text,
                fontFamily: font.mono,
                fontSize: 14,
              }}
            />
            <Text style={{ fontSize: 11, color: color.textSubtle, fontFamily: font.body }}>
              Updates the QR to a payment URI. Leave blank for address-only.
            </Text>
          </View>
        ) : null}

        <View style={{ flexDirection: 'row', gap: 10 }}>
          {canDerive ? (
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
              onPress={onCreateWallet}
            >
              <Icon name="plus" size={16} color={color.text} />
              <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body, color: color.text }}>
                New address
              </Text>
            </Pressable>
          ) : (
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
              onPress={() => toast({ kind: 'pending', title: 'Request amount', sub: 'Set amount above for crypto QR.' })}
            >
              <Icon name="request" size={16} color={color.text} />
              <Text style={{ fontWeight: '700', fontSize: 14, fontFamily: font.body, color: color.text }}>Request</Text>
            </Pressable>
          )}
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
