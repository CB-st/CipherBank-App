// Design tokens. Dark is the default app chrome; light is opt-in via Profile.
// `color` is a shared mutable object — ThemeProvider assigns the active palette in place
// so existing `import { color }` call sites pick up the new values on re-render.

export type Appearance = 'dark' | 'light';

export type ThemeColors = {
  gold: string;
  goldDark: string;
  violet: string;
  deepPurple: string;
  ink: string;
  inkDeep: string;
  canvas: string;
  surface: string;
  surfaceRaised: string;
  track: string;
  green: string;
  red: string;
  text: string;
  textMuted: string;
  textSubtle: string;
  hairline: string;
  onDark: string;
  onDarkMuted: string;
  onDarkSubtle: string;
  tabBar: string;
  tabBarBorder: string;
  skeleton: string;
  skeletonShine: string;
};

export const darkColor: ThemeColors = {
  gold: '#F2C14E',
  goldDark: '#C9971F',
  violet: '#7B4DFF',
  deepPurple: '#2B1E3E',
  ink: '#111318',
  inkDeep: '#0C0D11',
  canvas: '#0C0D11',
  surface: '#1A1624',
  surfaceRaised: '#241E30',
  track: '#2A2438',
  green: '#3FA46A',
  red: '#C0574B',
  text: '#F7F5F2',
  textMuted: '#C8C2D2',
  textSubtle: '#8A8496',
  hairline: '#2E2838',
  onDark: '#F7F5F2',
  onDarkMuted: '#C8C2D2',
  onDarkSubtle: '#B3AAC6',
  tabBar: 'rgba(12,13,17,0.96)',
  tabBarBorder: '#2E2838',
  skeleton: '#241E30',
  skeletonShine: '#2E2838',
};

export const lightColor: ThemeColors = {
  gold: '#F2C14E',
  goldDark: '#C9971F',
  violet: '#7B4DFF',
  deepPurple: '#2B1E3E',
  ink: '#111318',
  inkDeep: '#0C0D11',
  canvas: '#F7F5F2',
  surface: '#FFFFFF',
  surfaceRaised: '#FFFFFF',
  track: '#ECEAE6',
  green: '#3FA46A',
  red: '#C0574B',
  text: '#111318',
  textMuted: '#5A5563',
  textSubtle: '#8A8496',
  hairline: '#F0EDEA',
  onDark: '#F7F5F2',
  onDarkMuted: '#C8C2D2',
  onDarkSubtle: '#B3AAC6',
  tabBar: 'rgba(247,245,242,0.96)',
  tabBarBorder: '#ECECEC',
  skeleton: '#ECEAE6',
  skeletonShine: '#F5F3F0',
};

/** Active palette — starts dark; ThemeProvider assigns light/dark in place. */
export const color: ThemeColors = { ...darkColor };

export function applyAppearance(appearance: Appearance): ThemeColors {
  const next = appearance === 'light' ? lightColor : darkColor;
  Object.assign(color, next);
  return color;
}

export const font = {
  display: 'SpaceGrotesk',
  body: 'Manrope',
  mono: 'SpaceMono',
} as const;

export const radius = { chip: 10, button: 14, card: 18, panel: 22, pill: 30 } as const;
export const space = { 1: 4, 2: 8, 3: 14, 4: 18, 5: 22, 6: 30 } as const;

export const shadow = {
  card: {
    shadowColor: '#000',
    shadowOpacity: 0.18,
    shadowRadius: 12,
    shadowOffset: { width: 0, height: 4 },
    elevation: 3,
  },
  gold: {
    shadowColor: '#F2C14E',
    shadowOpacity: 0.35,
    shadowRadius: 20,
    shadowOffset: { width: 0, height: 8 },
    elevation: 6,
  },
} as const;
