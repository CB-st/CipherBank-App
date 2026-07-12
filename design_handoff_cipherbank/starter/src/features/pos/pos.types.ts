export type PosSessionStatus =
  | 'pending_auth'
  | 'authorized'
  | 'ready_to_present'
  | 'settled'
  | 'failed'
  | 'expired';

export interface PosSessionCreate {
  merchantId: string;
  amount: string;
  currency: string;
  posDeviceId?: string;
  label?: string;
}

export interface PosSession {
  sessionId: string;
  merchantId: string;
  amount: string;
  currency: string;
  label?: string;
  status: PosSessionStatus;
  expiresAt: number;
  ephemeralCardTokenId?: string;
  presentment?: PosPresentment;
}

export interface PosFundingSource {
  asset: string;
  value: string;
}

export interface PosAuthorizeBody {
  sessionId: string;
  sources: PosFundingSource[];
  cardId: string;
  fundingQuoteId?: string;
  /** Client attestation that local custody was unlocked — never send mnemonic. */
  deviceAttestation?: string;
}

export interface PosPresentment {
  tokenRef: string;
  last4: string;
  brand: string;
  ttlMs: number;
}

export interface PosAuthorizeResult {
  sessionId: string;
  status: 'authorized';
  ephemeralCardTokenId: string;
  presentment: PosPresentment;
}

export interface PosConfirmResult {
  sessionId: string;
  status: 'ready_to_present' | 'settled';
}

/** Payload written over NFC / mock tap — never includes PAN. */
export interface NfcPresentPayload {
  sessionId: string;
  tokenRef: string;
  merchantId?: string;
}
