import { useCallback, useEffect, useState } from 'react';
import {
  getSetupState,
  markSetupComplete,
  type SetupPath,
} from '@/features/account/setupState';

export function useSetupState() {
  const [path, setPath] = useState<SetupPath | null>(null);
  const [complete, setComplete] = useState(true);
  const [ready, setReady] = useState(false);

  const refresh = useCallback(async () => {
    const s = await getSetupState();
    setPath(s.path);
    setComplete(s.complete);
    setReady(true);
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const completeSetup = useCallback(async () => {
    await markSetupComplete();
    setComplete(true);
  }, []);

  return { path, complete, ready, refresh, completeSetup, needsSetup: ready && !complete };
}
