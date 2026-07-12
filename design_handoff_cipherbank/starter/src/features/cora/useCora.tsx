import React, { createContext, useContext, useCallback } from 'react';
import { usePrefs } from '@/features/prefs/usePrefs';

/** Cora lines per screen — dry, one-liners. Serious about money, dry about the rest. */
export const CORA_LINES: Record<string, string> = {
  home: "Rates move all day. Your privacy doesn't.",
  homeLoad: 'Pulling your balances. One sec — doing it properly.',
  convert: 'Locked-in rate. No spread games.',
  pay: 'Rent, paid partly in Dogecoin. Bold. Also completely fine.',
  send: "Instant. As it should've been all along.",
  receive: 'They see the handle. Not you.',
  keys: "No 'forgot password' button here. That's a feature, not an oversight.",
  profile: 'Your house, your rules. I just keep the keys where you left them.',
};

interface CoraCtx {
  enabled: boolean;
  toggle: () => void;
  setEnabled: (v: boolean) => void;
  lineFor: (screen: string) => string;
  source?: any;
}

const Ctx = createContext<CoraCtx>({
  enabled: true,
  toggle: () => {},
  setEnabled: () => {},
  lineFor: (s) => CORA_LINES[s] ?? '',
});
export const useCora = () => useContext(Ctx);

/** Cora visibility is driven by UserPrefs.coraEnabled (Profile + local/remote sync). */
export function CoraProvider({ children, source }: { children: React.ReactNode; source?: any }) {
  const { prefs, setCoraEnabled } = usePrefs();
  const enabled = prefs.coraEnabled;
  const toggle = useCallback(() => setCoraEnabled(!enabled), [enabled, setCoraEnabled]);
  const setEnabled = useCallback((v: boolean) => setCoraEnabled(v), [setCoraEnabled]);
  const lineFor = useCallback((s: string) => CORA_LINES[s] ?? '', []);
  return <Ctx.Provider value={{ enabled, toggle, setEnabled, lineFor, source }}>{children}</Ctx.Provider>;
}
