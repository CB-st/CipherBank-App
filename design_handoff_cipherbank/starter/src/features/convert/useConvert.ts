import { useMutation, useQueryClient } from '@tanstack/react-query';
import { uuid } from '@/lib/apiClient';
import { mockRequest } from '@/mocks';
import type { Portfolio } from '../portfolio/portfolio.types';

/**
 * Optimistic convert settlement.
 * Settlement stays on the mock /convert path until POST /convert is live (Part B).
 * Live estimates come from /iquote via useQuoteLock when USE_MOCK=false.
 */
export function useConvert() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: { quoteId: string; amount: string }) =>
      mockRequest<{ txId: string; status: string }>('POST', '/convert', v, { idempotencyKey: uuid() }),
    onMutate: async () => {
      await qc.cancelQueries({ queryKey: ['portfolio'] });
      const prev = qc.getQueryData<Portfolio>(['portfolio']);
      return { prev };
    },
    onError: (_e, _v, ctx) => {
      if (ctx?.prev) qc.setQueryData(['portfolio'], ctx.prev);
    },
    onSettled: () => qc.invalidateQueries({ queryKey: ['portfolio'] }),
  });
}
