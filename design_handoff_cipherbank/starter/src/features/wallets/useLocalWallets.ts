import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { Holding, Portfolio, WalletAccount } from '@/features/portfolio/portfolio.types';
import { addLocalWallet, loadLocalWallets, removeLocalWallet } from './localWallets';

const QK = ['localWallets'] as const;

/** Merge AsyncStorage drafts into portfolio holdings (zero-balance slots until chain read). */
export function mergeLocalWallets(portfolio: Portfolio, drafts: Awaited<ReturnType<typeof loadLocalWallets>>): Portfolio {
  if (!drafts.length) return portfolio;
  const holdings = portfolio.holdings.map((h) => {
    const extras = drafts.filter((d) => d.symbol === h.symbol);
    if (!extras.length) return h;
    const existing = h.wallets ?? defaultWallet(h);
    const ids = new Set(existing.map((w) => w.id));
    const added: WalletAccount[] = extras
      .filter((d) => !ids.has(d.id))
      .map((d) => ({
        id: d.id,
        label: d.label,
        amount: '0',
        usdValue: 0,
        address: d.address,
        source: d.source,
      }));
    if (!added.length) return h;
    return { ...h, wallets: [...existing, ...added] };
  });
  return { ...portfolio, holdings };
}

function defaultWallet(h: Holding): WalletAccount[] {
  return [
    {
      id: 'wal_' + h.symbol.toLowerCase() + '_primary',
      label: 'Primary',
      amount: h.amount,
      usdValue: h.usdValue,
      source: 'local',
    },
  ];
}

export function useLocalWallets() {
  const qc = useQueryClient();
  const q = useQuery({ queryKey: QK, queryFn: loadLocalWallets });

  const add = useMutation({
    mutationFn: addLocalWallet,
    onSuccess: () => qc.invalidateQueries({ queryKey: QK }),
  });

  const remove = useMutation({
    mutationFn: removeLocalWallet,
    onSuccess: () => qc.invalidateQueries({ queryKey: QK }),
  });

  return { drafts: q.data ?? [], ready: q.isSuccess, add, remove };
}
