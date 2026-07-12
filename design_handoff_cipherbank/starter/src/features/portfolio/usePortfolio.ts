import { useMemo } from 'react';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { getPortfolio } from './portfolio.api';
import { mergeLocalWallets, useLocalWallets } from '@/features/wallets/useLocalWallets';

/** Time-of-flight load: shell renders now, this streams balances in behind a skeleton. */
export function usePortfolio() {
  const q = useQuery({
    queryKey: ['portfolio'],
    queryFn: getPortfolio,
    staleTime: 15_000,
    placeholderData: keepPreviousData,
  });
  const { drafts } = useLocalWallets();

  const data = useMemo(() => {
    if (!q.data) return q.data;
    return mergeLocalWallets(q.data, drafts);
  }, [q.data, drafts]);

  return { ...q, data };
}
