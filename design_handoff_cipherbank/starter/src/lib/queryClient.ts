import { QueryClient } from '@tanstack/react-query';

// Stale-while-revalidate defaults: serve cache instantly, refresh in the background.
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 15_000,
      gcTime: 5 * 60_000,
      retry: 2,
      refetchOnWindowFocus: false,
    },
    mutations: { retry: 0 },
  },
});
