import React, { createContext, useContext, useState, useCallback } from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import { color, radius, font } from '@/theme';

type Toast = { kind: 'pending' | 'ok' | 'error'; title: string; sub?: string };
const Ctx = createContext<(t: Toast) => void>(() => {});
export const useToast = () => useContext(Ctx);

/** Optimistic-action feedback. pending = purple + spinner, ok = green + check, error = red. */
export function ToastProvider({ children }: { children: React.ReactNode }) {
  const [toast, setToast] = useState<Toast | null>(null);
  const show = useCallback((t: Toast) => {
    setToast(t);
    setTimeout(() => setToast(null), t.kind === 'pending' ? 4000 : 2600);
  }, []);
  return (
    <Ctx.Provider value={show}>
      {children}
      {toast && (
        <View style={{ position: 'absolute', left: 16, right: 16, bottom: 96,
          backgroundColor: toast.kind === 'ok' ? '#1F6B45' : toast.kind === 'error' ? color.red : color.deepPurple,
          borderRadius: radius.button, padding: 14, flexDirection: 'row', gap: 11, alignItems: 'center' }}>
          {toast.kind === 'pending' ? <ActivityIndicator color="#fff" /> : null}
          <View style={{ flex: 1 }}>
            <Text style={{ color: '#fff', fontFamily: font.body, fontWeight: '700', fontSize: 13 }}>{toast.title}</Text>
            {toast.sub ? <Text style={{ color: '#ffffffcc', fontSize: 11 }}>{toast.sub}</Text> : null}
          </View>
        </View>
      )}
    </Ctx.Provider>
  );
}
