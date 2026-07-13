import React, { useEffect, useRef, useState } from 'react';
import { View, ActivityIndicator } from 'react-native';
import { color } from '@/theme';
import { ActivationProvider, useActivation } from './activation';
import { runP2Bootstrap } from './bootstrap';
import { startBackgroundSync } from './backgroundSync';

function BootstrapGate({ children }: { children: React.ReactNode }) {
  const [ready, setReady] = useState(false);
  const { lastInteractiveAt } = useActivation();
  const interactiveRef = useRef(lastInteractiveAt);
  interactiveRef.current = lastInteractiveAt;

  useEffect(() => {
    let cancelled = false;
    (async () => {
      try {
        await runP2Bootstrap();
      } catch {
        /* degrade: UI still mounts; repos retry on demand */
      }
      if (!cancelled) setReady(true);
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!ready) return;
    return startBackgroundSync({
      getLastInteractiveAt: () => interactiveRef.current,
    });
  }, [ready]);

  if (!ready) {
    return (
      <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: color.canvas }}>
        <ActivityIndicator color={color.goldDark} />
      </View>
    );
  }

  return <>{children}</>;
}

/** Opens SQLite (P2), then starts idle+charging P3 watcher. */
export function BootstrapProvider({ children }: { children: React.ReactNode }) {
  return (
    <ActivationProvider>
      <BootstrapGate>{children}</BootstrapGate>
    </ActivationProvider>
  );
}
