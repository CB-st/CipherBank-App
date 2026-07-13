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
import { isMockApi, isSeedDemo } from '@/lib/runtimeFlags';

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
/** True while LocalAuthentication / PIN gate is showing — OS UI flips AppState to inactive. */
let authInProgress = 0;

export function beginAuthGate(): void {
  authInProgress += 1;
}

export function endAuthGate(): void {
  authInProgress = Math.max(0, authInProgress - 1);
}

function ensureAppStateWatch() {
  if (appStateSub) return;
  // Lock only on true background. `inactive` fires for biometric / device-PIN sheets
  // and would clear the session mid-unlock.
  appStateSub = AppState.addEventListener('change', (state) => {
    if (state === 'background' && authInProgress === 0) lockLocalCustody();
  });
}

function touchSession(mnemonic: string) {
  ensureAppStateWatch();
  sessionMnemonic = mnemonic;
  sessionExpiresAt = Date.now() + SESSION_TTL_MS;
}

export type UnlockOpts = {
  pin?: string;
  /** Skip biometric prompt (e.g. after PIN entry screen). */
  skipBiometrics?: boolean;
  /** Always re-prompt even if a session is live (payments, key export, POS present). */
  force?: boolean;
  /** Shown in the OS biometric dialog. */
  promptMessage?: string;
};

type LockListener = () => void;
const lockListeners = new Set<LockListener>();

/** Session / UI layers subscribe so app shell locks when custody clears. */
export function subscribeCustodyLock(listener: LockListener): () => void {
  lockListeners.add(listener);
  return () => lockListeners.delete(listener);
}

function notifyLocked() {
  lockListeners.forEach((l) => {
    try {
      l();
    } catch {
      /* ignore */
    }
  });
}

export function lockLocalCustody(): void {
  sessionMnemonic = null;
  sessionExpiresAt = 0;
  notifyLocked();
}

export function getSessionMnemonic(): string | null {
  if (!sessionMnemonic) return null;
  if (Date.now() > sessionExpiresAt) {
    lockLocalCustody();
    return null;
  }
  return sessionMnemonic;
}

async function authenticateDeviceOwner(promptMessage?: string): Promise<boolean> {
  if (Platform.OS === 'web') return false;
  beginAuthGate();
  try {
    // SECRET = device PIN/pattern/password; BIOMETRIC_* = fingerprint/face.
    // Do not require biometric enrollment — Android's BiometricPrompt with
    // DEVICE_CREDENTIAL shows the system keypad when fingerprint isn't available.
    const level = await LocalAuthentication.getEnrolledLevelAsync();
    if (level < LocalAuthentication.SecurityLevel.SECRET) return false;

    const res = await LocalAuthentication.authenticateAsync({
      promptMessage: promptMessage ?? 'Unlock CipherBank',
      cancelLabel: 'Cancel',
      disableDeviceFallback: false,
      fallbackLabel: 'Use device passcode',
      requireConfirmation: false,
      biometricsSecurityLevel: 'strong',
    });
    return res.success;
  } catch {
    return false;
  } finally {
    endAuthGate();
  }
}

/** True when the OS can show fingerprint and/or the built-in device PIN/pattern UI. */
export async function canUseDeviceOwnerAuth(): Promise<boolean> {
  if (Platform.OS === 'web') return false;
  try {
    const level = await LocalAuthentication.getEnrolledLevelAsync();
    return level >= LocalAuthentication.SecurityLevel.SECRET;
  } catch {
    return false;
  }
}

/**
 * Unlock: OS fingerprint / device PIN first, else CipherBank app PIN.
 * Decrypts into a short-lived session. Returns false if cancelled / wrong PIN.
 */
export async function unlockLocalCustody(opts: UnlockOpts = {}): Promise<boolean> {
  if (opts.force) {
    // Clear without notifying app-shell lock (we're mid step-up).
    sessionMnemonic = null;
    sessionExpiresAt = 0;
  } else {
    const existing = getSessionMnemonic();
    if (existing) return true;
  }

  await migrateLegacyIfNeeded();
  if (!(await hasLocalCustody())) return false;

  let gateOk = false;
  const prompt = opts.promptMessage ?? 'Unlock CipherBank';

  if (!opts.skipBiometrics) {
    const osOk = await authenticateDeviceOwner(prompt);
    if (osOk) gateOk = true;
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
    } else if (Platform.OS === 'web' || isMockApi()) {
      const pinSet = await hasPin();
      if (pinSet && !opts.pin) return false;
      gateOk = !pinSet;
    } else {
      const pinSet = await hasPin();
      if (!pinSet) {
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

/** Force OS/biometric (or PIN) re-auth before exposing the phrase. */
export async function exportMnemonic(): Promise<string | null> {
  const ok = await unlockLocalCustody({
    force: true,
    promptMessage: 'Unlock to view recovery phrase',
  });
  if (!ok) return null;
  return getSessionMnemonic();
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
  } catch (e) {
    console.warn('[custody] decryptMnemonic failed', e);
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

/**
 * Mock / emulator bootstrap: heal incomplete first-run state.
 * Only runs when `isSeedDemo()` (lab). Clean installs never auto-create custody.
 */
export async function ensureDemoCustody(): Promise<void> {
  if (!isSeedDemo()) return;

  try {
    await migrateLegacyIfNeeded();
  } catch {
    /* recreate below */
  }

  const existing = await readAndDecrypt();
  lockLocalCustody();
  if (existing && (await hasPin())) {
    return;
  }

  // Incomplete first-run / Hermes polyfill failures left flag-only or undecryptable vaults.
  await clearLocalCustody();
  await createLocalCustody({ pin: '000000' });
  lockLocalCustody();

  if (!(await hasPin())) {
    throw new Error('Demo custody failed to persist PIN');
  }
  const raw = await localSecureGet(BLOB_KEY);
  const secret = await localSecureGet(DEVICE_SECRET_KEY);
  if (!raw || !secret) {
    throw new Error(`Demo custody missing sealed material (blob=${!!raw} secret=${!!secret})`);
  }
  const mnemonic = await readAndDecrypt();
  lockLocalCustody();
  if (!mnemonic) {
    throw new Error('Demo custody decrypt returned empty');
  }
}
