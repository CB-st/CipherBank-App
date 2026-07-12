import React, { useEffect, useCallback } from 'react';
import { View, ActivityIndicator } from 'react-native';
import { GestureHandlerRootView } from 'react-native-gesture-handler';
import { SafeAreaProvider } from 'react-native-safe-area-context';
import { NavigationContainer } from '@react-navigation/native';
import { QueryClientProvider } from '@tanstack/react-query';
import { useFonts, SpaceGrotesk_700Bold } from '@expo-google-fonts/space-grotesk';
import {
  Manrope_400Regular,
  Manrope_600SemiBold,
  Manrope_700Bold,
  Manrope_800ExtraBold,
} from '@expo-google-fonts/manrope';
import { SpaceMono_400Regular, SpaceMono_700Bold } from '@expo-google-fonts/space-mono';
import * as SplashScreen from 'expo-splash-screen';
import { queryClient } from '@/lib/queryClient';
import { connectStream } from '@/lib/socket';
import { SessionProvider } from '@/features/session/useSession';
import { PrefsProvider } from '@/features/prefs/usePrefs';
import { CoraProvider } from '@/features/cora/useCora';
import { ToastProvider } from '@/components/primitives/Toast';
import { RootNavigator } from '@/app/RootNavigator';
import { ThemeProvider, color, useTheme } from '@/theme';

SplashScreen.preventAutoHideAsync().catch(() => {});

function ThemedNavigation() {
  const { appearance } = useTheme();
  return (
    <NavigationContainer key={appearance}>
      <RootNavigator />
    </NavigationContainer>
  );
}

/**
 * App shell. Providers only — renders synchronously, never awaits the network.
 * Data streams into screens via React Query hooks in src/features/*.
 */
export default function App() {
  const [fontsLoaded] = useFonts({
    SpaceGrotesk: SpaceGrotesk_700Bold,
    Manrope: Manrope_400Regular,
    Manrope_600SemiBold,
    Manrope_700Bold,
    Manrope_800ExtraBold,
    SpaceMono: SpaceMono_400Regular,
    SpaceMono_700Bold,
  });

  useEffect(() => {
    const ws = connectStream();
    return () => ws.close();
  }, []);

  const onLayout = useCallback(async () => {
    if (fontsLoaded) await SplashScreen.hideAsync();
  }, [fontsLoaded]);

  if (!fontsLoaded) {
    return (
      <View style={{ flex: 1, alignItems: 'center', justifyContent: 'center', backgroundColor: color.canvas }}>
        <ActivityIndicator color={color.goldDark} />
      </View>
    );
  }

  return (
    <GestureHandlerRootView style={{ flex: 1 }} onLayout={onLayout}>
      <SafeAreaProvider>
        <QueryClientProvider client={queryClient}>
          <SessionProvider>
            <PrefsProvider>
              <ThemeProvider>
                <CoraProvider>
                  <ToastProvider>
                    <ThemedNavigation />
                  </ToastProvider>
                </CoraProvider>
              </ThemeProvider>
            </PrefsProvider>
          </SessionProvider>
        </QueryClientProvider>
      </SafeAreaProvider>
    </GestureHandlerRootView>
  );
}
