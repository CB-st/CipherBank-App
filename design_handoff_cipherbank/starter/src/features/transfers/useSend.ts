import { useMutation, useQueryClient } from '@tanstack/react-query';
import { sendTransfer, type Speed } from './transfers.api';

export function useSend() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (v: { recipient: string; amount: string; source: string; speed: Speed }) => sendTransfer(v),
    onSettled: () => qc.invalidateQueries({ queryKey: ['portfolio'] }),
  });
}
