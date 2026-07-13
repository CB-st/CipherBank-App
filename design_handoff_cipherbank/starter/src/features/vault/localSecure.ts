import { Platform } from 'react-native';
import * as SecureStore from 'expo-secure-store';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { isMockApi, isSeedDemo } from '@/lib/runtimeFlags';

/**
 * Platform-safe secret storage.
 * Mock/emulator: AsyncStorage only (SecureStore round-trips are unreliable on AVD
 * and caused empty vault / Incorrect PIN after seal).
 * Production native: SecureStore with AsyncStorage mirror for resilience.
 * AsyncStorage path is not production-grade for seed material.
 */
const FALLBACK_PREFIX = 'cb_secure_fallback:';
const MOCK = isMockApi() || isSeedDemo();
const NATIVE_OPTIONS: SecureStore.SecureStoreOptions = {
  keychainAccessible: SecureStore.WHEN_UNLOCKED_THIS_DEVICE_ONLY,
};

async function fallbackGet(key: string): Promise<string | null> {
  return AsyncStorage.getItem(FALLBACK_PREFIX + key);
}
async function fallbackSet(key: string, value: string): Promise<void> {
  await AsyncStorage.setItem(FALLBACK_PREFIX + key, value);
}
async function fallbackDel(key: string): Promise<void> {
  await AsyncStorage.removeItem(FALLBACK_PREFIX + key);
}

async function secureGet(key: string): Promise<string | null> {
  try {
    const v = await SecureStore.getItemAsync(key, Platform.OS === 'ios' ? NATIVE_OPTIONS : undefined);
    if (v != null && v !== '') return v;
  } catch {
    /* try without options */
  }
  try {
    const v = await SecureStore.getItemAsync(key);
    if (v != null && v !== '') return v;
  } catch {
    /* ignore */
  }
  return null;
}

async function secureSet(key: string, value: string): Promise<boolean> {
  try {
    await SecureStore.setItemAsync(key, value, Platform.OS === 'ios' ? NATIVE_OPTIONS : undefined);
    return true;
  } catch {
    try {
      await SecureStore.setItemAsync(key, value);
      return true;
    } catch {
      return false;
    }
  }
}

async function secureDel(key: string): Promise<void> {
  try {
    await SecureStore.deleteItemAsync(key, Platform.OS === 'ios' ? NATIVE_OPTIONS : undefined);
  } catch {
    try {
      await SecureStore.deleteItemAsync(key);
    } catch {
      /* ignore */
    }
  }
}

export async function localSecureGet(key: string): Promise<string | null> {
  if (MOCK) return fallbackGet(key);
  return (await secureGet(key)) ?? fallbackGet(key);
}

export async function localSecureSet(key: string, value: string): Promise<void> {
  if (MOCK) {
    await fallbackSet(key, value);
    return;
  }
  const wroteSecure = await secureSet(key, value);
  try {
    await fallbackSet(key, value);
  } catch (e) {
    if (!wroteSecure) throw e;
  }
}

export async function localSecureDelete(key: string): Promise<void> {
  if (!MOCK) await secureDel(key);
  await fallbackDel(key);
}
