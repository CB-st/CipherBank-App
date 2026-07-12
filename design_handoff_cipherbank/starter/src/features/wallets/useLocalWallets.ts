import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { Holding, Portfolio, WalletAccount } from '@/features/portfolio/portfolio.types';
import {
  addLocalWallet,
  deriveNextWallet,
  loadLocalWallets,
  removeLocalWallet,
} from './localWallets';

const QK = ['localWallets'] as const;

/** Merge AsyncStorage drafts into portfolio holdings (zero-balance slots until chain read). */
export function mergeLocalWallets(portfolio: Portfolio, drafts: Awaited<ReturnType<typeof loadLocalWallets>>): Portfolio {
  if (!drafts.length) return portfolio;
  const holdings = portfolio.holdings.map((h) => {
    const extras = drafts.filter((d) => d.symbol === h.symbol);
    if (!extras.length) return h;
    const existing = h.wallets ?? defaultWallet(h);
    const byId = new Map(existing.map((w) => [w.id, w]));
    // Prefer derived draft metadata onto matching primary slots by label/index
    for (const d of extras) {
      const match =
        byId.get(d.id) ??
        existing.find(
          (w) =>
            w.source === 'local' &&
            !w.address &&
            (w.label === 'Primary' || w.label === d.label) &&
            d.accountIndex === 0,
        );
      if (match && (d.address || d.derivationPath)) {
        byId.set(match.id, {
          ...match,
          id: d.id.startsWith('wal_local_') ? match.id : match.id,
          label: d.label || match.label,
          address: d.address ?? match.address,
          derivationPath: d.derivationPath ?? match.derivationPath,
          source: d.source,
        });
        continue;
      }
      if (!byId.has(d.id)) {
        byId.set(d.id, {
          id: d.id,
          label: d.label,
          amount: '0',
          usdValue: 0,
          address: d.address,
          derivationPath: d.derivationPath,
          source: d.source,
        });
      }
    }
    // Also patch fixture wallets missing address when we have a derived Primary
    const primaryDraft = extras.find((d) => d.accountIndex === 0 && d.address);
    if (primaryDraft) {
      for (const [id, w] of byId) {
        if (w.label === 'Primary' && !w.address) {
          byId.set(id, {
            ...w,
            address: primaryDraft.address,
            derivationPath: primaryDraft.derivationPath,
            source: 'local',
          });
        }
      }
    }
    return { ...h, wallets: Array.from(byId.values()) };
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

  const deriveNext = useMutation({
    mutationFn: (input: { symbol: string; label?: string }) => deriveNextWallet(input.symbol, input.label),
    onSuccess: () => qc.invalidateQueries({ queryKey: QK }),
  });

  const remove = useMutation({
    mutationFn: removeLocalWallet,
    onSuccess: () => qc.invalidateQueries({ queryKey: QK }),
  });

  return { drafts: q.data ?? [], ready: q.isSuccess, add, deriveNext, remove };
}
