import { api } from '@/lib/apiClient';
export interface ReceiveInfo {
  handle: string;
  address: string;
  uri: string;
  qr?: string;
  amount?: string;
}
export const getReceive = (asset: string) => api.get<ReceiveInfo>('/receive/' + asset);
