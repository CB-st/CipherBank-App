import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { WelcomeScreen } from '@/screens/onboarding/WelcomeScreen';
import { KeysScreen } from '@/screens/onboarding/KeysScreen';
import { BackupQuizScreen } from '@/screens/onboarding/BackupQuizScreen';
import { SetPinScreen } from '@/screens/onboarding/SetPinScreen';

const Stack = createNativeStackNavigator();

/** Welcome → Keys → BackupQuiz → SetPin. */
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
      <Stack.Screen name="BackupQuiz" component={BackupQuizScreen} />
      <Stack.Screen name="SetPin" component={SetPinScreen} />
    </Stack.Navigator>
  );
}
