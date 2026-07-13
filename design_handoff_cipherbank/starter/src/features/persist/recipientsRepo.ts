import { getDb } from '@/features/persist/db';
import {
  initialsFromName,
  type AchAccountType,
  type AchRail,
  type AchRecipient,
  type AchRecipientInput,
} from '@/features/recipients/ach.types';
import { isSeedDemo } from '@/lib/runtimeFlags';

type AchRow = {
  id: string;
  display_name: string;
  account_holder_name: string;
  routing_number: string | null;
  account_number: string | null;
  account_type: string | null;
  bank_name: string | null;
  account_last4: string | null;
  rail: string;
  handle: string | null;
  memo: string | null;
  initials: string;
  created_at: number;
  updated_at: number;
};

function rowToRecipient(r: AchRow): AchRecipient {
  return {
    id: r.id,
    displayName: r.display_name,
    accountHolderName: r.account_holder_name,
    routingNumber: r.routing_number ?? undefined,
    accountNumber: r.account_number ?? undefined,
    accountType: (r.account_type as AchAccountType | null) ?? undefined,
    bankName: r.bank_name ?? undefined,
    accountLast4: r.account_last4 ?? undefined,
    rail: (r.rail as AchRail) || 'ACH',
    handle: r.handle ?? undefined,
    memo: r.memo ?? undefined,
    initials: r.initials,
    createdAt: r.created_at,
    updatedAt: r.updated_at,
  };
}

function last4FromAccount(accountNumber?: string, explicit?: string): string | null {
  if (explicit) return explicit;
  if (accountNumber && accountNumber.length >= 4) return accountNumber.slice(-4);
  return null;
}

export async function listAchRecipients(): Promise<AchRecipient[]> {
  const db = await getDb();
  const rows = await db.getAllAsync<AchRow>(
    'SELECT * FROM ach_recipients ORDER BY display_name COLLATE NOCASE ASC',
  );
  return rows.map(rowToRecipient);
}

export async function getAchRecipient(id: string): Promise<AchRecipient | null> {
  const db = await getDb();
  const row = await db.getFirstAsync<AchRow>('SELECT * FROM ach_recipients WHERE id = ?', id);
  return row ? rowToRecipient(row) : null;
}

export async function upsertAchRecipient(input: AchRecipientInput): Promise<AchRecipient> {
  const db = await getDb();
  const now = Date.now();
  const id = input.id ?? `rcp_${now.toString(36)}`;
  const existing = await getAchRecipient(id);
  const createdAt = existing?.createdAt ?? now;
  const initials = input.initials?.trim() || initialsFromName(input.displayName);
  const last4 = last4FromAccount(input.accountNumber, input.accountLast4);

  await db.runAsync(
    `INSERT OR REPLACE INTO ach_recipients
      (id, display_name, account_holder_name, routing_number, account_number, account_type,
       bank_name, account_last4, rail, handle, memo, initials, created_at, updated_at)
     VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`,
    id,
    input.displayName.trim(),
    input.accountHolderName.trim(),
    input.routingNumber?.replace(/\D/g, '') || null,
    input.accountNumber?.replace(/\s/g, '') || null,
    input.accountType ?? null,
    input.bankName?.trim() || null,
    last4,
    input.rail || 'ACH',
    input.handle?.trim() || null,
    input.memo?.trim() || null,
    initials,
    createdAt,
    now,
  );

  const saved = await getAchRecipient(id);
  if (!saved) throw new Error('Failed to save recipient');
  return saved;
}

export async function deleteAchRecipient(id: string): Promise<void> {
  const db = await getDb();
  await db.runAsync('DELETE FROM ach_recipients WHERE id = ?', id);
}

/** Seed demo ACH payees once — lab mode only (`EXPO_PUBLIC_SEED_DEMO`). */
export async function seedAchRecipientsIfEmpty(): Promise<void> {
  if (!isSeedDemo()) return;

  const db = await getDb();
  const done = await db.getFirstAsync<{ value: string }>(
    'SELECT value FROM sync_meta WHERE key = ?',
    'ach_seeded_v1',
  );
  if (done?.value === '1') return;

  const count = await db.getFirstAsync<{ n: number }>('SELECT COUNT(*) as n FROM ach_recipients');
  if ((count?.n ?? 0) === 0) {
    const seeds: AchRecipientInput[] = [
      {
        id: 'maya',
        displayName: 'Maya Chen',
        accountHolderName: 'Maya Chen',
        routingNumber: '021000021',
        accountNumber: '1000004021',
        accountType: 'checking',
        bankName: 'Chase',
        accountLast4: '4021',
        rail: 'ACH',
        handle: 'maya@cipherbank.id',
        initials: 'MC',
      },
      {
        id: 'sunset',
        displayName: 'Sunset Property Mgmt',
        accountHolderName: 'Sunset Property Management LLC',
        routingNumber: '121000248',
        accountNumber: '8877665544',
        accountType: 'checking',
        bankName: 'Wells Fargo',
        accountLast4: '5544',
        rail: 'ACH',
        handle: 'sunset@property.pay',
        memo: 'Rent',
        initials: 'SP',
      },
      {
        id: 'jordan',
        displayName: 'Jordan Lee',
        accountHolderName: 'Jordan A Lee',
        routingNumber: '026009593',
        accountNumber: '1122334455',
        accountType: 'savings',
        bankName: 'Bank of America',
        accountLast4: '4455',
        rail: 'ACH',
        initials: 'JL',
      },
    ];
    for (const s of seeds) {
      await upsertAchRecipient(s);
    }
  }

  await db.runAsync(
    'INSERT OR REPLACE INTO sync_meta (key, value, updated_at) VALUES (?, ?, ?)',
    'ach_seeded_v1',
    '1',
    Date.now(),
  );
}
