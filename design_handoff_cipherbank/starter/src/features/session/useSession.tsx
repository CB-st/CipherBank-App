import React, { createContext, useContext, useState, useEffect } from 'react';
import { createLocalCustody, hasLocalCustody, unlockLocalCustody } from '@/features/vault/custody';
import { api } from '@/lib/apiClient';

/** Self-custody: local mnemonic never leaves the device; session token is server-issued. */
interface Session {
  booting: boolean;
  hasWallet: boolean;
  unlocked: boolean;
  unlock: () => Promise<boolean>;
  createWallet: () => Promise<void>;
}

const Ctx = createContext<Session>({
  booting: true,
  hasWallet: false,
  unlocked: false,
  unlock: async () => false,
  createWallet: async () => {},
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
            await createLocalCustody();
          } catch {
            /* continue with UI flag */
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

  const createWallet = async () => {
    await createLocalCustody();
    try {
      await api.post('/session', { deviceBound: true });
    } catch {
      /* session optional during onboarding */
    }
    setHasWallet(true);
    setUnlocked(true);
  };

  const unlock = async () => {
    const ok = await unlockLocalCustody();
    setUnlocked(ok);
    return ok;
  };

  return (
    <Ctx.Provider value={{ booting, hasWallet, unlocked, unlock, createWallet }}>{children}</Ctx.Provider>
  );
}
