/** Monero / hybrid wallet product types (app ↔ `/v1/wallets`). */

export type WalletMode = 'managed' | 'unmanaged' | 'watch';

export type WalletSyncState = 'synced' | 'syncing' | 'pending' | 'error';

export type WalletSyncStatus = {
  height: number;
  target: number;
  state: WalletSyncState;
};

export type ServerWallet = {
  id: string;
  symbol: string;
  label: string;
  mode: WalletMode;
  address?: string;
  balance?: string;
  unlockedBalance?: string;
  restoreHeight?: number;
  sync?: WalletSyncStatus;
  viewKeyFingerprint?: string;
};

export type CreateWalletBody = {
  symbol: string;
  label?: string;
  mode: WalletMode;
  address?: string;
  /** Unmanaged only — sent once; never logged or stored in AsyncStorage. */
  viewKey?: string;
  restoreHeight?: number;
};

export type CreateWalletResult = {
  walletId: string;
  symbol: string;
  label: string;
  mode: WalletMode;
  address?: string;
  sync?: WalletSyncStatus;
  viewKeyFingerprint?: string;
};
