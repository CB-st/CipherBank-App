import { unlockLocalCustody, type UnlockOpts } from './custody';

export type AuthReason =
  | 'app_unlock'
  | 'payment'
  | 'convert'
  | 'pos_authorize'
  | 'pos_present'
  | 'reveal_keys'
  | 'copy_keys'
  | 'derive';

const PROMPTS: Record<AuthReason, string> = {
  app_unlock: 'Unlock CipherBank',
  payment: 'Confirm payment with biometrics',
  convert: 'Confirm convert with biometrics',
  pos_authorize: 'Authorize POS payment',
  pos_present: 'Confirm terminal presentment',
  reveal_keys: 'Unlock to view recovery phrase',
  copy_keys: 'Unlock to copy private material',
  derive: 'Unlock to derive a new address',
};

export type RequireAuthOpts = {
  reason: AuthReason;
  /** Always re-prompt (default true for payment / keys / POS present). */
  force?: boolean;
  pin?: string;
};

const FORCE_BY_DEFAULT: AuthReason[] = [
  'payment',
  'convert',
  'pos_authorize',
  'pos_present',
  'reveal_keys',
  'copy_keys',
];

/**
 * Step-up / app unlock gate.
 * Uses OS biometrics (with device passcode fallback) or CipherBank PIN.
 */
export async function requireAuth(opts: RequireAuthOpts): Promise<boolean> {
  const force = opts.force ?? FORCE_BY_DEFAULT.includes(opts.reason);
  const unlockOpts: UnlockOpts = {
    force,
    pin: opts.pin,
    skipBiometrics: !!opts.pin,
    promptMessage: PROMPTS[opts.reason],
  };
  return unlockLocalCustody(unlockOpts);
}

export function authPrompt(reason: AuthReason): string {
  return PROMPTS[reason];
}
