import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { requestQuote, type Quote } from './quotes.api';

/** Server-issued quote, client-counted TTL. Auto re-locks a fresh quote on expiry. */
export function useQuoteLock(from: string, to: string, amount: string) {
  const [secondsLeft, setSecondsLeft] = useState(0);
  const { data: quote, refetch } = useQuery<Quote>({
    queryKey: ['quote', from, to, amount],
    queryFn: () => requestQuote(from, to, amount),
    enabled: !!amount && Number(amount) > 0,
    refetchOnWindowFocus: false,
  });

  useEffect(() => {
    if (!quote) {
      setSecondsLeft(0);
      return;
    }
    const tick = () => {
      const left = Math.max(0, Math.round((quote.expiresAt - Date.now()) / 1000));
      setSecondsLeft(left);
      if (left === 0) refetch();
    };
    tick();
    const id = setInterval(tick, 1000);
    return () => clearInterval(id);
  }, [quote, refetch]);

  const expired = !!quote && secondsLeft === 0;
  return { quote, secondsLeft, expired, relock: refetch };
}
