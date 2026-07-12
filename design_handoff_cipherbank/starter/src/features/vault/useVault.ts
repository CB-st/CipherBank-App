import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import {
  addCardToken,
  listBinaries,
  listCards,
  registerBinary,
  removeCardToken,
} from './serverVault.api';
import { createLocalCustody, hasLocalCustody } from './custody';

/** Hybrid vault: local custody flags + server binaries/cards. */
export function useVault() {
  const qc = useQueryClient();
  const [localReady, setLocalReady] = useState(false);
  const [hasLocal, setHasLocal] = useState(false);

  useEffect(() => {
    hasLocalCustody().then((v) => {
      setHasLocal(v);
      setLocalReady(true);
    });
  }, []);

  const binaries = useQuery({
    queryKey: ['vault', 'binaries'],
    queryFn: listBinaries,
    staleTime: 30_000,
  });

  const cards = useQuery({
    queryKey: ['vault', 'cards'],
    queryFn: listCards,
    staleTime: 30_000,
  });

  const ensureLocal = useMutation({
    mutationFn: createLocalCustody,
    onSuccess: () => setHasLocal(true),
  });

  const addBinary = useMutation({
    mutationFn: registerBinary,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['vault', 'binaries'] }),
  });

  const addCard = useMutation({
    mutationFn: addCardToken,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['vault', 'cards'] }),
  });

  const removeCard = useMutation({
    mutationFn: removeCardToken,
    onSuccess: () => qc.invalidateQueries({ queryKey: ['vault', 'cards'] }),
  });

  return {
    localReady,
    hasLocal,
    binaries: binaries.data?.binaries ?? [],
    cards: cards.data?.cards ?? [],
    binariesLoading: binaries.isLoading,
    cardsLoading: cards.isLoading,
    ensureLocal,
    addBinary,
    addCard,
    removeCard,
    refetchVault: () => {
      binaries.refetch();
      cards.refetch();
    },
  };
}
