/**
 * On-device ACH / payee contacts for Send.
 * Fields mirror what an ODFI needs to originate a US ACH credit (NACHA):
 * account holder name, ABA routing number, account number, account type.
 */
export type AchAccountType = 'checking' | 'savings';

export type AchRail = 'ACH' | 'cipherbank';

export type AchRecipient = {
  id: string;
  /** Short label shown in the contact list. */
  displayName: string;
  /** Legal name on the receiving account (ACH receiver name). */
  accountHolderName: string;
  /** ABA routing number (9 digits) — required for bank ACH. */
  routingNumber?: string;
  /** Full DDA account number — stored on-device only. */
  accountNumber?: string;
  accountType?: AchAccountType;
  bankName?: string;
  /** Display helper; derived from accountNumber when omitted. */
  accountLast4?: string;
  rail: AchRail;
  /** CipherBank handle when rail is cipherbank (or dual). */
  handle?: string;
  /** Default payment memo / ACH addenda. */
  memo?: string;
  initials: string;
  createdAt: number;
  updatedAt: number;
};

export type AchRecipientInput = Omit<AchRecipient, 'id' | 'createdAt' | 'updatedAt' | 'accountLast4'> & {
  id?: string;
  accountLast4?: string;
};

export function initialsFromName(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return '?';
  if (parts.length === 1) return parts[0]!.slice(0, 2).toUpperCase();
  return (parts[0]![0]! + parts[parts.length - 1]![0]!).toUpperCase();
}

export function maskAccount(r: AchRecipient): string {
  if (r.accountLast4) return `••${r.accountLast4}`;
  if (r.accountNumber && r.accountNumber.length >= 4) return `••${r.accountNumber.slice(-4)}`;
  if (r.handle) return r.handle;
  return 'Account';
}

export function recipientSubtitle(r: AchRecipient): string {
  if (r.rail === 'ACH' || r.routingNumber) {
    const bank = r.bankName ?? 'Bank';
    return `${bank} ${maskAccount(r)} · ACH`;
  }
  if (r.handle) return r.handle;
  return r.memo ?? 'Contact';
}
