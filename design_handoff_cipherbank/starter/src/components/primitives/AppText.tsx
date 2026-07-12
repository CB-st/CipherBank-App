import React from 'react';
import { Text, TextProps } from 'react-native';
import { color, font } from '@/theme';

type Variant = 'display' | 'title' | 'body' | 'label' | 'mono';
const STYLE: Record<Variant, any> = {
  display: { fontFamily: font.display, fontWeight: '700', letterSpacing: -1 },
  title:   { fontFamily: font.display, fontWeight: '700', fontSize: 18, letterSpacing: -0.4 },
  body:    { fontFamily: font.body, fontSize: 15 },
  label:   { fontFamily: font.body, fontWeight: '600', fontSize: 13 },
  mono:    { fontFamily: font.mono, fontSize: 12 },
};
export function AppText({ variant = 'body', style, ...rest }: TextProps & { variant?: Variant }) {
  return <Text {...rest} style={[{ color: color.text }, STYLE[variant], style]} />;
}
