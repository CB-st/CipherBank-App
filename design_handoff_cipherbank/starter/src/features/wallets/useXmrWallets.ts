import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { createWallet, listWallets, refreshWallet } from './xmr.api';
import type { CreateWalletBody } from './xmr.types';

const QK = ['serverWallets'] as const;

export function useServerWallets(symbol?: string) {
  return useQuery({
    queryKey: [...QK, symbol ?? 'all'],
    queryFn: () => listWallets(symbol),
    staleTime: 15_000,
  });
}

export function useCreateServerWallet() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateWalletBody) => createWallet(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: QK }),
  });
}

export function useRefreshServerWallet() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => refreshWallet(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: QK }),
  });
}
