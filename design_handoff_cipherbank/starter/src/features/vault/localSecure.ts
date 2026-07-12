import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import AsyncStorage from '@react-native-async-storage/async-storage';

/**
 * Platform-safe secret storage.
 * iOS/Android: Keychain / Keystore via expo-secure-store.
 * Web: SecureStore when available; otherwise AsyncStorage (weaker — never log values).
 */
const WEB_PREFIX = 'cb_secure_web:';

async function webGet(key: string): Promise<string | null> {
  return AsyncStorage.getItem(WEB_PREFIX + key);
}
async function webSet(key: string, value: string): Promise<void> {
  await AsyncStorage.setItem(WEB_PREFIX + key, value);
}
async function webDel(key: string): Promise<void> {
  await AsyncStorage.removeItem(WEB_PREFIX + key);
}

export async function localSecureGet(key: string): Promise<string | null> {
  if (Platform.OS === 'web') {
    try {
      return (await SecureStore.getItemAsync(key)) ?? (await webGet(key));
    } catch {
      return webGet(key);
    }
  }
  try {
    return await SecureStore.getItemAsync(key);
  } catch {
    return null;
  }
}

export async function localSecureSet(key: string, value: string): Promise<void> {
  if (Platform.OS === 'web') {
    try {
      await SecureStore.setItemAsync(key, value);
      return;
    } catch {
      await webSet(key, value);
      return;
    }
  }
  await SecureStore.setItemAsync(key, value);
}

export async function localSecureDelete(key: string): Promise<void> {
  if (Platform.OS === 'web') {
    try {
      await SecureStore.deleteItemAsync(key);
    } catch {
      /* fall through */
    }
    await webDel(key);
    return;
  }
  try {
    await SecureStore.deleteItemAsync(key);
  } catch {
    /* ignore */
  }
}
