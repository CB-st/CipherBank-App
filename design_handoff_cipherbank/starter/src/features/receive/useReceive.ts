import { useQuery } from '@tanstack/react-query';
import { getReceive } from './receive.api';
export function useReceive(asset: string) {
  return useQuery({ queryKey: ['receive', asset], queryFn: () => getReceive(asset), staleTime: 60_000 });
}
