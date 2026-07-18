import { queryClient } from './queryClient';
import { useMock, connectMockStream } from '@/mocks';
import { fromWire } from '@/lib/wireFormat';

const WSS = process.env.EXPO_PUBLIC_WSS ?? '';

type WireStreamMsg = {
  TYPE?: string;
  type?: string;
  PAYLOAD?: unknown;
  payload?: unknown;
};

/**
 * Real-time stream: rate ticks + settlement events → React Query cache.
 * Live WSS uses SCREAMING_SNAKE `{ TYPE, PAYLOAD }`; mock stays camelCase in-process.
 */
export function connectStream(): { close: () => void } {
  if (useMock()) {
    return connectMockStream();
  }

  const ws = new WebSocket(WSS);
  ws.onmessage = (e) => {
    const raw = JSON.parse(e.data) as WireStreamMsg;
    const type = raw.TYPE ?? raw.type ?? '';
    const payload = fromWire(raw.PAYLOAD ?? raw.payload ?? {});
    switch (type) {
      case 'BALANCE.UPDATE':
      case 'balance.update':
        queryClient.setQueryData(['portfolio'], payload);
        break;
      case 'RATE.TICK':
      case 'rate.tick': {
        const p = payload as { pair?: string; PAIR?: string };
        const pair = p.pair ?? p.PAIR;
        queryClient.setQueryData(['ticker', pair], payload);
        break;
      }
      case 'CONVERT.SETTLED':
      case 'TRANSFER.SETTLED':
      case 'PAYMENT.SETTLED':
      case 'POS.SETTLED':
      case 'convert.settled':
      case 'transfer.settled':
      case 'payment.settled':
      case 'pos.settled':
        queryClient.invalidateQueries({ queryKey: ['portfolio'] });
        queryClient.invalidateQueries({ queryKey: ['activity'] });
        break;
    }
  };
  ws.onclose = () => setTimeout(connectStream, 2000);
  return { close: () => ws.close() };
}
