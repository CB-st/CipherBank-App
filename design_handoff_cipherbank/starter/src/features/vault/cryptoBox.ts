import { pbkdf2 } from '@noble/hashes/pbkdf2.js';
import { sha256 } from '@noble/hashes/sha2.js';
import { bytesToHex, hexToBytes, utf8ToBytes, randomBytes } from '@noble/hashes/utils.js';
import { gcm } from '@noble/ciphers/aes.js';

export type CustodyBlobV2 = {
  version: 2;
  ciphertext: string;
  iv: string;
  salt: string;
  /** AES key is always derived from the device secret + salt. */
  kdf: 'device';
};

function bytesToUtf8(b: Uint8Array): string {
  return new TextDecoder().decode(b);
}

export function deriveKeyFromDeviceSecret(deviceSecret: string, saltHex: string): Uint8Array {
  const salt = hexToBytes(saltHex);
  return pbkdf2(sha256, utf8ToBytes(deviceSecret), salt, { c: 80_000, dkLen: 32 });
}

export function encryptMnemonic(plaintext: string, key: Uint8Array): { ciphertext: string; iv: string } {
  const iv = randomBytes(12);
  const ct = gcm(key, iv).encrypt(utf8ToBytes(plaintext));
  return { ciphertext: bytesToHex(ct), iv: bytesToHex(iv) };
}

export function decryptMnemonic(blob: Pick<CustodyBlobV2, 'ciphertext' | 'iv'>, key: Uint8Array): string {
  const iv = hexToBytes(blob.iv);
  const ct = hexToBytes(blob.ciphertext);
  return bytesToUtf8(gcm(key, iv).decrypt(ct));
}

export function newSaltHex(): string {
  return bytesToHex(randomBytes(16));
}

export function newDeviceSecret(): string {
  return bytesToHex(randomBytes(32));
}

export function parseBlob(raw: string): CustodyBlobV2 | null {
  try {
    const j = JSON.parse(raw) as CustodyBlobV2;
    if (j.version !== 2 || !j.ciphertext || !j.iv || !j.salt) return null;
    return j;
  } catch {
    return null;
  }
}
