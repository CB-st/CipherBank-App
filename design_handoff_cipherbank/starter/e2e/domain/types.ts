export type Network = 'bitcoin' | 'monero' | 'dogecoin';
export type CustodyMode = 'user' | 'cipherbank';
export type PaymentSource = 'user-wallet' | 'cb-wallet' | 'prepaid-card';

export type CanonicalOutcome =
  | 'APPROVED'
  | 'STEP_UP_REQUIRED'
  | 'DENIED_LIMIT'
  | 'DENIED_SECURITY'
  | 'DENIED_COMPLIANCE'
  | 'PENDING_REVIEW'
  | 'DEGRADED_RETRY'
  | 'FAILED_NETWORK'
  | 'EXPIRED_QUOTE';

export interface StoryStep {
  id: string;
  action: string;
  expected: string;
  service?: string;
}

export interface StoryDefinition {
  id: string;
  title: string;
  sourceDiagram: string;
  actor: string;
  userStory: string;
  preconditions: readonly string[];
  steps: readonly StoryStep[];
  successCriteria: readonly string[];
  negativeCases: readonly string[];
}

export interface AccountInput {
  email: string;
  password: string;
}

export interface WalletInput {
  label: string;
  network: Network;
  custody: CustodyMode;
}

export interface CardFundingInput {
  sourceWalletLabel: string;
  sourceAsset: Network;
  cardCurrency: string;
  amount: string;
}

export interface MerchantPaymentInput {
  merchant: string;
  amount: string;
  currency: string;
  source: PaymentSource;
}
