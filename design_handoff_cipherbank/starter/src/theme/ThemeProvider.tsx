import React, { createContext, useContext, useLayoutEffect, useMemo } from 'react';
import { StatusBar } from 'expo-status-bar';
import { usePrefs } from '@/features/prefs/usePrefs';
import { applyAppearance, color, type Appearance, type ThemeColors } from './tokens';

interface ThemeCtx {
  appearance: Appearance;
  color: ThemeColors;
  isDark: boolean;
}

const Ctx = createContext<ThemeCtx>({
  appearance: 'dark',
  color,
  isDark: true,
});

export const useTheme = () => useContext(Ctx);

/** Syncs palette to prefs.appearance (default dark). */
export function ThemeProvider({ children }: { children: React.ReactNode }) {
  const { prefs } = usePrefs();
  const appearance: Appearance = prefs.appearance === 'light' ? 'light' : 'dark';

  useLayoutEffect(() => {
    applyAppearance(appearance);
  }, [appearance]);

  const value = useMemo<ThemeCtx>(
    () => ({
      appearance,
      color: applyAppearance(appearance),
      isDark: appearance === 'dark',
    }),
    [appearance],
  );

  return (
    <Ctx.Provider value={value}>
      <StatusBar style={appearance === 'dark' ? 'light' : 'dark'} />
      {children}
    </Ctx.Provider>
  );
}
