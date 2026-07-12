import { api } from '@/lib/apiClient';

export interface Quote {
  quoteId: string;
  rate: number;
  expiresAt: number;
  from: string;
  to: string;
  amountOut?: string;
  fee?: string;
}

export const requestQuote = (from: string, to: string, amount: string) =>
  api.post<Quote>('/quotes', { from, to, amount });
