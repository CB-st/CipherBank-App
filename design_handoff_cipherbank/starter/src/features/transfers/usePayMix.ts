import { useMutation, useQueryClient } from '@tanstack/react-query';
import { payWithMix } from './transfers.api';

/** Pay a bill with multiple funding sources. Server mediates; recipient gets clean funds. */
export function usePayMix() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: { recipient: string; total: string; sources: { asset: string; value: string }[] }) => payWithMix(v),
    onSettled: () => qc.invalidateQueries({ queryKey: ['portfolio'] }),
  });
}
