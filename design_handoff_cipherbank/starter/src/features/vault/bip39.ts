import { generateMnemonic as scureGenerate, validateMnemonic as scureValidate, mnemonicToSeedSync } from '@scure/bip39';
import { wordlist } from '@scure/bip39/wordlists/english.js';

/** 128-bit entropy → 12-word BIP39 phrase. Never log the result. */
export function generateMnemonic(): string {
  return scureGenerate(wordlist, 128);
}

export function validateMnemonic(phrase: string): boolean {
  return scureValidate(normalizeMnemonic(phrase), wordlist);
}

export function mnemonicToSeed(phrase: string): Uint8Array {
  return mnemonicToSeedSync(normalizeMnemonic(phrase));
}

export function mnemonicWords(phrase: string): string[] {
  return normalizeMnemonic(phrase).split(' ').filter(Boolean);
}

export function normalizeMnemonic(phrase: string): string {
  return phrase.trim().toLowerCase().replace(/\s+/g, ' ');
}
