import React, { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { AppState, type AppStateStatus, View } from 'react-native';
import {
  createLocalCustody,
  hasLocalCustody,
  unlockLocalCustody,
  lockLocalCustody,
  getSessionMnemonic,
  subscribeCustodyLock,
  beginAuthGate,
  endAuthGate,
  ensureDemoCustody,
} from '@/features/vault/custody';
import { requireAuth, type AuthReason } from '@/features/vault/requireAuth';
import { ensureDerivedWallets } from '@/features/wallets/localWallets';
import { api } from '@/lib/apiClient';
import { isSeedDemo } from '@/lib/runtimeFlags';

/** Default idle lock — shorter than custody mnemonic TTL. */
export const DEFAULT_APP_LOCK_IDLE_MS = 60_000;

interface Session {
  booting: boolean;
  hasWallet: boolean;
  unlocked: boolean;
  lock: () => void;
  touch: () => void;
  unlock: (pin?: string, opts?: { skipBiometrics?: boolean; reason?: AuthReason }) => Promise<boolean>;
  createWallet: () => Promise<void>;
  finishCustodySetup: () => Promise<void>;
  /** Idle timeout ms (from prefs when wired; default 60s). */
  idleMs: number;
  setIdleMs: (ms: number) => void;
}

const Ctx = createContext<Session>({
  booting: true,
  hasWallet: false,
  unlocked: false,
  lock: () => {},
  touch: () => {},
  unlock: async () => false,
  createWallet: async () => {},
  finishCustodySetup: async () => {},
  idleMs: DEFAULT_APP_LOCK_IDLE_MS,
  setIdleMs: () => {},
});
export const useSession = () => useContext(Ctx);

export function SessionProvider({ children }: { children: React.ReactNode }) {
  const [booting, setBooting] = useState(true);
  const [hasWallet, setHasWallet] = useState(false);
  const [unlocked, setUnlocked] = useState(false);
  const [idleMs, setIdleMs] = useState(DEFAULT_APP_LOCK_IDLE_MS);
  const lastTouch = useRef(Date.now());
  const unlockedRef = useRef(false);
  unlockedRef.current = unlocked;

  const lock = useCallback(() => {
    lockLocalCustody();
    setUnlocked(false);
  }, []);

  const touch = useCallback(() => {
    lastTouch.current = Date.now();
  }, []);

  useEffect(() => {
    return subscribeCustodyLock(() => setUnlocked(false));
  }, []);

  // Idle timeout while unlocked
  useEffect(() => {
    if (!unlocked || !hasWallet) return;
    const id = setInterval(() => {
      if (Date.now() - lastTouch.current >= idleMs) lock();
    }, 5_000);
    return () => clearInterval(id);
  }, [unlocked, hasWallet, idleMs, lock]);

  // True background only — biometric / device-PIN sheets set AppState to inactive.
  useEffect(() => {
    const onChange = (state: AppStateStatus) => {
      if (state === 'background' && unlockedRef.current) {
        lock();
      } else if (state === 'active') {
        lastTouch.current = Date.now();
      }
    };
    const sub = AppState.addEventListener('change', onChange);
    return () => sub.remove();
  }, [lock]);

  useEffect(() => {
    (async () => {
      if (isSeedDemo()) {
        try {
          await ensureDemoCustody();
        } catch (e) {
          console.warn('[session] ensureDemoCustody failed', e);
          try {
            await createLocalCustody({ pin: '000000' });
            lockLocalCustody();
          } catch (e2) {
            console.warn('[session] createLocalCustody failed', e2);
          }
        }
        setHasWallet(true);
        setUnlocked(false);
        setTimeout(() => setBooting(false), 500);
        return;
      }
      const local = await hasLocalCustody();
      setHasWallet(local);
      setUnlocked(false);
      setTimeout(() => setBooting(false), 700);
    })();
  }, []);

  const finishCustodySetup = async () => {
    try {
      await api.post('/session', { deviceBound: true });
    } catch {
      /* optional */
    }
    setHasWallet(true);
    setUnlocked(true);
    touch();
  };

  const createWallet = async () => {
    if (!(await hasLocalCustody())) {
      await createLocalCustody({ pin: '000000' });
      await ensureDerivedWallets();
    }
    await finishCustodySetup();
  };

  const unlock = async (pin?: string, opts?: { skipBiometrics?: boolean; reason?: AuthReason }) => {
    const reason = opts?.reason ?? 'app_unlock';
    beginAuthGate();
    try {
      // Custody already live (e.g. biometrics just succeeded) — sync shell only.
      if (!pin && opts?.skipBiometrics && getSessionMnemonic()) {
        setUnlocked(true);
        touch();
        return true;
      }

      const ok =
        pin || opts?.skipBiometrics
          ? await unlockLocalCustody({
              pin,
              skipBiometrics: true,
              force: !!pin, // PIN path always re-verifies; skip-bio without pin only when no PIN set
              promptMessage: undefined,
            })
          : await requireAuth({ reason, force: true });
      setUnlocked(ok);
      if (ok) {
        touch();
        if (getSessionMnemonic()) {
          try {
            await ensureDerivedWallets();
          } catch {
            /* optional */
          }
        }
      }
      return ok;
    } finally {
      endAuthGate();
    }
  };

  const value = useMemo(
    () => ({
      booting,
      hasWallet,
      unlocked,
      lock,
      touch,
      unlock,
      createWallet,
      finishCustodySetup,
      idleMs,
      setIdleMs,
    }),
    [booting, hasWallet, unlocked, lock, touch, idleMs],
  );

  return (
    <Ctx.Provider value={value}>
      <View
        style={{ flex: 1 }}
        onTouchStart={touch}
        onStartShouldSetResponderCapture={() => {
          touch();
          return false;
        }}
      >
        {children}
      </View>
    </Ctx.Provider>
  );
}
