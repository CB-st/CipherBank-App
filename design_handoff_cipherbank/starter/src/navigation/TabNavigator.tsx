import React from 'react';
import { createBottomTabNavigator } from '@react-navigation/bottom-tabs';
import { TabBar } from '@/components/chrome/TabBar';
import { HomeScreen } from '@/screens/home/HomeScreen';
import { ConvertScreen } from '@/screens/convert/ConvertScreen';
import { PayScreen } from '@/screens/pay/PayScreen';
import { SendScreen } from '@/screens/send/SendScreen';
import { ReceiveScreen } from '@/screens/receive/ReceiveScreen';
import { ProfileScreen } from '@/screens/profile/ProfileScreen';

const Tab = createBottomTabNavigator();

/** The persistent shell. Renders on frame 1; each screen owns its own async data. */
export function TabNavigator() {
  return (
    <Tab.Navigator
      tabBar={(props) => <TabBar {...props} />}
      screenOptions={{ headerShown: false }}
    >
      <Tab.Screen name="Home" component={HomeScreen} />
      <Tab.Screen name="Convert" component={ConvertScreen} />
      <Tab.Screen name="Pay" component={PayScreen} />
      <Tab.Screen name="Send" component={SendScreen} />
      <Tab.Screen name="Receive" component={ReceiveScreen} />
      <Tab.Screen name="Profile" component={ProfileScreen} />
    </Tab.Navigator>
  );
}
