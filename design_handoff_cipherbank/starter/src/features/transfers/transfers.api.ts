import { api, uuid } from '@/lib/apiClient';

export type Speed = 'instant' | 'ach';

export const sendTransfer = (v: { recipient: string; amount: string; source: string; speed: Speed }) =>
  api.post('/transfers', v, { idempotencyKey: uuid() });

export const payWithMix = (v: { recipient: string; total: string; sources: { asset: string; value: string }[] }) =>
  api.post('/payments', v, { idempotencyKey: uuid() });
