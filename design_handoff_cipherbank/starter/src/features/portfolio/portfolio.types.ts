export type AssetType = 'crypto' | 'fiat' | 'security';

/** Custody origin for a wallet account — prepares for local read / mnemonic-derived addresses. */
export type WalletSource = 'local' | 'watch' | 'server';

/**
 * One spendable / receivable account under an asset.
 * Local wallets will later derive addresses from the on-device mnemonic;
 * watch wallets are read-only imported addresses.
 */
export interface WalletAccount {
  id: string;
  label: string;
  amount: string;
  usdValue: number;
  /** Receive address when known (local derivation or watch import). */
  address?: string;
  derivationPath?: string;
  source: WalletSource;
}

export interface Holding {
  symbol: string;
  name: string;
  glyph: string;
  type: AssetType;
  /** Aggregate across wallets */
  amount: string;
  usdValue: number;
  change24h: number;
  note?: string;
  /** Per-asset wallets. Crypto holdings should include at least one. */
  wallets?: WalletAccount[];
}

export interface Portfolio {
  total: number;
  change24h: { amount: number; pct: number };
  holdings: Holding[];
}

/** Client-side draft for adding a wallet before chain read is live. */
export interface LocalWalletDraft {
  id: string;
  symbol: string;
  label: string;
  address?: string;
  source: WalletSource;
  createdAt: number;
}
