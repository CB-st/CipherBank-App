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

    for (const d of extras) {
      const matchById = byId.get(d.id);
      const matchByLabel = existing.find((w) => w.label === d.label || (w.label === 'Primary' && d.accountIndex === 0));
      const match = matchById ?? matchByLabel;

      if (match && d.address) {
        // Prefer API / derived draft address over fixture when present
        byId.set(match.id, {
          ...match,
          label: d.label || match.label,
          address: d.address,
          derivationPath: d.derivationPath ?? match.derivationPath,
          source: d.source,
          mode: d.mode ?? match.mode,
          sync: d.sync ?? match.sync,
          viewKeyFingerprint: d.viewKeyFingerprint ?? match.viewKeyFingerprint,
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
          mode: d.mode,
          sync: d.sync,
          viewKeyFingerprint: d.viewKeyFingerprint,
        });
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
