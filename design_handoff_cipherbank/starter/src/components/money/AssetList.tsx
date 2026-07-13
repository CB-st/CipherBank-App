import React, { useState } from 'react';
import { View, Text, TextInput, Modal, Pressable, ScrollView } from 'react-native';
import { color, radius, shadow, font } from '@/theme';
import { AssetRow } from './AssetRow';
import { Skeleton } from '../primitives/Skeleton';
import { PressableScale } from '../primitives/PressableScale';
import { Button } from '../primitives/Button';
import { useLocalWallets } from '@/features/wallets/useLocalWallets';
import { useCreateServerWallet } from '@/features/wallets/useXmrWallets';
import { fingerprintViewKey } from '@/features/wallets/xmr.api';
import { getWalletModule, type WalletUiMode } from '@/features/wallets/registry';
import { validateWatchAddress } from '@/features/wallets/addressValidate';
import { shortenAddress } from '@/features/wallets/paymentUri';
import { QrCode } from '../primitives/QrCode';
import { buildPaymentUri } from '@/features/wallets/paymentUri';
import { useToast } from '../primitives/Toast';
import type { Holding, LocalWalletDraft } from '@/features/portfolio/portfolio.types';

type AddMode = WalletUiMode;

export function AssetList({
  holdings,
  hiddenHoldings = [],
  hidden,
}: {
  holdings: Holding[];
  hiddenHoldings?: Holding[];
  hidden?: boolean;
}) {
  const { add, deriveNext } = useLocalWallets();
  const createServer = useCreateServerWallet();
  const toast = useToast();
  const [target, setTarget] = useState<Holding | null>(null);
  const [label, setLabel] = useState('');
  const [address, setAddress] = useState('');
  const [viewKey, setViewKey] = useState('');
  const [mode, setMode] = useState<AddMode>('watch');
  const [otherOpen, setOtherOpen] = useState(false);
  const [created, setCreated] = useState<LocalWalletDraft | null>(null);

  const mod = target ? getWalletModule(target.symbol) : null;
  const canDerive = mod?.canDerive ?? false;

  const openAdd = (h: Holding) => {
    const m = getWalletModule(h.symbol);
    setTarget(h);
    setCreated(null);
    setLabel('');
    setAddress('');
    setViewKey('');
    setMode(m.addModes[0] ?? 'watch');
  };

  const close = () => {
    setTarget(null);
    setCreated(null);
  };

  const submitWatch = () => {
    if (!target) return;
    if (address.trim()) {
      const check = validateWatchAddress(target.symbol, address);
      if (!check.ok) {
        toast({ kind: 'error', title: 'Invalid address', sub: check.reason ?? '' });
        return;
      }
    }
    add.mutate(
      {
        symbol: target.symbol,
        label: label || `Wallet ${(target.wallets?.length ?? 0) + 1}`,
        address: address || undefined,
        source: address ? 'watch' : 'local',
        mode: 'watch',
      },
      {
        onSuccess: (w) => {
          toast({
            kind: 'ok',
            title: 'Wallet added',
            sub: address ? 'Watch address saved locally' : 'Slot saved locally',
          });
          if (w.address) setCreated(w);
          else close();
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
            title: 'Address generated',
            sub: w.address ? shortenAddress(w.address) : w.derivationPath ?? 'Saved',
          });
          setCreated(w);
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

  const submitServerWallet = () => {
    if (!target || !mod?.usesServerWallets) return;
    const walletMode = mode as 'managed' | 'unmanaged' | 'watch';
    if (walletMode === 'unmanaged' && (!address.trim() || !viewKey.trim())) {
      toast({ kind: 'error', title: 'Need address + view key', sub: 'Spend key stays on this device.' });
      return;
    }
    if (walletMode === 'watch' && !address.trim()) {
      toast({ kind: 'error', title: 'Address required', sub: '' });
      return;
    }
    createServer.mutate(
      {
        symbol: target.symbol,
        label: label || (walletMode === 'managed' ? 'CipherBank managed' : walletMode === 'unmanaged' ? 'Unmanaged' : 'Watch'),
        mode: walletMode,
        address: address.trim() || undefined,
        viewKey: walletMode === 'unmanaged' ? viewKey.trim() : undefined,
      },
      {
        onSuccess: (res) => {
          add.mutate({
            id: res.walletId,
            symbol: target.symbol,
            label: res.label,
            address: res.address,
            source: mod.sourceFor(walletMode),
            mode: walletMode,
            sync: res.sync,
            viewKeyFingerprint: res.viewKeyFingerprint ?? (viewKey ? fingerprintViewKey(viewKey) : undefined),
          });
          toast({
            kind: 'ok',
            title: walletMode === 'managed' ? 'Managed wallet' : 'Wallet registered',
            sub: res.address ? res.address.slice(0, 14) + '…' : 'Sync pending',
          });
          setViewKey('');
          if (res.address) {
            setCreated({
              id: res.walletId,
              symbol: target.symbol,
              label: res.label,
              address: res.address,
              source: mod.sourceFor(walletMode),
              mode: walletMode,
              sync: res.sync,
              viewKeyFingerprint: res.viewKeyFingerprint ?? (viewKey ? fingerprintViewKey(viewKey) : undefined),
              createdAt: Date.now(),
            });
          } else close();
        },
        onError: () => toast({ kind: 'error', title: 'Could not create wallet', sub: 'Try again.' }),
      },
    );
  };

  const busy = add.isPending || deriveNext.isPending || createServer.isPending;

  const modeLabel = (id: AddMode) => {
    if (id === 'derive') return 'Derive next';
    if (id === 'managed') return 'Managed';
    if (id === 'unmanaged') return 'Unmanaged';
    return 'Watch';
  };

  const modeChip = (id: AddMode) => (
    <PressableScale
      key={id}
      onPress={() => setMode(id)}
      style={{
        flex: 1,
        minWidth: '30%',
        paddingVertical: 10,
        borderRadius: radius.button,
        backgroundColor: mode === id ? color.deepPurple : color.track,
        alignItems: 'center',
      }}
    >
      <Text
        style={{
          fontFamily: font.body,
          fontWeight: '700',
          fontSize: 12,
          color: mode === id ? color.gold : color.textMuted,
        }}
      >
        {modeLabel(id)}
      </Text>
    </PressableScale>
  );

  return (
    <>
      <View style={[{ backgroundColor: color.surface, borderRadius: radius.card, paddingHorizontal: 14 }, shadow.card]}>
        {holdings.map((h, i) => (
          <AssetRow
            key={h.symbol}
            h={h}
            last={i === holdings.length - 1 && hiddenHoldings.length === 0}
            hidden={hidden}
            onAddWallet={h.type === 'crypto' ? () => openAdd(h) : undefined}
          />
        ))}
        {hiddenHoldings.length > 0 ? (
          <>
            <PressableScale
              onPress={() => setOtherOpen((o) => !o)}
              style={{
                flexDirection: 'row',
                alignItems: 'center',
                justifyContent: 'space-between',
                paddingVertical: 14,
                borderTopWidth: holdings.length ? 1 : 0,
                borderTopColor: color.hairline,
              }}
            >
              <Text style={{ fontWeight: '700', fontSize: 13, fontFamily: font.body, color: color.textMuted }}>
                Other assets ({hiddenHoldings.length})
              </Text>
              <Text style={{ fontFamily: font.mono, fontSize: 11, color: color.textSubtle }}>
                {otherOpen ? '▲' : '▼'}
              </Text>
            </PressableScale>
            {otherOpen
              ? hiddenHoldings.map((h, i) => (
                  <AssetRow
                    key={'hidden-' + h.symbol}
                    h={h}
                    last={i === hiddenHoldings.length - 1}
                    hidden={hidden}
                    onAddWallet={h.type === 'crypto' ? () => openAdd(h) : undefined}
                  />
                ))
              : null}
          </>
        ) : null}
      </View>

      <Modal visible={!!target} transparent animationType="fade" onRequestClose={close}>
        <Pressable style={{ flex: 1, backgroundColor: '#00000088', justifyContent: 'flex-end' }} onPress={close}>
          <Pressable
            onPress={(e) => e.stopPropagation()}
            style={{
              backgroundColor: color.surfaceRaised,
              borderTopLeftRadius: radius.panel,
              borderTopRightRadius: radius.panel,
              padding: 20,
              gap: 12,
              paddingBottom: 32,
              maxHeight: '88%',
            }}
          >
            <ScrollView keyboardShouldPersistTaps="handled" contentContainerStyle={{ gap: 12 }}>
              {created?.address ? (
                <>
                  <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 20, color: color.text }}>
                    {created.label} ready
                  </Text>
                  <Text style={{ fontSize: 13, color: color.textMuted, fontFamily: font.body }}>
                    Scan or copy this {created.symbol} address. It is saved on this device only.
                  </Text>
                  <View style={{ alignItems: 'center', paddingVertical: 8 }}>
                    <QrCode value={buildPaymentUri(created.symbol, created.address)} size={180} />
                  </View>
                  <Text
                    selectable
                    style={{ fontFamily: font.mono, fontSize: 12, color: color.text, textAlign: 'center' }}
                  >
                    {created.address}
                  </Text>
                  {created.derivationPath ? (
                    <Text style={{ fontFamily: font.mono, fontSize: 11, color: color.textSubtle, textAlign: 'center' }}>
                      {created.derivationPath}
                    </Text>
                  ) : null}
                  <Button label="Done" onPress={close} />
                </>
              ) : (
                <>
              <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 20, color: color.text }}>
                Add {target?.symbol} wallet
              </Text>
              <Text style={{ fontSize: 13, color: color.textMuted, fontFamily: font.body }}>
                {mod?.notes ??
                  (canDerive
                    ? 'Derive the next account from your on-device seed, or paste a watch-only address.'
                    : 'Paste a watch-only address.')}
              </Text>

              {mod && mod.addModes.length > 1 ? (
                <View style={{ flexDirection: 'row', flexWrap: 'wrap', gap: 8 }}>{mod.addModes.map(modeChip)}</View>
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

              {mod?.usesServerWallets && (mode === 'unmanaged' || mode === 'watch') ? (
                <TextInput
                  value={address}
                  onChangeText={setAddress}
                  placeholder="Primary address"
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

              {mod?.usesServerWallets && mode === 'unmanaged' ? (
                <TextInput
                  value={viewKey}
                  onChangeText={setViewKey}
                  placeholder="Private view key (not spend key)"
                  placeholderTextColor={color.textSubtle}
                  autoCapitalize="none"
                  autoCorrect={false}
                  secureTextEntry
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

              {!mod?.usesServerWallets && (mode === 'watch' || !canDerive) ? (
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
                label={
                  mod?.usesServerWallets
                    ? mode === 'managed'
                      ? 'Create managed wallet'
                      : mode === 'unmanaged'
                        ? 'Register view sync'
                        : 'Add watch address'
                    : mode === 'derive' && canDerive
                      ? 'Derive next account'
                      : 'Add wallet'
                }
                busy={busy}
                onPress={() => {
                  if (mod?.usesServerWallets) submitServerWallet();
                  else if (mode === 'derive' && canDerive) submitDerive();
                  else submitWatch();
                }}
              />
              <PressableScale onPress={close} style={{ alignItems: 'center', paddingVertical: 8 }}>
                <Text style={{ color: color.textSubtle, fontWeight: '600', fontFamily: font.body }}>Cancel</Text>
              </PressableScale>
                </>
              )}
            </ScrollView>
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
