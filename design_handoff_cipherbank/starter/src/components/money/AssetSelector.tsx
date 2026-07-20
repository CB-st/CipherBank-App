import React from 'react';
import { Modal, View, Text, Pressable, FlatList } from 'react-native';
import { color, font } from '@/theme';
import { AssetGlyph } from './AssetGlyph';
import { listAssets } from '@/features/assets/assetConfig';

/** Bottom-sheet asset picker. Filter by type; disabled assets (securities) show a badge. */
export function AssetSelector({
  visible,
  onClose,
  onPick,
  type,
  allowedSymbols,
}: {
  visible: boolean;
  onClose: () => void;
  onPick: (symbol: string) => void;
  type?: 'crypto' | 'fiat';
  /** When set, only these tickers are selectable (from POST /currencies). */
  allowedSymbols?: string[];
}) {
  const allowed = allowedSymbols?.map((s) => s.toUpperCase());
  const assets = listAssets(type ? { type } : undefined).filter((a) => {
    if (!allowed || allowed.length === 0) return true;
    return allowed.includes(a.symbol.toUpperCase());
  });
  return (
    <Modal visible={visible} transparent animationType="slide" onRequestClose={onClose}>
      <Pressable onPress={onClose} style={{ flex: 1, backgroundColor: '#00000066', justifyContent: 'flex-end' }}>
        <Pressable style={{ backgroundColor: color.canvas, borderTopLeftRadius: 24, borderTopRightRadius: 24, padding: 18, paddingBottom: 34 }}>
          <Text style={{ fontFamily: font.display, fontWeight: '700', fontSize: 18, marginBottom: 12 }}>Choose asset</Text>
          <FlatList
            data={assets}
            keyExtractor={(a) => a.symbol}
            renderItem={({ item }) => (
              <Pressable disabled={item.enabled === false} onPress={() => { onPick(item.symbol); onClose(); }}
                style={{ flexDirection: 'row', alignItems: 'center', gap: 12, paddingVertical: 11, opacity: item.enabled === false ? 0.5 : 1 }}>
                <AssetGlyph symbol={item.symbol} />
                <View style={{ flex: 1 }}>
                  <Text style={{ fontWeight: '700', fontSize: 15 }}>{item.name}</Text>
                  <Text style={{ fontFamily: font.mono, fontSize: 12, color: color.textSubtle }}>{item.symbol}{item.note ? ' · ' + item.note : ''}</Text>
                </View>
                {item.badge ? <Text style={{ fontSize: 10, fontWeight: '800', color: '#B8860B' }}>{item.badge}</Text> : null}
              </Pressable>
            )}
          />
        </Pressable>
      </Pressable>
    </Modal>
  );
}
