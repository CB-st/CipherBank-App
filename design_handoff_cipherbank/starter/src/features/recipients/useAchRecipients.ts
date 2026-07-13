import { useCallback, useEffect, useState } from 'react';
import {
  listAchRecipients,
  upsertAchRecipient,
  seedAchRecipientsIfEmpty,
} from '@/features/persist/recipientsRepo';
import type { AchRecipient, AchRecipientInput } from '@/features/recipients/ach.types';

export function useAchRecipients() {
  const [recipients, setRecipients] = useState<AchRecipient[]>([]);
  const [loading, setLoading] = useState(true);

  const refresh = useCallback(async () => {
    setLoading(true);
    try {
      await seedAchRecipientsIfEmpty();
      setRecipients(await listAchRecipients());
    } catch {
      setRecipients([]);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const save = useCallback(
    async (input: AchRecipientInput) => {
      const row = await upsertAchRecipient(input);
      await refresh();
      return row;
    },
    [refresh],
  );

  return { recipients, loading, refresh, save };
}
