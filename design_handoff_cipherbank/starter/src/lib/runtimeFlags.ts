/** Runtime feature flags from Expo public env. */

/** Route /v1 through local mock handlers. */
export function isMockApi(): boolean {
  return process.env.EXPO_PUBLIC_USE_MOCK === 'true';
}

/**
 * Lab / QA mode: pre-seed demo custody, ACH payees, and rich portfolio.
 * Clean OOTB installs leave this unset/false.
 * Legacy `EXPO_PUBLIC_MOCK_HAS_WALLET=true` still enables seed demo for one release.
 */
export function isSeedDemo(): boolean {
  return (
    process.env.EXPO_PUBLIC_SEED_DEMO === 'true' ||
    process.env.EXPO_PUBLIC_MOCK_HAS_WALLET === 'true'
  );
}
