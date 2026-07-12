import { useMutation, useQueryClient } from '@tanstack/react-query';
import { api, uuid } from '@/lib/apiClient';
import type { Portfolio } from '../portfolio/portfolio.types';

/** Optimistic convert: commit locally, POST with idempotency key, reconcile on settle. */
export function useConvert() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: { quoteId: string; amount: string }) =>
      api.post('/convert', v, { idempotencyKey: uuid() }),
    onMutate: async (v) => {
      await qc.cancelQueries({ queryKey: ['portfolio'] });
      const prev = qc.getQueryData<Portfolio>(['portfolio']);
      // TODO: apply optimistic delta to prev here
      return { prev };
    },
    onError: (_e, _v, ctx) => { if (ctx?.prev) qc.setQueryData(['portfolio'], ctx.prev); },
    onSettled: () => qc.invalidateQueries({ queryKey: ['portfolio'] }),
  });
}
