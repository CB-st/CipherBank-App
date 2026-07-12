import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { TabNavigator } from './TabNavigator';
import { MockPosScreen } from '@/screens/pos/MockPosScreen';
import { KeysScreen } from '@/screens/onboarding/KeysScreen';
import { BackupQuizScreen } from '@/screens/onboarding/BackupQuizScreen';
import { SetPinScreen } from '@/screens/onboarding/SetPinScreen';

const Stack = createNativeStackNavigator();

/** Tab shell + custody setup + modal labs (POS, etc.). */
export function MainStack() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false, animation: 'fade_from_bottom' }}>
      <Stack.Screen name="Tabs" component={TabNavigator} />
      <Stack.Screen name="PosLab" component={MockPosScreen} />
      <Stack.Screen name="Keys" component={KeysScreen} />
      <Stack.Screen name="BackupQuiz" component={BackupQuizScreen} />
      <Stack.Screen name="SetPin" component={SetPinScreen} />
    </Stack.Navigator>
  );
}
