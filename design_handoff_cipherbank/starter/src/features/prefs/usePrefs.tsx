import React, { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import {
  DEFAULT_PREFS,
  type AppearancePref,
  type BaseCurrency,
  type HomeSection,
  type SendSpeedPref,
  type UserPrefs,
} from './prefs.types';
import { normalizePrefs } from './localeCurrency';
import { fetchRemotePrefs, loadLocalPrefs, pushRemotePrefs, saveLocalPrefs } from './prefs.store';

interface PrefsCtx {
  prefs: UserPrefs;
  ready: boolean;
  setPref: <K extends keyof UserPrefs>(key: K, value: UserPrefs[K]) => void;
  toggleSection: (section: HomeSection) => void;
  moveSection: (section: HomeSection, dir: -1 | 1) => void;
  setCoraEnabled: (v: boolean) => void;
  setValuesHiddenOnLaunch: (v: boolean) => void;
  setDefaultSendSpeed: (v: SendSpeedPref) => void;
  setAppearance: (v: AppearancePref) => void;
  setBaseCurrency: (v: BaseCurrency) => void;
  toggleEnabledCurrency: (symbol: string) => void;
}

const Ctx = createContext<PrefsCtx>({
  prefs: DEFAULT_PREFS,
  ready: false,
  setPref: () => {},
  toggleSection: () => {},
  moveSection: () => {},
  setCoraEnabled: () => {},
  setValuesHiddenOnLaunch: () => {},
  setDefaultSendSpeed: () => {},
  setAppearance: () => {},
  setBaseCurrency: () => {},
  toggleEnabledCurrency: () => {},
});

export const usePrefs = () => useContext(Ctx);

export function PrefsProvider({ children }: { children: React.ReactNode }) {
  const [prefs, setPrefs] = useState<UserPrefs>(DEFAULT_PREFS);
  const [ready, setReady] = useState(false);
  const syncTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const prefsRef = useRef(prefs);
  prefsRef.current = prefs;

  useEffect(() => {
    (async () => {
      const local = await loadLocalPrefs();
      setPrefs(local);
      try {
        const remote = await fetchRemotePrefs();
        const merged = normalizePrefs({
          ...local,
          ...remote,
          homeOrder: remote.homeOrder?.length ? remote.homeOrder : local.homeOrder,
          homeVisible: { ...local.homeVisible, ...remote.homeVisible },
          baseCurrency: remote.baseCurrency ?? local.baseCurrency,
          enabledCurrencies: remote.enabledCurrencies?.length
            ? remote.enabledCurrencies
            : local.enabledCurrencies,
        });
        setPrefs(merged);
        await saveLocalPrefs(merged);
      } catch {
        /* offline / mock miss — keep local */
      }
      setReady(true);
    })();
  }, []);

  const persist = useCallback((next: UserPrefs) => {
    setPrefs(next);
    if (syncTimer.current) clearTimeout(syncTimer.current);
    syncTimer.current = setTimeout(async () => {
      await saveLocalPrefs(next);
      try {
        await pushRemotePrefs(next);
      } catch {
        /* local remains source of truth until sync succeeds */
      }
    }, 400);
  }, []);

  const setPref = useCallback(
    <K extends keyof UserPrefs>(key: K, value: UserPrefs[K]) => {
      persist({ ...prefsRef.current, [key]: value });
    },
    [persist],
  );

  const toggleSection = useCallback(
    (section: HomeSection) => {
      const cur = prefsRef.current;
      persist({
        ...cur,
        homeVisible: { ...cur.homeVisible, [section]: !cur.homeVisible[section] },
      });
    },
    [persist],
  );

  const moveSection = useCallback(
    (section: HomeSection, dir: -1 | 1) => {
      const order = [...prefsRef.current.homeOrder];
      const i = order.indexOf(section);
      if (i < 0) return;
      const j = i + dir;
      if (j < 0 || j >= order.length) return;
      [order[i], order[j]] = [order[j], order[i]];
      persist({ ...prefsRef.current, homeOrder: order });
    },
    [persist],
  );

  const setBaseCurrency = useCallback(
    (v: BaseCurrency) => setPref('baseCurrency', v),
    [setPref],
  );

  const toggleEnabledCurrency = useCallback(
    (symbol: string) => {
      const sym = symbol.toUpperCase();
      const cur = prefsRef.current;
      const set = new Set(cur.enabledCurrencies.map((s) => s.toUpperCase()));
      if (set.has(sym)) set.delete(sym);
      else set.add(sym);
      persist({ ...cur, enabledCurrencies: Array.from(set).sort() });
    },
    [persist],
  );

  const value = useMemo<PrefsCtx>(
    () => ({
      prefs,
      ready,
      setPref,
      toggleSection,
      moveSection,
      setCoraEnabled: (v) => setPref('coraEnabled', v),
      setValuesHiddenOnLaunch: (v) => setPref('valuesHiddenOnLaunch', v),
      setDefaultSendSpeed: (v) => setPref('defaultSendSpeed', v),
      setAppearance: (v) => setPref('appearance', v),
      setBaseCurrency,
      toggleEnabledCurrency,
    }),
    [prefs, ready, setPref, toggleSection, moveSection, setBaseCurrency, toggleEnabledCurrency],
  );

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}
