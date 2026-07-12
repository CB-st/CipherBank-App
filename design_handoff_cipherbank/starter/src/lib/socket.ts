import { queryClient } from './queryClient';
import { useMock, connectMockStream } from '@/mocks';

const WSS = process.env.EXPO_PUBLIC_WSS ?? '';

/**
 * Real-time stream: rate ticks + settlement events → React Query cache.
 * In mock mode, uses an in-process emitter instead of a WebSocket.
 */
export function connectStream(): { close: () => void } {
  if (useMock()) {
    return connectMockStream();
  }

  const ws = new WebSocket(WSS);
  ws.onmessage = (e) => {
    const msg = JSON.parse(e.data) as { type: string; payload: any };
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
  };
  ws.onclose = () => setTimeout(connectStream, 2000);
  return { close: () => ws.close() };
}
