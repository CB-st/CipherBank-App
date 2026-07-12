import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useSession } from '@/features/session/useSession';
import { SplashScreen } from '@/components/loading/SplashScreen';
import { MainStack } from './MainStack';
import { OnboardingStack } from './OnboardingStack';

const Stack = createNativeStackNavigator();

/** Boot splash while session hydrates, then onboarding gate, then the tab shell. */
export function RootNavigator() {
  const { booting, hasWallet } = useSession();
  if (booting) return <SplashScreen />;
  return (
    <Stack.Navigator
      screenOptions={{
        headerShown: false,
        animation: 'fade',
        animationDuration: 220,
      }}
    >
      {hasWallet ? (
        <Stack.Screen name="Main" component={MainStack} />
      ) : (
        <Stack.Screen name="Onboarding" component={OnboardingStack} />
      )}
    </Stack.Navigator>
  );
}
