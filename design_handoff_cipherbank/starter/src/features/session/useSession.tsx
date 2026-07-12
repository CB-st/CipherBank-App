import React, { createContext, useContext, useState, useEffect } from 'react';
import {
  createLocalCustody,
  hasLocalCustody,
  unlockLocalCustody,
  getSessionMnemonic,
} from '@/features/vault/custody';
import { ensureDerivedWallets } from '@/features/wallets/localWallets';
import { api } from '@/lib/apiClient';

/** Self-custody: local mnemonic never leaves the device; session token is server-issued. */
interface Session {
  booting: boolean;
  hasWallet: boolean;
  unlocked: boolean;
  unlock: (pin?: string) => Promise<boolean>;
  /** Legacy entry — prefer Keys → Quiz → SetPin → finishCustodySetup. */
  createWallet: () => Promise<void>;
  /** After SetPin seals custody + derives wallets. */
  finishCustodySetup: () => Promise<void>;
}

const Ctx = createContext<Session>({
  booting: true,
  hasWallet: false,
  unlocked: false,
  unlock: async () => false,
  createWallet: async () => {},
  finishCustodySetup: async () => {},
});
export const useSession = () => useContext(Ctx);

export function SessionProvider({ children }: { children: React.ReactNode }) {
  const [booting, setBooting] = useState(true);
  const [hasWallet, setHasWallet] = useState(false);
  const [unlocked, setUnlocked] = useState(false);

  useEffect(() => {
    (async () => {
      if (process.env.EXPO_PUBLIC_MOCK_HAS_WALLET === 'true') {
        const local = await hasLocalCustody();
        if (!local) {
          try {
            // Demo bootstrap: real BIP39 + encrypted blob + demo PIN (not for production funds).
            await createLocalCustody({ pin: '000000' });
            await ensureDerivedWallets();
          } catch {
            /* continue with UI flag */
          }
        } else {
          try {
            await unlockLocalCustody({ pin: '000000', skipBiometrics: true });
            if (getSessionMnemonic()) await ensureDerivedWallets();
          } catch {
            /* leave locked */
          }
        }
        setHasWallet(true);
        setUnlocked(true);
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
      /* session optional during onboarding */
    }
    setHasWallet(true);
    setUnlocked(true);
  };

  const createWallet = async () => {
    if (!(await hasLocalCustody())) {
      await createLocalCustody({ pin: '000000' });
      await ensureDerivedWallets();
    }
    await finishCustodySetup();
  };

  const unlock = async (pin?: string) => {
    const ok = await unlockLocalCustody(pin ? { pin, skipBiometrics: true } : {});
    setUnlocked(ok);
    if (ok && getSessionMnemonic()) {
      try {
        await ensureDerivedWallets();
      } catch {
        /* derive optional until chain sync */
      }
    }
    return ok;
  };

  return (
    <Ctx.Provider value={{ booting, hasWallet, unlocked, unlock, createWallet, finishCustodySetup }}>
      {children}
    </Ctx.Provider>
  );
}
