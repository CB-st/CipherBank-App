import { getSyncMeta, setSyncMeta } from '@/features/persist/marketRepo';

export type SetupPath = 'new' | 'returning';

const KEY_PATH = 'setup_path';
const KEY_COMPLETE = 'setup_complete';
const KEY_BOOTSTRAP_AT = 'account_bootstrap_at';

export async function beginSetupPath(path: SetupPath): Promise<void> {
  await setSyncMeta(KEY_PATH, path);
  await setSyncMeta(KEY_COMPLETE, '0');
}

export async function getSetupPath(): Promise<SetupPath | null> {
  const v = await getSyncMeta(KEY_PATH);
  if (v === 'new' || v === 'returning') return v;
  return null;
}

export async function isSetupComplete(): Promise<boolean> {
  const complete = await getSyncMeta(KEY_COMPLETE);
  if (complete === '1') return true;
  if (complete === '0') return false;
  // No setup flags → legacy / seed-demo / already using the app — do not nag.
  return true;
}

export async function markSetupComplete(): Promise<void> {
  await setSyncMeta(KEY_COMPLETE, '1');
}

export async function markAccountBootstrapAt(ts = Date.now()): Promise<void> {
  await setSyncMeta(KEY_BOOTSTRAP_AT, String(ts));
}

export async function getSetupState(): Promise<{
  path: SetupPath | null;
  complete: boolean;
  bootstrapAt: number | null;
}> {
  const path = await getSetupPath();
  const complete = await isSetupComplete();
  const raw = await getSyncMeta(KEY_BOOTSTRAP_AT);
  const bootstrapAt = raw ? Number(raw) : null;
  return {
    path,
    complete,
    bootstrapAt: Number.isFinite(bootstrapAt) ? bootstrapAt : null,
  };
}
