import React from 'react';
import Svg, { Path, Rect, Circle } from 'react-native-svg';
import { color as C } from '@/theme';

/** Central icon registry. Add new glyphs here; screens reference by name only. */
export type IconName =
  | 'convert' | 'send' | 'pay' | 'receive' | 'home' | 'activity' | 'profile'
  | 'shield' | 'shield-check' | 'bell' | 'eye' | 'eye-off' | 'qr' | 'copy'
  | 'share' | 'back' | 'chevron' | 'caret-down' | 'plus' | 'check' | 'close'
  | 'wifi' | 'download' | 'request';

const P = (d: string) => ({ d });
const REG: Record<IconName, { paths: string[]; fill?: boolean }> = {
  convert:      { paths: ['M7 4v13M7 4L4 7M7 4l3 3M17 20V7M17 20l3-3M17 20l-3-3'] },
  send:         { paths: ['M22 2L11 13M22 2l-7 20-4-9-9-4 20-7z'] },
  pay:          { paths: ['M2 5h20v14H2zM2 10h20'] },
  receive:      { paths: ['M12 19V5M12 19l-6-6M12 19l6-6'] },
  home:         { paths: ['M3 10l9-7 9 7v9a2 2 0 01-2 2H5a2 2 0 01-2-2z'] },
  activity:     { paths: ['M3 12h4l3 8 4-16 3 8h4'] },
  profile:      { paths: ['M12 12a4 4 0 100-8 4 4 0 000 8zM4 21v-1a6 6 0 016-6h4a6 6 0 016 6v1'] },
  shield:       { paths: ['M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z'] },
  'shield-check':{ paths: ['M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z','M9 12l2 2 4-4'] },
  bell:         { paths: ['M18 8A6 6 0 006 8c0 7-3 9-3 9h18s-3-2-3-9','M13.7 21a2 2 0 01-3.4 0'] },
  eye:          { paths: ['M1 12s4-8 11-8 11 8 11 8-4 8-11 8S1 12 1 12z','M12 15a3 3 0 100-6 3 3 0 000 6z'] },
  'eye-off':    { paths: ['M17.9 17.9A10.7 10.7 0 0112 20C5 20 1 12 1 12a19 19 0 015.1-5.9M9.9 4.2A10.9 10.9 0 0112 4c7 0 11 8 11 8a19 19 0 01-2.2 3.2M1 1l22 22','M9.9 9.9a3 3 0 004.2 4.2'] },
  qr:           { paths: ['M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h3v3h-3zM19 19h2v2h-2z'] },
  copy:         { paths: ['M9 9h11v11H9zM5 15V5a2 2 0 012-2h10'] },
  share:        { paths: ['M4 12v8a2 2 0 002 2h12a2 2 0 002-2v-8','M16 6l-4-4-4 4','M12 2v14'] },
  back:         { paths: ['M15 18l-6-6 6-6'] },
  chevron:      { paths: ['M9 6l6 6-6 6'] },
  'caret-down': { paths: ['M6 9l6 6 6-6'] },
  plus:         { paths: ['M12 5v14M5 12h14'] },
  check:        { paths: ['M20 6L9 17l-5-5'] },
  close:        { paths: ['M18 6L6 18M6 6l12 12'] },
  wifi:         { paths: ['M5 12.5a10 10 0 0114 0M8.5 16a5 5 0 017 0M12 19.5h.01'] },
  download:     { paths: ['M12 3v12M12 15l-4-4M12 15l4-4','M4 17v3h16v-3'] },
  request:      { paths: ['M12 3v12M12 15l-4-4M12 15l4-4','M4 17v3h16v-3'] },
};

export function Icon({ name, size = 22, color = C.deepPurple, strokeWidth = 2.2 }:
  { name: IconName; size?: number; color?: string; strokeWidth?: number }) {
  const def = REG[name];
  return (
    <Svg width={size} height={size} viewBox="0 0 24 24" fill="none">
      {def.paths.map((d, i) => (
        <Path key={i} d={d} stroke={color} strokeWidth={strokeWidth} strokeLinecap="round" strokeLinejoin="round" />
      ))}
    </Svg>
  );
}
