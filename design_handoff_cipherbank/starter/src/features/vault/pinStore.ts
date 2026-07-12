/**
 * PIN verification: salted SHA-256 hash only — never store plaintext PIN.
 * Lockout after 5 failures with exponential backoff.
 */
import { sha256 } from '@noble/hashes/sha2.js';
import { bytesToHex, hexToBytes, utf8ToBytes, randomBytes } from '@noble/hashes/utils.js';
import { localSecureDelete, localSecureGet, localSecureSet } from './localSecure';

const PIN_META_KEY = 'cb_pin_meta_v1';
const MAX_ATTEMPTS = 5;
const BASE_LOCK_MS = 30_000;

export type PinMeta = {
  salt: string;
  hash: string;
  failures: number;
  lockedUntil: number;
};

function hashPin(pin: string, saltHex: string): string {
  const salt = hexToBytes(saltHex);
  const digest = sha256(new Uint8Array([...utf8ToBytes(pin), ...salt]));
  return bytesToHex(digest);
}

async function loadMeta(): Promise<PinMeta | null> {
  const raw = await localSecureGet(PIN_META_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as PinMeta;
  } catch {
    return null;
  }
}

async function saveMeta(meta: PinMeta): Promise<void> {
  await localSecureSet(PIN_META_KEY, JSON.stringify(meta));
}

export async function hasPin(): Promise<boolean> {
  const m = await loadMeta();
  return !!(m?.hash && m?.salt);
}

export async function setPin(pin: string): Promise<void> {
  if (!/^\d{6}$/.test(pin)) throw new Error('PIN must be 6 digits');
  const salt = bytesToHex(randomBytes(16));
  await saveMeta({
    salt,
    hash: hashPin(pin, salt),
    failures: 0,
    lockedUntil: 0,
  });
}

export type PinVerifyResult =
  | { ok: true }
  | { ok: false; reason: 'missing' | 'locked' | 'mismatch'; lockedUntil?: number; remaining?: number };

export async function verifyPin(pin: string): Promise<PinVerifyResult> {
  const meta = await loadMeta();
  if (!meta) return { ok: false, reason: 'missing' };
  const now = Date.now();
  if (meta.lockedUntil > now) {
    return { ok: false, reason: 'locked', lockedUntil: meta.lockedUntil };
  }
  if (hashPin(pin, meta.salt) === meta.hash) {
    await saveMeta({ ...meta, failures: 0, lockedUntil: 0 });
    return { ok: true };
  }
  const failures = meta.failures + 1;
  let lockedUntil = 0;
  if (failures >= MAX_ATTEMPTS) {
    const exp = Math.min(failures - MAX_ATTEMPTS + 1, 6);
    lockedUntil = now + BASE_LOCK_MS * Math.pow(2, exp - 1);
  }
  await saveMeta({ ...meta, failures, lockedUntil });
  if (lockedUntil) return { ok: false, reason: 'locked', lockedUntil };
  return { ok: false, reason: 'mismatch', remaining: MAX_ATTEMPTS - failures };
}

export async function clearPin(): Promise<void> {
  await localSecureDelete(PIN_META_KEY);
}

export async function pinLockRemainingMs(): Promise<number> {
  const meta = await loadMeta();
  if (!meta?.lockedUntil) return 0;
  return Math.max(0, meta.lockedUntil - Date.now());
}
