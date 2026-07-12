import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { WelcomeScreen } from '@/screens/onboarding/WelcomeScreen';
import { KeysScreen } from '@/screens/onboarding/KeysScreen';

const Stack = createNativeStackNavigator();

/** Welcome -> Secure keys -> (BankLink -> Fund). See screens/onboarding. */
export function OnboardingStack() {
  return (
    <Stack.Navigator
      screenOptions={{
        headerShown: false,
        animation: 'fade_from_bottom',
        animationDuration: 280,
      }}
    >
      <Stack.Screen name="Welcome" component={WelcomeScreen} />
      <Stack.Screen name="Keys" component={KeysScreen} />
    </Stack.Navigator>
  );
}
