/**
 * Modular light-wallet registry.
 * Each currency the user enables locally plugs in derive / server-register / UI modes.
 * See AGENTS.md § Modular light wallets.
 */
import type { WalletMode, WalletSource } from '@/features/portfolio/portfolio.types';
import { isDerivableSymbol } from './derive';

export type WalletUiMode = 'derive' | WalletMode;

export type WalletModule = {
  symbol: string;
  /** Modes shown in Add-wallet for this asset. */
  addModes: WalletUiMode[];
  canDerive: boolean;
  /** Maps UI mode → portfolio source after create. */
  sourceFor: (mode: WalletUiMode) => WalletSource;
  /** Uses CipherBank `/wallets` product API (e.g. XMR). */
  usesServerWallets: boolean;
  notes?: string;
};

const MODULES: Record<string, WalletModule> = {
  BTC: {
    symbol: 'BTC',
    addModes: ['derive', 'watch'],
    canDerive: true,
    sourceFor: (m) => (m === 'watch' ? 'watch' : 'local'),
    usesServerWallets: false,
    notes: 'BIP84 native segwit from on-device BIP39',
  },
  ETH: {
    symbol: 'ETH',
    addModes: ['derive', 'watch'],
    canDerive: true,
    sourceFor: (m) => (m === 'watch' ? 'watch' : 'local'),
    usesServerWallets: false,
    notes: 'BIP44 m/44\'/60\'/0\'/0/i from on-device BIP39',
  },
  XMR: {
    symbol: 'XMR',
    addModes: ['managed', 'unmanaged', 'watch'],
    canDerive: false,
    sourceFor: (m) => (m === 'managed' ? 'server' : m === 'watch' ? 'watch' : 'local'),
    usesServerWallets: true,
    notes: 'Hybrid: managed wallet-rpc or unmanaged view-key sync — see docs/MONERO_LINK.md',
  },
  LTC: {
    symbol: 'LTC',
    addModes: ['derive', 'watch'],
    canDerive: true,
    sourceFor: (m) => (m === 'watch' ? 'watch' : 'local'),
    usesServerWallets: false,
    notes: 'BIP84 native segwit m/84\'/2\'/0\'/0/i from on-device BIP39',
  },
  DOGE: {
    symbol: 'DOGE',
    addModes: ['derive', 'watch'],
    canDerive: true,
    sourceFor: (m) => (m === 'watch' ? 'watch' : 'local'),
    usesServerWallets: false,
    notes: 'BIP44 m/44\'/3\'/0\'/0/i P2PKH from on-device BIP39',
  },
};

/** Default for coins not yet registered: watch-only until a module is added. */
function fallbackModule(symbol: string): WalletModule {
  const sym = symbol.toUpperCase();
  return {
    symbol: sym,
    addModes: ['watch'],
    canDerive: isDerivableSymbol(sym),
    sourceFor: () => 'watch',
    usesServerWallets: false,
    notes: 'No dedicated light-wallet module yet — watch address only',
  };
}

export function getWalletModule(symbol: string): WalletModule {
  const sym = symbol.toUpperCase();
  return MODULES[sym] ?? fallbackModule(sym);
}

export function listWalletModules(): WalletModule[] {
  return Object.values(MODULES);
}

/** Register or replace a module when enabling a currency on-device. */
export function registerWalletModule(mod: WalletModule): void {
  MODULES[mod.symbol.toUpperCase()] = mod;
}
