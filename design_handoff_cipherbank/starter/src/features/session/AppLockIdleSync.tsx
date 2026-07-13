import { useEffect } from 'react';
import { usePrefs } from '@/features/prefs/usePrefs';
import { useSession } from '@/features/session/useSession';

/** Keeps session idle lock in sync with Profile prefs. */
export function AppLockIdleSync() {
  const { prefs, ready } = usePrefs();
  const { setIdleMs } = useSession();
  useEffect(() => {
    if (!ready) return;
    setIdleMs(Math.max(15, prefs.appLockIdleSec) * 1000);
  }, [ready, prefs.appLockIdleSec, setIdleMs]);
  return null;
}
