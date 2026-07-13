import React, { createContext, useCallback, useContext, useMemo, useRef, useState } from 'react';
import { jobQueue } from './jobQueue';

export type Activation =
  | 'shell'
  | 'chart'
  | 'convert'
  | 'nfc_pos'
  | 'background';

type ActivationCtx = {
  activation: Activation;
  setActivation: (a: Activation) => void;
  /** Last time a P0/P1 interactive activation was set (ms). */
  lastInteractiveAt: number;
  markInteractive: () => void;
};

const Ctx = createContext<ActivationCtx>({
  activation: 'shell',
  setActivation: () => {},
  lastInteractiveAt: Date.now(),
  markInteractive: () => {},
});

export const useActivation = () => useContext(Ctx);

const INTERACTIVE: Activation[] = ['chart', 'convert', 'nfc_pos'];

export function ActivationProvider({ children }: { children: React.ReactNode }) {
  const [activation, setActivationState] = useState<Activation>('shell');
  const lastInteractiveAt = useRef(Date.now());
  const [, bump] = useState(0);

  const markInteractive = useCallback(() => {
    lastInteractiveAt.current = Date.now();
    jobQueue.setPauseP3(true);
    bump((n) => n + 1);
  }, []);

  const setActivation = useCallback((a: Activation) => {
    setActivationState(a);
    if (INTERACTIVE.includes(a)) {
      lastInteractiveAt.current = Date.now();
      jobQueue.setPauseP3(true);
      bump((n) => n + 1);
    }
  }, []);

  const value = useMemo(
    () => ({
      activation,
      setActivation,
      lastInteractiveAt: lastInteractiveAt.current,
      markInteractive,
    }),
    [activation, setActivation, markInteractive],
  );

  return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function isInteractiveActivation(a: Activation): boolean {
  return INTERACTIVE.includes(a);
}
