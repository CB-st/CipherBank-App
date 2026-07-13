import { bech32 } from '@scure/base';
import { isDerivableSymbol } from './derive';

export type AddressCheck = { ok: boolean; reason?: string };

/** Lightweight client-side checks before saving a watch address. */
export function validateWatchAddress(symbol: string, address: string): AddressCheck {
  const sym = symbol.toUpperCase();
  const addr = address.trim();
  if (!addr) return { ok: false, reason: 'Address required' };

  switch (sym) {
    case 'BTC':
      return checkBech32(addr, ['bc']);
    case 'LTC':
      return checkBech32(addr, ['ltc']);
    case 'ETH':
      if (!/^0x[0-9a-fA-F]{40}$/.test(addr)) return { ok: false, reason: 'Need 0x + 40 hex chars' };
      return { ok: true };
    case 'DOGE':
      if (!/^D[1-9A-HJ-NP-Za-km-z]{25,34}$/.test(addr)) {
        return { ok: false, reason: 'Dogecoin P2PKH addresses start with D' };
      }
      return { ok: true };
    case 'XMR': {
      // Standard (95) or integrated (106) base58 — loose length check
      if (addr.length < 90 || addr.length > 110) {
        return { ok: false, reason: 'Monero address length looks wrong' };
      }
      return { ok: true };
    }
    default:
      if (addr.length < 8) return { ok: false, reason: 'Address too short' };
      return { ok: true };
  }
}

function checkBech32(addr: string, hrpAllowed: string[]): AddressCheck {
  try {
    const lower = addr.toLowerCase();
    const decoded = bech32.decode(lower as `${string}1${string}`);
    if (!hrpAllowed.includes(decoded.prefix)) {
      return { ok: false, reason: `Expected ${hrpAllowed.join('/')} address` };
    }
    return { ok: true };
  } catch {
    return { ok: false, reason: 'Invalid bech32 address' };
  }
}

export function canGenerateAddress(symbol: string): boolean {
  return isDerivableSymbol(symbol);
}
