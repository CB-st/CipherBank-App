import { api } from '@/lib/apiClient';
import { upsertAchRecipient } from '@/features/persist/recipientsRepo';
import { loadPrefs, savePrefs } from '@/features/persist/prefsRepo';
import { normalizePrefs } from '@/features/prefs/localeCurrency';
import type { UserPrefs } from '@/features/prefs/prefs.types';
import { initialsFromName, type AchAccountType, type AchRail } from '@/features/recipients/ach.types';
import { markAccountBootstrapAt, markSetupComplete } from './setupState';

export type BootstrapRecipientPublic = {
  id: string;
  displayName: string;
  accountHolderName: string;
  bankName?: string;
  accountLast4?: string;
  accountType?: AchAccountType;
  routingNumber?: string;
  rail?: AchRail;
  handle?: string;
  memo?: string;
  initials?: string;
};

export type AccountBootstrapResponse = {
  prefs?: Partial<UserPrefs>;
  recipients: BootstrapRecipientPublic[];
  syncedAt: number;
};

/**
 * Pull CipherBank account metadata (contacts + prefs) for returning users / new installs.
 * Never returns seed material. Account numbers are not on the wire — last4 + routing only.
 */
export async function pullAccountBootstrap(): Promise<AccountBootstrapResponse> {
  const data = await api.get<AccountBootstrapResponse>('/account/bootstrap');
  const recipients = data.recipients ?? [];

  for (const r of recipients) {
    await upsertAchRecipient({
      id: r.id,
      displayName: r.displayName,
      accountHolderName: r.accountHolderName,
      bankName: r.bankName,
      accountLast4: r.accountLast4,
      accountType: r.accountType,
      routingNumber: r.routingNumber,
      // No full account number from cloud — user can fill later for ACH origination.
      rail: r.rail ?? 'ACH',
      handle: r.handle,
      memo: r.memo,
      initials: r.initials?.trim() || initialsFromName(r.displayName),
    });
  }

  if (data.prefs && Object.keys(data.prefs).length) {
    const local = await loadPrefs();
    await savePrefs(normalizePrefs({ ...local, ...data.prefs }));
  }

  await markAccountBootstrapAt(data.syncedAt ?? Date.now());
  if (recipients.length > 0) {
    await markSetupComplete();
  }

  return data;
}
