import { Platform } from 'react-native';
import * as LocalAuthentication from 'expo-local-authentication';
import { localSecureDelete, localSecureGet, localSecureSet } from './localSecure';

const MNEMONIC_KEY = 'cb_custody_mnemonic';
const DEVICE_KEY = 'cb_device_signing_key';
const WALLET_FLAG = 'cb_wallet_present';

/** Mock BIP39-shaped phrase for UI / mock mode. Replace with real entropy later. */
const MOCK_PHRASE = [
  'cipher', 'ledger', 'violet', 'anchor', 'orbit', 'quartz',
  'harbor', 'nebula', 'velvet', 'prism', 'ember', 'signal',
].join(' ');

export async function hasLocalCustody(): Promise<boolean> {
  const flag = await localSecureGet(WALLET_FLAG);
  const mnemonic = await localSecureGet(MNEMONIC_KEY);
  return flag === '1' && !!mnemonic;
}

export async function createLocalCustody(): Promise<{ mnemonic: string }> {
  const mnemonic = process.env.EXPO_PUBLIC_USE_MOCK === 'true' ? MOCK_PHRASE : MOCK_PHRASE;
  await localSecureSet(MNEMONIC_KEY, mnemonic);
  await localSecureSet(DEVICE_KEY, 'devkey_' + Date.now().toString(36));
  await localSecureSet(WALLET_FLAG, '1');
  return { mnemonic };
}

/** Gate local secret reveal. Native: biometrics. Web: always allows (PIN stub later). */
export async function unlockLocalCustody(): Promise<boolean> {
  if (Platform.OS === 'web') return true;
  try {
    const hasHardware = await LocalAuthentication.hasHardwareAsync();
    const enrolled = await LocalAuthentication.isEnrolledAsync();
    if (!hasHardware || !enrolled) return true;
    const res = await LocalAuthentication.authenticateAsync({
      promptMessage: 'Unlock CipherBank custody',
      cancelLabel: 'Cancel',
      disableDeviceFallback: false,
    });
    return res.success;
  } catch {
    return false;
  }
}

export async function exportMnemonic(): Promise<string | null> {
  const ok = await unlockLocalCustody();
  if (!ok) return null;
  return localSecureGet(MNEMONIC_KEY);
}

export async function clearLocalCustody(): Promise<void> {
  await localSecureDelete(MNEMONIC_KEY);
  await localSecureDelete(DEVICE_KEY);
  await localSecureDelete(WALLET_FLAG);
}
