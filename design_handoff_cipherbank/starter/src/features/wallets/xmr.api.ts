import { api } from '@/lib/apiClient';
import type { CreateWalletBody, CreateWalletResult, ServerWallet, WalletSyncStatus } from './xmr.types';

export const listWallets = (symbol?: string) =>
  api.get<{ wallets: ServerWallet[] }>('/wallets' + (symbol ? `?symbol=${encodeURIComponent(symbol)}` : ''));

export const getWallet = (id: string) => api.get<ServerWallet>(`/wallets/${id}`);

export const createWallet = (body: CreateWalletBody) => api.post<CreateWalletResult>('/wallets', body);

export const refreshWallet = (id: string) =>
  api.post<{ id: string; sync: WalletSyncStatus }>(`/wallets/${id}/refresh`);

/** Short fingerprint for UI only — never reverse to the view key. */
export function fingerprintViewKey(viewKey: string): string {
  const cleaned = viewKey.trim().toLowerCase().replace(/\s+/g, '');
  if (cleaned.length < 8) return '••••';
  return cleaned.slice(0, 4) + '…' + cleaned.slice(-4);
}
