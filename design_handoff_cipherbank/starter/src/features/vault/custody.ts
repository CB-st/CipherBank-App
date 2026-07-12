import { AppState, Platform } from 'react-native';
import * as LocalAuthentication from 'expo-local-authentication';
import { generateMnemonic, validateMnemonic, normalizeMnemonic } from './bip39';
import {
  decryptMnemonic,
  encryptMnemonic,
  deriveKeyFromDeviceSecret,
  newDeviceSecret,
  newSaltHex,
  parseBlob,
  type CustodyBlobV2,
} from './cryptoBox';
import { clearPin, hasPin, setPin, verifyPin } from './pinStore';
import { localSecureDelete, localSecureGet, localSecureSet } from './localSecure';

const BLOB_KEY = 'cb_custody_v2';
const LEGACY_MNEMONIC_KEY = 'cb_custody_mnemonic';
const DEVICE_SECRET_KEY = 'cb_device_secret_v2';
const DEVICE_KEY_LEGACY = 'cb_device_signing_key';
const WALLET_FLAG = 'cb_wallet_present';
const SESSION_TTL_MS = 5 * 60 * 1000;

/** In-memory unlock window — never persisted. Cleared on background / TTL / lock. */
let sessionMnemonic: string | null = null;
let sessionExpiresAt = 0;

/** Staging phrase during onboarding (Keys → Quiz → PIN) before seal. */
let pendingMnemonic: string | null = null;

let appStateSub: { remove: () => void } | null = null;

function ensureAppStateWatch() {
  if (appStateSub) return;
  appStateSub = AppState.addEventListener('change', (state) => {
    if (state !== 'active') lockLocalCustody();
  });
}

function touchSession(mnemonic: string) {
  ensureAppStateWatch();
  sessionMnemonic = mnemonic;
  sessionExpiresAt = Date.now() + SESSION_TTL_MS;
}

export function lockLocalCustody(): void {
  sessionMnemonic = null;
  sessionExpiresAt = 0;
}

export function getSessionMnemonic(): string | null {
  if (!sessionMnemonic) return null;
  if (Date.now() > sessionExpiresAt) {
    lockLocalCustody();
    return null;
  }
  return sessionMnemonic;
}

export function setPendingMnemonic(phrase: string): void {
  pendingMnemonic = normalizeMnemonic(phrase);
}

export function getPendingMnemonic(): string | null {
  return pendingMnemonic;
}

export function clearPendingMnemonic(): void {
  pendingMnemonic = null;
}

export async function hasLocalCustody(): Promise<boolean> {
  const flag = await localSecureGet(WALLET_FLAG);
  if (flag !== '1') return false;
  const raw = await localSecureGet(BLOB_KEY);
  if (raw && parseBlob(raw)) return true;
  const legacy = await localSecureGet(LEGACY_MNEMONIC_KEY);
  return !!legacy;
}

async function migrateLegacyIfNeeded(): Promise<void> {
  const existing = await localSecureGet(BLOB_KEY);
  if (existing && parseBlob(existing)) {
    await localSecureDelete(LEGACY_MNEMONIC_KEY);
    return;
  }
  const legacy = await localSecureGet(LEGACY_MNEMONIC_KEY);
  if (!legacy || !validateMnemonic(legacy)) return;
  // Re-seal under v2 with a fresh device secret (no PIN yet — caller should setPin).
  const deviceSecret = newDeviceSecret();
  const salt = newSaltHex();
  const key = deriveKeyFromDeviceSecret(deviceSecret, salt);
  const { ciphertext, iv } = encryptMnemonic(normalizeMnemonic(legacy), key);
  const blob: CustodyBlobV2 = { version: 2, ciphertext, iv, salt, kdf: 'device' };
  await localSecureSet(DEVICE_SECRET_KEY, deviceSecret);
  await localSecureSet(BLOB_KEY, JSON.stringify(blob));
  await localSecureDelete(LEGACY_MNEMONIC_KEY);
  await localSecureSet(WALLET_FLAG, '1');
}

async function readAndDecrypt(): Promise<string | null> {
  await migrateLegacyIfNeeded();
  const raw = await localSecureGet(BLOB_KEY);
  const blob = raw ? parseBlob(raw) : null;
  if (!blob) return null;
  const deviceSecret = await localSecureGet(DEVICE_SECRET_KEY);
  if (!deviceSecret) return null;
  try {
    return decryptMnemonic(blob, deriveKeyFromDeviceSecret(deviceSecret, blob.salt));
  } catch {
    return null;
  }
}

export type CreateCustodyOpts = {
  mnemonic?: string;
  pin?: string;
};

/**
 * Generate (or accept) a mnemonic, AES-GCM seal into SecureStore, optional PIN hash.
 * Opens a short-lived in-memory session. Never log the returned mnemonic.
 */
export async function createLocalCustody(opts: CreateCustodyOpts = {}): Promise<{ mnemonic: string }> {
  const mnemonic = normalizeMnemonic(opts.mnemonic ?? generateMnemonic());
  if (!validateMnemonic(mnemonic)) throw new Error('Invalid mnemonic');

  const deviceSecret = newDeviceSecret();
  const salt = newSaltHex();
  const key = deriveKeyFromDeviceSecret(deviceSecret, salt);
  const { ciphertext, iv } = encryptMnemonic(mnemonic, key);
  const blob: CustodyBlobV2 = { version: 2, ciphertext, iv, salt, kdf: 'device' };

  await localSecureSet(DEVICE_SECRET_KEY, deviceSecret);
  await localSecureSet(BLOB_KEY, JSON.stringify(blob));
  await localSecureSet(WALLET_FLAG, '1');
  await localSecureDelete(LEGACY_MNEMONIC_KEY);
  await localSecureDelete(DEVICE_KEY_LEGACY);

  if (opts.pin) await setPin(opts.pin);

  touchSession(mnemonic);
  clearPendingMnemonic();
  return { mnemonic };
}

/** Seal the pending onboarding mnemonic with a PIN (Keys → Quiz → SetPin). */
export async function sealPendingCustody(pin: string): Promise<void> {
  const mnemonic = getPendingMnemonic();
  if (!mnemonic) throw new Error('No pending mnemonic');
  await createLocalCustody({ mnemonic, pin });
}

async function authenticateBiometrics(): Promise<boolean> {
  if (Platform.OS === 'web') return false;
  try {
    const hasHardware = await LocalAuthentication.hasHardwareAsync();
    const enrolled = await LocalAuthentication.isEnrolledAsync();
    if (!hasHardware || !enrolled) return false;
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

export type UnlockOpts = {
  pin?: string;
  /** Skip biometric prompt (e.g. after PIN entry screen). */
  skipBiometrics?: boolean;
};

/**
 * Unlock: biometrics first (when enrolled), else PIN.
 * Decrypts into a short-lived session. Returns false if cancelled / wrong PIN.
 */
export async function unlockLocalCustody(opts: UnlockOpts = {}): Promise<boolean> {
  const existing = getSessionMnemonic();
  if (existing) return true;

  await migrateLegacyIfNeeded();
  if (!(await hasLocalCustody())) return false;

  let gateOk = false;

  if (!opts.skipBiometrics) {
    const bio = await authenticateBiometrics();
    if (bio) gateOk = true;
  }

  if (!gateOk) {
    if (opts.pin) {
      const pinSet = await hasPin();
      if (!pinSet) {
        gateOk = true;
      } else {
        const v = await verifyPin(opts.pin);
        if (!v.ok) return false;
        gateOk = true;
      }
    } else if (Platform.OS === 'web' || process.env.EXPO_PUBLIC_USE_MOCK === 'true') {
      // Web / mock: allow unlock without PIN when no bio — still requires custody present.
      // If a PIN is set, require it (caller must pass pin).
      const pinSet = await hasPin();
      if (pinSet && !opts.pin) return false;
      gateOk = !pinSet;
    } else {
      // Native without bio: need PIN
      const pinSet = await hasPin();
      if (!pinSet) {
        // No PIN and no bio — still allow decrypt (device-bound SecureStore only)
        gateOk = true;
      } else {
        return false;
      }
    }
  }

  if (!gateOk) return false;

  const mnemonic = await readAndDecrypt();
  if (!mnemonic) return false;
  touchSession(mnemonic);
  return true;
}

export async function exportMnemonic(): Promise<string | null> {
  const session = getSessionMnemonic();
  if (session) return session;
  const ok = await unlockLocalCustody();
  if (!ok) return null;
  return getSessionMnemonic();
}

export async function clearLocalCustody(): Promise<void> {
  lockLocalCustody();
  clearPendingMnemonic();
  await localSecureDelete(BLOB_KEY);
  await localSecureDelete(LEGACY_MNEMONIC_KEY);
  await localSecureDelete(DEVICE_SECRET_KEY);
  await localSecureDelete(DEVICE_KEY_LEGACY);
  await localSecureDelete(WALLET_FLAG);
  await clearPin();
}
