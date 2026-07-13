import { HDKey } from '@scure/bip32';
import { bech32, base58check } from '@scure/base';
import { sha256 } from '@noble/hashes/sha2.js';
import { ripemd160 } from '@noble/hashes/legacy.js';
import { keccak_256 } from '@noble/hashes/sha3.js';
import { bytesToHex } from '@noble/hashes/utils.js';
import { getPublicKey } from '@noble/secp256k1';
import { mnemonicToSeed } from '@/features/vault/bip39';

export type DerivedAddress = {
  address: string;
  path: string;
  accountIndex: number;
};

const doge58 = base58check(sha256);

function rootFromMnemonic(mnemonic: string): HDKey {
  return HDKey.fromMasterSeed(mnemonicToSeed(mnemonic));
}

/** BIP84 native segwit: m/84'/0'/0'/0/{i} → bc1… */
export function deriveBtcAddress(mnemonic: string, accountIndex = 0): DerivedAddress {
  const path = `m/84'/0'/0'/0/${accountIndex}`;
  const child = rootFromMnemonic(mnemonic).derive(path);
  if (!child.publicKey) throw new Error('BTC derive failed');
  const hash = ripemd160(sha256(child.publicKey));
  const address = bech32.encode('bc', [0, ...bech32.toWords(hash)]);
  return { address, path, accountIndex };
}

/** BIP84 Litecoin: m/84'/2'/0'/0/{i} → ltc1… */
export function deriveLtcAddress(mnemonic: string, accountIndex = 0): DerivedAddress {
  const path = `m/84'/2'/0'/0/${accountIndex}`;
  const child = rootFromMnemonic(mnemonic).derive(path);
  if (!child.publicKey) throw new Error('LTC derive failed');
  const hash = ripemd160(sha256(child.publicKey));
  const address = bech32.encode('ltc', [0, ...bech32.toWords(hash)]);
  return { address, path, accountIndex };
}

/** BIP44 Dogecoin: m/44'/3'/0'/0/{i} → P2PKH D… */
export function deriveDogeAddress(mnemonic: string, accountIndex = 0): DerivedAddress {
  const path = `m/44'/3'/0'/0/${accountIndex}`;
  const child = rootFromMnemonic(mnemonic).derive(path);
  if (!child.publicKey) throw new Error('DOGE derive failed');
  const hash = ripemd160(sha256(child.publicKey));
  const payload = new Uint8Array(1 + hash.length);
  payload[0] = 30; // mainnet P2PKH version
  payload.set(hash, 1);
  const address = doge58.encode(payload);
  return { address, path, accountIndex };
}

function ethChecksumAddress(addrHex: string): string {
  const hex = addrHex.toLowerCase().replace(/^0x/, '');
  const hash = bytesToHex(keccak_256(new TextEncoder().encode(hex)));
  let out = '0x';
  for (let i = 0; i < hex.length; i++) {
    out += parseInt(hash[i], 16) >= 8 ? hex[i].toUpperCase() : hex[i];
  }
  return out;
}

/** BIP44 Ethereum: m/44'/60'/0'/0/{i} → checksummed 0x… */
export function deriveEthAddress(mnemonic: string, accountIndex = 0): DerivedAddress {
  const path = `m/44'/60'/0'/0/${accountIndex}`;
  const child = rootFromMnemonic(mnemonic).derive(path);
  if (!child.privateKey) throw new Error('ETH derive failed');
  const pub = getPublicKey(child.privateKey, false);
  const addrHex = bytesToHex(keccak_256(pub.slice(1)).slice(-20));
  return { address: ethChecksumAddress(addrHex), path, accountIndex };
}

export function deriveAddress(symbol: string, mnemonic: string, accountIndex = 0): DerivedAddress | null {
  const s = symbol.toUpperCase();
  if (s === 'BTC') return deriveBtcAddress(mnemonic, accountIndex);
  if (s === 'ETH') return deriveEthAddress(mnemonic, accountIndex);
  if (s === 'LTC') return deriveLtcAddress(mnemonic, accountIndex);
  if (s === 'DOGE') return deriveDogeAddress(mnemonic, accountIndex);
  return null;
}

export function isDerivableSymbol(symbol: string): boolean {
  const s = symbol.toUpperCase();
  return s === 'BTC' || s === 'ETH' || s === 'LTC' || s === 'DOGE';
}
