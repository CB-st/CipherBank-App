import { queryClient } from '@/lib/queryClient';
import type { Portfolio } from '@/features/portfolio/portfolio.types';
import portfolioFixture from './fixtures/portfolio.json';

type StreamMsg =
  | { type: 'balance.update'; payload: Portfolio }
  | { type: 'rate.tick'; payload: { pair: string; rate: number; ts: number } }
  | { type: 'convert.settled'; payload: { txId: string; amountOut: string } }
  | { type: 'transfer.settled'; payload: { txId: string; arrivedAt: number } }
  | { type: 'payment.settled'; payload: { paymentId: string; breakdown: { asset: string; value: string }[] } };

type Listener = (msg: StreamMsg) => void;

const listeners = new Set<Listener>();
let connected = false;
let rateTimer: ReturnType<typeof setInterval> | null = null;

function emit(msg: StreamMsg) {
  listeners.forEach((l) => l(msg));
  switch (msg.type) {
    case 'balance.update':
      queryClient.setQueryData(['portfolio'], msg.payload);
      break;
    case 'rate.tick':
      queryClient.setQueryData(['ticker', msg.payload.pair], msg.payload);
      break;
    case 'convert.settled':
    case 'transfer.settled':
    case 'payment.settled':
      queryClient.invalidateQueries({ queryKey: ['portfolio'] });
      queryClient.invalidateQueries({ queryKey: ['activity'] });
      break;
  }
}

/** Schedule a settlement event after a short delay (simulates async rails). */
export function scheduleSettlement(
  kind: 'convert' | 'transfer' | 'payment',
  id: string,
  extra?: { amountOut?: string; breakdown?: { asset: string; value: string }[] },
) {
  setTimeout(() => {
    if (kind === 'convert') {
      emit({ type: 'convert.settled', payload: { txId: id, amountOut: extra?.amountOut ?? '0' } });
    } else if (kind === 'transfer') {
      emit({ type: 'transfer.settled', payload: { txId: id, arrivedAt: Date.now() } });
    } else {
      emit({
        type: 'payment.settled',
        payload: { paymentId: id, breakdown: extra?.breakdown ?? [] },
      });
    }
    // Push a refreshed portfolio snapshot after settle
    emit({ type: 'balance.update', payload: portfolioFixture as Portfolio });
  }, 1200);
}

export function connectMockStream() {
  if (connected) return { close: () => disconnectMockStream() };
  connected = true;
  rateTimer = setInterval(() => {
    const base = 63204.18;
    const jitter = (Math.random() - 0.5) * 40;
    emit({
      type: 'rate.tick',
      payload: { pair: 'BTC/USD', rate: Math.round((base + jitter) * 100) / 100, ts: Date.now() },
    });
  }, 5000);
  return { close: () => disconnectMockStream() };
}

export function disconnectMockStream() {
  connected = false;
  if (rateTimer) clearInterval(rateTimer);
  rateTimer = null;
}

export function subscribeMockStream(listener: Listener) {
  listeners.add(listener);
  return () => listeners.delete(listener);
}

export function isMockStreamConnected() {
  return connected;
}
