import React from 'react';
import { createNativeStackNavigator } from '@react-navigation/native-stack';
import { TabNavigator } from './TabNavigator';
import { MockPosScreen } from '@/screens/pos/MockPosScreen';

const Stack = createNativeStackNavigator();

/** Tab shell + modal/stack labs (POS, etc.). */
export function MainStack() {
  return (
    <Stack.Navigator screenOptions={{ headerShown: false, animation: 'fade_from_bottom' }}>
      <Stack.Screen name="Tabs" component={TabNavigator} />
      <Stack.Screen name="PosLab" component={MockPosScreen} />
    </Stack.Navigator>
  );
}
