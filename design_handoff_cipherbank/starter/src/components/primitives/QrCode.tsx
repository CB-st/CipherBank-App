import React, { useMemo } from 'react';
import { View, ActivityIndicator } from 'react-native';
import Svg, { Rect } from 'react-native-svg';
import qrcode from 'qrcode-generator';
import { color } from '@/theme';

type Props = {
  /** Payload encoded in the QR (address or payment URI). */
  value: string;
  size?: number;
  quietZone?: number;
  colorDark?: string;
  colorLight?: string;
};

/**
 * Device-side QR from a string — no network.
 * Uses qrcode-generator (Metro-safe) + react-native-svg.
 */
export function QrCode({
  value,
  size = 172,
  quietZone = 2,
  colorDark = color.ink,
  colorLight = '#FFFFFF',
}: Props) {
  const matrix = useMemo(() => {
    const trimmed = value?.trim();
    if (!trimmed) return null;
    try {
      // type 0 = auto size
      const qr = qrcode(0, 'M');
      qr.addData(trimmed);
      qr.make();
      const n = qr.getModuleCount();
      const cells: boolean[][] = [];
      for (let r = 0; r < n; r++) {
        const row: boolean[] = [];
        for (let c = 0; c < n; c++) row.push(qr.isDark(r, c));
        cells.push(row);
      }
      return cells;
    } catch {
      return null;
    }
  }, [value]);

  if (!value?.trim()) {
    return (
      <View
        style={{
          width: size,
          height: size,
          borderRadius: 12,
          backgroundColor: colorLight,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      />
    );
  }

  if (!matrix) {
    return (
      <View
        style={{
          width: size,
          height: size,
          borderRadius: 12,
          backgroundColor: colorLight,
          alignItems: 'center',
          justifyContent: 'center',
        }}
      >
        <ActivityIndicator color={color.goldDark} />
      </View>
    );
  }

  const n = matrix.length;
  const modules = n + quietZone * 2;
  const cell = size / modules;

  return (
    <View style={{ width: size, height: size, backgroundColor: colorLight, borderRadius: 12, overflow: 'hidden' }}>
      <Svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
        <Rect x={0} y={0} width={size} height={size} fill={colorLight} />
        {matrix.map((row, r) =>
          row.map((dark, c) =>
            dark ? (
              <Rect
                key={`${r}-${c}`}
                x={(c + quietZone) * cell}
                y={(r + quietZone) * cell}
                width={cell}
                height={cell}
                fill={colorDark}
              />
            ) : null,
          ),
        )}
      </Svg>
    </View>
  );
}
