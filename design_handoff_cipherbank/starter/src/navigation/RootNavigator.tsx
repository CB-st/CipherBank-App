import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { useSession } from '@/features/session/useSession';
import { SplashScreen } from '@/components/loading/SplashScreen';
import { UnlockScreen } from '@/screens/lock/UnlockScreen';
import { MainStack } from './MainStack';
import { OnboardingStack } from './OnboardingStack';

const Stack = createNativeStackNavigator();

/** Boot splash → onboarding or lock gate → tab shell. */
export function RootNavigator() {
  const { booting, hasWallet, unlocked } = useSession();
  if (booting) return <SplashScreen />;

  if (hasWallet && !unlocked) {
    return <UnlockScreen />;
  }

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
