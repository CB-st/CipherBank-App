import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import AsyncStorage from '@react-native-async-storage/async-storage';

/**
 * Platform-safe secret storage.
 * iOS/Android: Keychain / Keystore via expo-secure-store (WHEN_UNLOCKED_THIS_DEVICE_ONLY).
 * Web: SecureStore when available; otherwise AsyncStorage — weaker, never for production seed.
 */
const WEB_PREFIX = 'cb_secure_web:';

const NATIVE_OPTIONS: SecureStore.SecureStoreOptions = {
  keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY,
};

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
    return await SecureStore.getItemAsync(key, NATIVE_OPTIONS);
  } catch {
    try {
      return await SecureStore.getItemAsync(key);
    } catch {
      return null;
    }
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
  try {
    await SecureStore.setItemAsync(key, value, NATIVE_OPTIONS);
  } catch {
    await SecureStore.setItemAsync(key, value);
  }
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
    await SecureStore.deleteItemAsync(key, NATIVE_OPTIONS);
  } catch {
    try {
      await SecureStore.deleteItemAsync(key);
    } catch {
      /* ignore */
    }
  }
}
