import { api, uuid } from '@/lib/apiClient';

export interface VaultBinary {
  id: string;
  label: string;
  kind: string;
  status: string;
  createdAt: number;
}

export interface CardToken {
  id: string;
  brand: string;
  last4: string;
  expMonth: number;
  expYear: number;
  processorToken: string;
  createdAt: number;
  hardwareTest?: boolean;
  label?: string;
}

export const listBinaries = () => api.get<{ binaries: VaultBinary[] }>('/vault/binaries');
export const registerBinary = (v: { label: string; kind?: string }) =>
  api.post<VaultBinary>('/vault/binaries', v, { idempotencyKey: uuid() });

export const listCards = () => api.get<{ cards: CardToken[] }>('/vault/cards');
export const addCardToken = (v: { brand: string; last4: string; expMonth: number; expYear: number }) =>
  api.post<CardToken>('/vault/cards', v, { idempotencyKey: uuid() });
export const removeCardToken = (id: string) => api.post<{ ok: boolean }>('/vault/cards/' + id + '/delete', {}, { idempotencyKey: uuid() });
